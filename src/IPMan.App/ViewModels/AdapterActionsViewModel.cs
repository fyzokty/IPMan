using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.App.ViewModels;

/// <summary>
/// Coordinates all network-changing actions for the currently selected adapter.
/// One shared busy state prevents the actions from overlapping.
/// </summary>
public sealed partial class AdapterActionsViewModel : ObservableObject, IDisposable
{
    private readonly IStaticIpv4ApplyService _staticApplyService;
    private readonly IDhcpApplyService _dhcpApplyService;
    private readonly IRecoveryRestoreService _recoveryRestoreService;
    private readonly IUserConfirmationService _confirmationService;
    private readonly IAdapterRefreshCoordinator _refreshCoordinator;
    private bool _isDisposed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyStaticCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplyDhcpCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestoreLastCommand))]
    private AdapterViewModel? _currentAdapter;

    /// <summary>True while one action is calling an application service.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyStaticCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplyDhcpCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestoreLastCommand))]
    private bool _isBusy;

    /// <summary>The localized inline result of the most recent action.</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>The visual importance of the most recent action result.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private ApplyStatusSeverity _statusSeverity;

    /// <summary>True when an inline action result should be displayed.</summary>
    public bool HasStatusMessage => StatusSeverity != ApplyStatusSeverity.None;

    /// <summary>Creates the shared action coordinator.</summary>
    public AdapterActionsViewModel(
        IStaticIpv4ApplyService staticApplyService,
        IDhcpApplyService dhcpApplyService,
        IRecoveryRestoreService recoveryRestoreService,
        IUserConfirmationService confirmationService,
        IAdapterRefreshCoordinator refreshCoordinator)
    {
        ArgumentNullException.ThrowIfNull(staticApplyService);
        ArgumentNullException.ThrowIfNull(dhcpApplyService);
        ArgumentNullException.ThrowIfNull(recoveryRestoreService);
        ArgumentNullException.ThrowIfNull(confirmationService);
        ArgumentNullException.ThrowIfNull(refreshCoordinator);

        _staticApplyService = staticApplyService;
        _dhcpApplyService = dhcpApplyService;
        _recoveryRestoreService = recoveryRestoreService;
        _confirmationService = confirmationService;
        _refreshCoordinator = refreshCoordinator;
    }

    /// <summary>Attaches the actions to the selected adapter, or detaches them when null.</summary>
    public void Attach(AdapterViewModel? adapter)
    {
        if (ReferenceEquals(CurrentAdapter, adapter))
        {
            ClearStatus();
            NotifyCommandsCanExecuteChanged();
            return;
        }

        if (CurrentAdapter is not null)
        {
            CurrentAdapter.Draft.PropertyChanged -= OnDraftPropertyChanged;
        }

        CurrentAdapter = adapter;

        if (CurrentAdapter is not null)
        {
            CurrentAdapter.Draft.PropertyChanged += OnDraftPropertyChanged;
        }

        ClearStatus();
        NotifyCommandsCanExecuteChanged();
    }

    /// <summary>Releases the selected draft subscription.</summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (CurrentAdapter is not null)
        {
            CurrentAdapter.Draft.PropertyChanged -= OnDraftPropertyChanged;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteAction))]
    private async Task ApplyStaticAsync(CancellationToken cancellationToken)
    {
        AdapterViewModel? adapter = CurrentAdapter;

        if (adapter is null)
        {
            return;
        }

        AdapterDraftViewModel draft = adapter.Draft;

        if (!draft.IsValid)
        {
            draft.MarkAllFieldsTouched();
            string fields = string.Join(
                Environment.NewLine,
                draft.Errors
                    .Select(error => error.Field)
                    .Distinct()
                    .Select(ValidationMessageFormatter.DescribeField));
            SetStatus(
                string.Concat(Strings.ApplyValidationFailed, Environment.NewLine, fields),
                ApplyStatusSeverity.Error);
            return;
        }

        StaticIpv4ApplyResult? result = null;
        IsBusy = true;

        try
        {
            result = await ApplyStaticWithConfirmationsAsync(
                adapter,
                draft.ValidatedConfiguration!,
                cancellationToken);

            if (result is null)
            {
                return;
            }

            ApplyStatusMessage message = ApplyResultMessageFormatter.Describe(result);
            SetStatus(message);

            if (result.Status is StaticIpv4ApplyStatus.VerifiedSuccess or StaticIpv4ApplyStatus.NoChange)
            {
                draft.MarkApplied();
            }
        }
        finally
        {
            if (result is not null)
            {
                _refreshCoordinator.RequestRefresh(NetworkChangeReason.ConfigurationApplied);
            }

            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteAction))]
    private async Task ApplyDhcpAsync(CancellationToken cancellationToken)
    {
        AdapterViewModel? adapter = CurrentAdapter;

        if (adapter is null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            DhcpApplyResult result = await _dhcpApplyService.ApplyAsync(
                new DhcpApplyRequest(adapter.Id),
                cancellationToken);
            SetStatus(ApplyResultMessageFormatter.Describe(result));
        }
        finally
        {
            _refreshCoordinator.RequestRefresh(NetworkChangeReason.ConfigurationApplied);
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteAction))]
    private async Task RestoreLastAsync(CancellationToken cancellationToken)
    {
        AdapterViewModel? adapter = CurrentAdapter;

        if (adapter is null)
        {
            return;
        }

        bool accepted = _confirmationService.Confirm(
            new UserConfirmationRequest(
                Strings.ConfirmRestoreTitle,
                Strings.FormatConfirmRestoreMessage(adapter.DisplayName)));

        if (!accepted)
        {
            SetStatus(Strings.ActionDeclined, ApplyStatusSeverity.Information);
            return;
        }

        RecoveryRestoreResult? result = null;
        IsBusy = true;

        try
        {
            result = await RestoreWithConfirmationsAsync(
                adapter,
                cancellationToken);

            if (result is null)
            {
                return;
            }

            SetStatus(ApplyResultMessageFormatter.Describe(result));
        }
        finally
        {
            if (result is not null)
            {
                _refreshCoordinator.RequestRefresh(NetworkChangeReason.ConfigurationApplied);
            }

            IsBusy = false;
        }
    }

    private async Task<StaticIpv4ApplyResult?> ApplyStaticWithConfirmationsAsync(
        AdapterViewModel adapter,
        StaticIpv4Configuration configuration,
        CancellationToken cancellationToken)
    {
        bool confirmPotentialConflict = false;
        bool continueAfterIndeterminateProbe = false;
        StaticIpv4ApplyResult? result = null;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            result = await _staticApplyService.ApplyAsync(
                new StaticIpv4ApplyRequest(
                    adapter.Id,
                    configuration,
                    confirmPotentialConflict,
                    continueAfterIndeterminateProbe),
                cancellationToken);

            if (attempt == 2)
            {
                break;
            }

            if (result.Status == StaticIpv4ApplyStatus.ConflictConfirmationRequired)
            {
                if (confirmPotentialConflict)
                {
                    break;
                }

                if (!ConfirmConflict(configuration.Ipv4Address))
                {
                    SetStatus(Strings.ActionDeclined, ApplyStatusSeverity.Information);
                    return null;
                }

                confirmPotentialConflict = true;
                continue;
            }

            if (result.Status == StaticIpv4ApplyStatus.ProbeIndeterminateConfirmationRequired)
            {
                if (continueAfterIndeterminateProbe)
                {
                    break;
                }

                if (!ConfirmIndeterminateProbe())
                {
                    SetStatus(Strings.ActionDeclined, ApplyStatusSeverity.Information);
                    return null;
                }

                continueAfterIndeterminateProbe = true;
                continue;
            }

            break;
        }

        return result!;
    }

    private async Task<RecoveryRestoreResult?> RestoreWithConfirmationsAsync(
        AdapterViewModel adapter,
        CancellationToken cancellationToken)
    {
        bool confirmPotentialConflict = false;
        bool continueAfterIndeterminateProbe = false;
        RecoveryRestoreResult? result = null;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            result = await _recoveryRestoreService.RestoreLatestAsync(
                new RecoveryRestoreRequest(
                    adapter.Id,
                    confirmPotentialConflict,
                    continueAfterIndeterminateProbe),
                cancellationToken);

            if (attempt == 2)
            {
                break;
            }

            if (result.Status == RecoveryRestoreStatus.ConflictConfirmationRequired)
            {
                if (confirmPotentialConflict)
                {
                    break;
                }

                string address = result.RestoredFrom?.Ipv4Addresses.FirstOrDefault()?.Address
                    ?? adapter.Draft.Ipv4Address;

                if (!ConfirmConflict(address))
                {
                    SetStatus(Strings.ActionDeclined, ApplyStatusSeverity.Information);
                    return null;
                }

                confirmPotentialConflict = true;
                continue;
            }

            if (result.Status == RecoveryRestoreStatus.ProbeIndeterminateConfirmationRequired)
            {
                if (continueAfterIndeterminateProbe)
                {
                    break;
                }

                if (!ConfirmIndeterminateProbe())
                {
                    SetStatus(Strings.ActionDeclined, ApplyStatusSeverity.Information);
                    return null;
                }

                continueAfterIndeterminateProbe = true;
                continue;
            }

            break;
        }

        return result!;
    }

    private bool ConfirmConflict(string address) =>
        _confirmationService.Confirm(
            new UserConfirmationRequest(
                Strings.ConfirmConflictTitle,
                Strings.FormatConfirmConflictMessage(address)));

    private bool ConfirmIndeterminateProbe() =>
        _confirmationService.Confirm(
            new UserConfirmationRequest(
                Strings.ConfirmProbeIndeterminateTitle,
                Strings.ConfirmProbeIndeterminateMessage));

    private bool CanExecuteAction() => !IsBusy && CurrentAdapter is not null;

    private void OnDraftPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        NotifyCommandsCanExecuteChanged();

    private void NotifyCommandsCanExecuteChanged()
    {
        ApplyStaticCommand.NotifyCanExecuteChanged();
        ApplyDhcpCommand.NotifyCanExecuteChanged();
        RestoreLastCommand.NotifyCanExecuteChanged();
    }

    private void ClearStatus()
    {
        StatusMessage = string.Empty;
        StatusSeverity = ApplyStatusSeverity.None;
    }

    private void SetStatus(ApplyStatusMessage message) => SetStatus(message.Text, message.Severity);

    private void SetStatus(string text, ApplyStatusSeverity severity)
    {
        StatusMessage = text;
        StatusSeverity = severity;
    }
}
