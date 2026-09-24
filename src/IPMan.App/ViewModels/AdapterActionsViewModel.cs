using System.ComponentModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Application.Networking;
using IPMan.Application.Settings;
using IPMan.Domain.Networking;
using IPMan.Domain.Settings;

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
    private readonly IQuickNetworkActionService? _quickActionService;
    private readonly IClipboardService? _clipboardService;
    private readonly IAppSettingsRepository? _settingsRepository;
    private bool _actionCancelledBecauseAdapterUnavailable;
    private bool _isDisposed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyStaticCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplyDhcpCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestoreLastCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyNetworkInfoCommand))]
    [NotifyCanExecuteChangedFor(nameof(PingGatewayCommand))]
    [NotifyCanExecuteChangedFor(nameof(FlushDnsCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenewIpCommand))]
    private AdapterViewModel? _currentAdapter;

    /// <summary>True while one action is calling an application service.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyStaticCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplyDhcpCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestoreLastCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyNetworkInfoCommand))]
    [NotifyCanExecuteChangedFor(nameof(PingGatewayCommand))]
    [NotifyCanExecuteChangedFor(nameof(FlushDnsCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenewIpCommand))]
    private bool _isBusy;

    /// <summary>The localized inline result of the most recent action.</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>The visual importance of the most recent action result.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private ApplyStatusSeverity _statusSeverity;

    /// <summary>Raised when a static or DHCP apply action finishes.</summary>
    public event EventHandler<bool>? ApplyCompleted;

    /// <summary>True when an inline action result should be displayed.</summary>
    public bool HasStatusMessage => StatusSeverity != ApplyStatusSeverity.None;

    /// <summary>Explains why the gateway ping button is unavailable.</summary>
    public string PingGatewayDisabledReason => GetPingGatewayDisabledReason();

    /// <summary>Explains why lease renewal is unavailable.</summary>
    public string RenewIpDisabledReason => GetRenewIpDisabledReason();

    /// <summary>Creates the shared action coordinator.</summary>
    public AdapterActionsViewModel(
        IStaticIpv4ApplyService staticApplyService,
        IDhcpApplyService dhcpApplyService,
        IRecoveryRestoreService recoveryRestoreService,
        IUserConfirmationService confirmationService,
        IAdapterRefreshCoordinator refreshCoordinator)
        : this(
            staticApplyService,
            dhcpApplyService,
            recoveryRestoreService,
            confirmationService,
            refreshCoordinator,
            quickActionService: null,
            clipboardService: null,
            settingsRepository: null)
    {
    }

    /// <summary>Creates the shared action coordinator with quick-action services.</summary>
    public AdapterActionsViewModel(
        IStaticIpv4ApplyService staticApplyService,
        IDhcpApplyService dhcpApplyService,
        IRecoveryRestoreService recoveryRestoreService,
        IUserConfirmationService confirmationService,
        IAdapterRefreshCoordinator refreshCoordinator,
        IQuickNetworkActionService? quickActionService,
        IClipboardService? clipboardService,
        IAppSettingsRepository? settingsRepository = null)
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
        _quickActionService = quickActionService;
        _clipboardService = clipboardService;
        _settingsRepository = settingsRepository;
    }

    /// <summary>Attaches the actions to the selected adapter, or detaches them when null.</summary>
    public void Attach(AdapterViewModel? adapter)
    {
        if (ReferenceEquals(CurrentAdapter, adapter))
        {
            ClearStatusCore();
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

        ClearStatusCore();
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

    /// <summary>Shows the concise completion notice for an explicitly selected profile.</summary>
    public void ShowProfileAppliedNotice()
    {
        StatusSeverity = ApplyStatusSeverity.None;
        SetStatus(Strings.ProfileApplied, ApplyStatusSeverity.Information);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteAction))]
    private async Task ApplyStaticAsync(CancellationToken cancellationToken)
    {
        AdapterViewModel? adapter = CurrentAdapter;

        if (adapter is null)
        {
            ApplyCompleted?.Invoke(this, false);
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
            ApplyCompleted?.Invoke(this, false);
            return;
        }

        StaticIpv4ApplyResult? result = null;
        bool succeeded = false;
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
                succeeded = true;
            }
        }
        finally
        {
            if (result is not null)
            {
                _refreshCoordinator.RequestRefresh(NetworkChangeReason.ConfigurationApplied);
            }

            IsBusy = false;
            ApplyCompleted?.Invoke(this, succeeded);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteDhcp))]
    private async Task ApplyDhcpAsync(CancellationToken cancellationToken)
    {
        AdapterViewModel? adapter = CurrentAdapter;

        if (adapter is null)
        {
            ApplyCompleted?.Invoke(this, false);
            return;
        }

        if (!ShouldApplyDhcp(adapter))
        {
            ApplyCompleted?.Invoke(this, false);
            return;
        }

        bool succeeded = false;
        IsBusy = true;

        try
        {
            DhcpApplyResult result = await _dhcpApplyService.ApplyAsync(
                new DhcpApplyRequest(adapter.Id),
                cancellationToken);
            SetStatus(ApplyResultMessageFormatter.Describe(result));
            succeeded = result.Status is DhcpApplyStatus.VerifiedSuccess or DhcpApplyStatus.NoChange;
        }
        finally
        {
            _refreshCoordinator.RequestRefresh(NetworkChangeReason.ConfigurationApplied);
            IsBusy = false;
            ApplyCompleted?.Invoke(this, succeeded);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCopyNetworkInfo))]
    private void CopyNetworkInfo()
    {
        AdapterViewModel? adapter = CurrentAdapter;
        if (adapter is null || _clipboardService is null)
        {
            SetStatus(Strings.CopyNetworkInfoFailed, ApplyStatusSeverity.Error);
            return;
        }

        bool copied = _clipboardService.SetText(AdapterDisplayFormatter.FormatCopySummary(adapter.Snapshot));
        SetStatus(
            copied ? Strings.CopyNetworkInfoSuccess : Strings.CopyNetworkInfoFailed,
            copied ? ApplyStatusSeverity.Success : ApplyStatusSeverity.Error);
    }

    [RelayCommand(CanExecute = nameof(CanPingGateway))]
    private async Task PingGatewayAsync(CancellationToken cancellationToken)
    {
        AdapterViewModel? adapter = CurrentAdapter;
        if (adapter is null || _quickActionService is null ||
            adapter.Snapshot.Ipv4Gateways.Count == 0 ||
            !IPAddress.TryParse(adapter.Snapshot.Ipv4Gateways[0], out IPAddress? gateway))
        {
            SetStatus(Strings.AdapterActionUnavailable, ApplyStatusSeverity.Error);
            return;
        }

        IsBusy = true;
        try
        {
            GatewayPingResult result = await _quickActionService
                .PingGatewayAsync(gateway, cancellationToken)
                .ConfigureAwait(true);
            SetStatus(AdapterDisplayFormatter.FormatGatewayPingResult(gateway.ToString(), result));
        }
        catch (OperationCanceledException)
        {
            SetStatus(Strings.AdapterActionUnavailable, ApplyStatusSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanFlushDns))]
    private async Task FlushDnsAsync(CancellationToken cancellationToken)
    {
        if (!_confirmationService.Confirm(new UserConfirmationRequest(
                Strings.ConfirmFlushDnsTitle,
                Strings.ConfirmFlushDnsMessage)))
        {
            SetStatus(Strings.ActionDeclined, ApplyStatusSeverity.Information);
            return;
        }

        if (_quickActionService is null)
        {
            SetStatus(Strings.FormatQuickActionFailure(null), ApplyStatusSeverity.Error);
            return;
        }

        IsBusy = true;
        try
        {
            QuickNetworkActionResult result = await _quickActionService
                .FlushDnsCacheAsync(cancellationToken)
                .ConfigureAwait(true);
            SetQuickActionStatus(result, Strings.DnsFlushSuccess);
        }
        catch (OperationCanceledException)
        {
            SetStatus(Strings.AdapterActionUnavailable, ApplyStatusSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRenewIp))]
    private async Task RenewIpAsync(CancellationToken cancellationToken)
    {
        AdapterViewModel? adapter = CurrentAdapter;
        if (adapter is null || _quickActionService is null)
        {
            SetStatus(Strings.AdapterActionUnavailable, ApplyStatusSeverity.Error);
            return;
        }

        NetworkAdapterSnapshot previousSnapshot = adapter.Snapshot;
        IsBusy = true;
        try
        {
            QuickNetworkActionResult result = await _quickActionService
                .ReleaseRenewAsync(adapter.Id, cancellationToken)
                .ConfigureAwait(true);
            SetRenewIpStatus(result, previousSnapshot);
            _refreshCoordinator.RequestRefresh(NetworkChangeReason.ConfigurationApplied);
        }
        catch (OperationCanceledException)
        {
            SetStatus(Strings.AdapterActionUnavailable, ApplyStatusSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ClearStatus() => ClearStatusCore();

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

    /// <summary>Cancels an in-flight action after discovery loses its target adapter.</summary>
    public void CancelUnavailableAction()
    {
        if (!IsBusy)
        {
            return;
        }

        _actionCancelledBecauseAdapterUnavailable = true;
        ApplyStaticCommand.Cancel();
        ApplyDhcpCommand.Cancel();
        RestoreLastCommand.Cancel();
        PingGatewayCommand.Cancel();
        FlushDnsCommand.Cancel();
        RenewIpCommand.Cancel();
        SetStatus(Strings.AdapterActionUnavailable, ApplyStatusSeverity.Error);
    }

    partial void OnIsBusyChanged(bool value)
    {
        if (!value && _actionCancelledBecauseAdapterUnavailable)
        {
            _actionCancelledBecauseAdapterUnavailable = false;
            SetStatus(Strings.AdapterActionUnavailable, ApplyStatusSeverity.Error);
        }
    }

    private bool CanExecuteDhcp() => CanExecuteAction() &&
        CurrentAdapter!.Snapshot.Mode != NetworkConfigurationMode.Dhcp;

    private bool CanCopyNetworkInfo() => !IsBusy && CurrentAdapter is not null;

    private bool CanPingGateway() => !IsBusy && CurrentAdapter is not null &&
        CurrentAdapter.IsConnected && !IsUnsupportedQuickAdapter(CurrentAdapter.Snapshot) &&
        CurrentAdapter.Snapshot.Ipv4Gateways.Count > 0;

    private bool CanFlushDns() => !IsBusy && CurrentAdapter is not null;

    private bool CanRenewIp() => !IsBusy && CurrentAdapter is not null &&
        CurrentAdapter.IsConnected && !IsUnsupportedQuickAdapter(CurrentAdapter.Snapshot) &&
        CurrentAdapter.Snapshot.Mode == NetworkConfigurationMode.Dhcp;

    private string GetPingGatewayDisabledReason()
    {
        if (CurrentAdapter is null || !CurrentAdapter.IsConnected)
        {
            return Strings.QuickActionAdapterInactive;
        }

        if (IsUnsupportedQuickAdapter(CurrentAdapter.Snapshot))
        {
            return Strings.QuickActionUnsupportedAdapter;
        }

        return CurrentAdapter.Snapshot.Ipv4Gateways.Count == 0
            ? Strings.QuickActionNoIpv4Gateway
            : string.Empty;
    }

    private string GetRenewIpDisabledReason()
    {
        if (CurrentAdapter is null || !CurrentAdapter.IsConnected)
        {
            return Strings.QuickActionAdapterInactive;
        }

        if (IsUnsupportedQuickAdapter(CurrentAdapter.Snapshot))
        {
            return Strings.QuickActionUnsupportedAdapter;
        }

        return CurrentAdapter.Snapshot.Mode != NetworkConfigurationMode.Dhcp
            ? Strings.QuickActionDhcpRequired
            : string.Empty;
    }

    private static bool IsUnsupportedQuickAdapter(NetworkAdapterSnapshot snapshot)
    {
        string description = $"{snapshot.Name} {snapshot.Description}";
        return description.Contains("loopback", StringComparison.OrdinalIgnoreCase) ||
            description.Contains("tunnel", StringComparison.OrdinalIgnoreCase);
    }

    private void OnDraftPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        NotifyCommandsCanExecuteChanged();

    private void NotifyCommandsCanExecuteChanged()
    {
        ApplyStaticCommand.NotifyCanExecuteChanged();
        ApplyDhcpCommand.NotifyCanExecuteChanged();
        RestoreLastCommand.NotifyCanExecuteChanged();
        CopyNetworkInfoCommand.NotifyCanExecuteChanged();
        PingGatewayCommand.NotifyCanExecuteChanged();
        FlushDnsCommand.NotifyCanExecuteChanged();
        RenewIpCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(PingGatewayDisabledReason));
        OnPropertyChanged(nameof(RenewIpDisabledReason));
    }

    private void ClearStatusCore()
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

    private void SetQuickActionStatus(QuickNetworkActionResult result, string successMessage)
    {
        if (result.IsSuccess)
        {
            SetStatus(successMessage, ApplyStatusSeverity.Success);
            return;
        }

        SetStatus(
            result.IsAdapterUnavailable ? Strings.AdapterActionUnavailable : Strings.FormatQuickActionFailure(result.ErrorCode),
            ApplyStatusSeverity.Error);
    }

    private bool ShouldApplyDhcp(AdapterViewModel adapter)
    {
        AppSettings? settings = LoadSettings();
        if (settings?.SkipDhcpQuickActionConfirmation == true)
        {
            return true;
        }

        (bool accepted, bool doNotShowAgain) = _confirmationService.ConfirmWithOptions(
            new UserConfirmationRequest(
                Strings.ConfirmDhcpTitle,
                Strings.FormatConfirmDhcpMessage(
                    adapter.Snapshot.Ipv4Address ?? Strings.ValueUnavailable,
                    adapter.Snapshot.SubnetMask ?? Strings.ValueUnavailable,
                    adapter.Snapshot.Gateway ?? Strings.ValueUnavailable,
                    string.Join(", ", adapter.Snapshot.Ipv4DnsServers)),
                ShowDoNotShowAgain: true));
        if (!accepted)
        {
            SetStatus(Strings.ActionDeclined, ApplyStatusSeverity.Information);
            return false;
        }

        if (doNotShowAgain && settings is not null)
        {
            _ = _settingsRepository!
                .SaveAsync(
                    settings with { SkipDhcpQuickActionConfirmation = true },
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        return true;
    }

    private AppSettings? LoadSettings() => _settingsRepository?
        .LoadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();

    private void SetRenewIpStatus(
        QuickNetworkActionResult result,
        NetworkAdapterSnapshot previousSnapshot)
    {
        if (result.IsSuccess)
        {
            SetStatus(Strings.IpRenewSuccess, ApplyStatusSeverity.Success);
            return;
        }

        if (result.IsAdapterUnavailable)
        {
            SetStatus(Strings.AdapterActionUnavailable, ApplyStatusSeverity.Error);
            return;
        }

        SetStatus(
            Strings.FormatIpRenewFailure(
                previousSnapshot.Ipv4Address ?? Strings.ValueUnavailable,
                previousSnapshot.SubnetMask ?? Strings.ValueUnavailable,
                previousSnapshot.Gateway ?? Strings.ValueUnavailable,
                result.ErrorCode),
            ApplyStatusSeverity.Error);
    }
}
