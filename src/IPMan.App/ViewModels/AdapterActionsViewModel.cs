using System.ComponentModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.Services;
using IPMan.Application.Logging;
using IPMan.Application.Networking;
using IPMan.Application.Settings;
using IPMan.Application.Profiles;
using IPMan.Domain.Networking;
using IPMan.Domain.Settings;
using IPMan.Domain.Profiles;

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
    private readonly OperationNotificationService? _operationNotificationService;
    private readonly ICriticalLogger _criticalLogger;
    private readonly ISafetyCountdownCoordinator _safetyCoordinator;
    private IReadOnlyList<NetworkProfile> _availableSettings = [];
    private bool _actionCancelledBecauseAdapterUnavailable;
    private bool _isDisposed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyStaticCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplyProfileCommand))]
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
    [NotifyCanExecuteChangedFor(nameof(ApplyProfileCommand))]
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

    public StaticIpv4ApplyResult? LastStaticApplyResult { get; private set; }
    public DhcpApplyResult? LastDhcpApplyResult { get; private set; }
    public SafeSwitchOutcome? LastSwitchOutcome { get; private set; }

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
        IAdapterRefreshCoordinator refreshCoordinator,
        ISafetyCountdownCoordinator safetyCoordinator)
        : this(
            staticApplyService,
            dhcpApplyService,
            recoveryRestoreService,
            confirmationService,
            refreshCoordinator,
            safetyCoordinator,
            quickActionService: null,
            clipboardService: null,
            settingsRepository: null,
            operationNotificationService: null)
    {
    }

    /// <summary>Creates the shared action coordinator with quick-action services.</summary>
    public AdapterActionsViewModel(
        IStaticIpv4ApplyService staticApplyService,
        IDhcpApplyService dhcpApplyService,
        IRecoveryRestoreService recoveryRestoreService,
        IUserConfirmationService confirmationService,
        IAdapterRefreshCoordinator refreshCoordinator,
        ISafetyCountdownCoordinator safetyCoordinator,
        IQuickNetworkActionService? quickActionService,
        IClipboardService? clipboardService,
        IAppSettingsRepository? settingsRepository = null,
        OperationNotificationService? operationNotificationService = null,
        ICriticalLogger? criticalLogger = null)
    {
        ArgumentNullException.ThrowIfNull(staticApplyService);
        ArgumentNullException.ThrowIfNull(dhcpApplyService);
        ArgumentNullException.ThrowIfNull(recoveryRestoreService);
        ArgumentNullException.ThrowIfNull(confirmationService);
        ArgumentNullException.ThrowIfNull(refreshCoordinator);
        ArgumentNullException.ThrowIfNull(safetyCoordinator);

        _staticApplyService = staticApplyService;
        _dhcpApplyService = dhcpApplyService;
        _recoveryRestoreService = recoveryRestoreService;
        _confirmationService = confirmationService;
        _refreshCoordinator = refreshCoordinator;
        _safetyCoordinator = safetyCoordinator;
        _quickActionService = quickActionService;
        _clipboardService = clipboardService;
        _settingsRepository = settingsRepository;
        _operationNotificationService = operationNotificationService;
        _criticalLogger = criticalLogger ?? new NullCriticalLogger();
    }

    /// <summary>Updates saved settings used to name the pre-switch configuration.</summary>
    public void SetAvailableSettings(IReadOnlyList<NetworkProfile> settings) => _availableSettings = settings;

    /// <summary>Attaches the actions to the selected adapter, or detaches them when null.</summary>
    public void Attach(AdapterViewModel? adapter)
    {
        if (ReferenceEquals(CurrentAdapter, adapter))
        {
            ClearStatusCore();
            NotifyCommandsCanExecuteChanged();
            return;
        }

        Unsubscribe(CurrentAdapter);
        CurrentAdapter = adapter;
        Subscribe(CurrentAdapter);

        ClearStatusCore();
        NotifyCommandsCanExecuteChanged();
    }

    /// <summary>Releases the selected adapter and draft subscriptions.</summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        Unsubscribe(CurrentAdapter);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteAction))]
    private async Task ApplyProfileAsync(NetworkProfile? profile, CancellationToken cancellationToken)
    {
        if (!CanExecuteAction())
        {
            return;
        }

        if (profile is null || profile.Mode == NetworkConfigurationMode.Dhcp)
        {
            await ApplyDhcpAsync(cancellationToken);
            return;
        }

        await ApplyStaticConfigurationAsync(CurrentAdapter!, profile.ToStaticConfiguration(), profile.Name, false, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteAction))]
    private async Task ApplyStaticAsync(CancellationToken cancellationToken)
    {
        AdapterViewModel? adapter = CurrentAdapter;

        if (adapter is null || !CanExecuteAction())
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
            SetStatus(ApplyResultMessageFormatter.DescribeInvalidInput(fields));
            ApplyCompleted?.Invoke(this, false);
            return;
        }

        await ApplyStaticConfigurationAsync(adapter, draft.ValidatedConfiguration!, Strings.SafeSwitchManualSetting, true, cancellationToken);
    }

    private async Task ApplyStaticConfigurationAsync(
        AdapterViewModel adapter,
        StaticIpv4Configuration configuration,
        string settingName,
        bool markDraftApplied,
        CancellationToken cancellationToken)
    {
        bool succeeded = await RunSwitchAsync(adapter, settingName, configuration,
            Strings.NotificationOperationApply, async token =>
            {
                StaticIpv4ApplyResult? result = await ApplyStaticWithConfirmationsAsync(adapter, configuration, token);
                LastStaticApplyResult = result;
                if (result is null)
                {
                    return null;
                }
                SetStatus(ApplyResultMessageFormatter.Describe(result));
                return result.Status switch
                {
                    StaticIpv4ApplyStatus.VerifiedSuccess => SafeSwitchApplyResult.Applied,
                    StaticIpv4ApplyStatus.NoChange => SafeSwitchApplyResult.NoChange,
                    _ => SafeSwitchApplyResult.Failed
                };
            }, cancellationToken);
        if (succeeded && markDraftApplied)
        {
            adapter.Draft.MarkApplied();
        }
        ApplyCompleted?.Invoke(this, succeeded);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteDhcp))]
    private async Task ApplyDhcpAsync(CancellationToken cancellationToken)
    {
        ClearSwitchResult();
        AdapterViewModel? adapter = CurrentAdapter;
        if (adapter is null || !CanExecuteAction())
        {
            ApplyCompleted?.Invoke(this, false);
            return;
        }
        if (!ShouldApplyDhcp(adapter))
        {
            ApplyCompleted?.Invoke(this, false);
            return;
        }
        bool succeeded = await RunSwitchAsync(adapter, Strings.SafeSwitchAutomatic, null,
            Strings.NotificationOperationDhcp, async token =>
            {
                DhcpApplyResult result = await _dhcpApplyService.ApplyAsync(new DhcpApplyRequest(adapter.Id), token);
                LastDhcpApplyResult = result;
                SetStatus(ApplyResultMessageFormatter.Describe(result));
                return result.Status switch
                {
                    DhcpApplyStatus.VerifiedSuccess => SafeSwitchApplyResult.Applied,
                    DhcpApplyStatus.NoChange => SafeSwitchApplyResult.NoChange,
                    _ => SafeSwitchApplyResult.Failed
                };
            }, cancellationToken);
        ApplyCompleted?.Invoke(this, succeeded);
    }

    private async Task<bool> RunSwitchAsync(AdapterViewModel adapter, string settingName,
        StaticIpv4Configuration? target, string operation,
        Func<CancellationToken, Task<SafeSwitchApplyResult?>> applyAsync, CancellationToken cancellationToken)
    {
        ClearSwitchResult();
        NetworkAdapterSnapshot snapshot = adapter.Snapshot;
        string previousName = snapshot.Mode == NetworkConfigurationMode.Dhcp ? Strings.SafeSwitchAutomatic :
            new ActiveSettingMatcher().Find(snapshot, _availableSettings)?.Name ?? Strings.SafeSwitchManualSetting;
        SafeSwitchRequest request = new(snapshot, settingName, previousName, target);
        bool applyCompleted = false;
        IsBusy = true;
        ClearStatusCore();
        try
        {
            SafeSwitchOutcome outcome = await _safetyCoordinator.RunAsync(request, async token =>
            {
                try
                {
                    SafeSwitchApplyResult? result = await applyAsync(token);
                    applyCompleted = result is not null;
                    return result ?? SafeSwitchApplyResult.Failed;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    applyCompleted = true;
                    ReportUnexpectedActionFailure(operation, exception);
                    return SafeSwitchApplyResult.Failed;
                }
            }, cancellationToken);
            LastSwitchOutcome = outcome;
            switch (outcome.Status)
            {
                case SafeSwitchStatus.Kept:
                    _actionCancelledBecauseAdapterUnavailable = false;
                    SetStatus(Strings.FormatSafeSwitchKept(settingName), ApplyStatusSeverity.Information);
                    ReportOperation(operation, adapter, succeeded: true);
                    break;
                case SafeSwitchStatus.Reverted:
                    _actionCancelledBecauseAdapterUnavailable = false;
                    SetStatus(Strings.FormatSafeSwitchReverted(previousName), ApplyStatusSeverity.Information);
                    ReportOperation(Strings.NotificationOperationRestore, adapter, succeeded: true);
                    break;
                case SafeSwitchStatus.RevertFailed:
                    _actionCancelledBecauseAdapterUnavailable = false;
                    SetStatus(Strings.SafeSwitchRevertFailed, ApplyStatusSeverity.Error);
                    ReportOperation(Strings.NotificationOperationRestore, adapter, succeeded: false);
                    break;
                case SafeSwitchStatus.Cancelled:
                    ClearStatusCore();
                    break;
                case SafeSwitchStatus.Busy:
                    SetStatus(Strings.SafeSwitchBusy, ApplyStatusSeverity.Information);
                    break;
                case SafeSwitchStatus.ApplyFailed:
                    if (!HasStatusMessage)
                    {
                        SetStatus(outcome.Message ?? Strings.SafeSwitchApplyFailed, ApplyStatusSeverity.Error);
                    }
                    if (applyCompleted)
                    {
                        ReportOperation(operation, adapter, succeeded: false);
                    }
                    break;
                case SafeSwitchStatus.NoChange:
                    ReportOperation(operation, adapter, succeeded: true);
                    break;
            }
            if (outcome.PreferenceSaveFailed)
            {
                SetStatus(string.Join(Environment.NewLine, new[] { StatusMessage, Strings.SettingsPersistenceWarning }
                    .Where(text => text.Length > 0)),
                    StatusSeverity == ApplyStatusSeverity.Error ? ApplyStatusSeverity.Error : ApplyStatusSeverity.Warning);
            }
            return outcome.Status is SafeSwitchStatus.Kept or SafeSwitchStatus.NoChange;
        }
        finally
        {
            if (applyCompleted)
            {
                _refreshCoordinator.RequestRefresh(NetworkChangeReason.ConfigurationApplied);
            }
            IsBusy = false;
        }
    }

    private void ClearSwitchResult()
    {
        LastStaticApplyResult = null;
        LastDhcpApplyResult = null;
        LastSwitchOutcome = null;
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
        catch (Exception exception)
        {
            ReportUnexpectedActionFailure(nameof(PingGatewayCommand), exception);
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
        catch (Exception exception)
        {
            ReportUnexpectedActionFailure(nameof(FlushDnsCommand), exception);
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
            ReportOperation(Strings.NotificationOperationRenewIp, adapter, result.IsSuccess);
            _refreshCoordinator.RequestRefresh(NetworkChangeReason.ConfigurationApplied);
        }
        catch (OperationCanceledException)
        {
            SetStatus(Strings.AdapterActionUnavailable, ApplyStatusSeverity.Error);
            ReportOperation(Strings.NotificationOperationRenewIp, adapter, succeeded: false);
        }
        catch (Exception exception)
        {
            ReportUnexpectedActionFailure(nameof(RenewIpCommand), exception);
            ReportOperation(Strings.NotificationOperationRenewIp, adapter, succeeded: false);
            _refreshCoordinator.RequestRefresh(NetworkChangeReason.ConfigurationApplied);
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
        bool failedUnexpectedly = false;
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
            ReportOperation(
                Strings.NotificationOperationRestore,
                adapter,
                result.Status is RecoveryRestoreStatus.VerifiedSuccess or RecoveryRestoreStatus.NoChange);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            failedUnexpectedly = true;
            ReportUnexpectedActionFailure(nameof(RestoreLastCommand), exception);
            ReportOperation(Strings.NotificationOperationRestore, adapter, succeeded: false);
        }
        finally
        {
            if (result is not null || failedUnexpectedly)
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
        ApplyProfileCommand.Cancel();
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

    /// <summary>
    /// Mode, gateway and connection dependent actions read the selected
    /// adapter's snapshot, which a refresh replaces in place on the same tab
    /// ViewModel. Observing it keeps those actions and their disabled reasons
    /// current after DHCP/static transitions and link changes.
    /// </summary>
    private void Subscribe(AdapterViewModel? adapter)
    {
        if (adapter is null)
        {
            return;
        }

        adapter.PropertyChanged += OnAdapterPropertyChanged;
        adapter.Draft.PropertyChanged += OnDraftPropertyChanged;
    }

    private void Unsubscribe(AdapterViewModel? adapter)
    {
        if (adapter is null)
        {
            return;
        }

        adapter.PropertyChanged -= OnAdapterPropertyChanged;
        adapter.Draft.PropertyChanged -= OnDraftPropertyChanged;
    }

    private void OnAdapterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AdapterViewModel.Snapshot) or null or "")
        {
            NotifyCommandsCanExecuteChanged();
        }
    }

    private void OnDraftPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        NotifyCommandsCanExecuteChanged();

    private void NotifyCommandsCanExecuteChanged()
    {
        ApplyStaticCommand.NotifyCanExecuteChanged();
        ApplyProfileCommand.NotifyCanExecuteChanged();
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

    /// <summary>
    /// Keeps an unexpected defect from a network action inside the action boundary:
    /// the service layer lets programming errors propagate, and this is the last
    /// point where they can be recorded and shown without terminating the process.
    /// </summary>
    private void ReportUnexpectedActionFailure(string action, Exception exception)
    {
        _criticalLogger.Log(new CriticalLogEntry(
            CriticalLogCategory.Unhandled,
            $"Adapter action {action} failed unexpectedly.",
            exception.HResult,
            exception));
        SetStatus(Strings.ActionUnexpectedFailure, ApplyStatusSeverity.Error);
    }

    private void ReportOperation(string operation, AdapterViewModel adapter, bool succeeded) =>
        _operationNotificationService?.Report(operation, adapter.Id.Value, adapter.DisplayName, succeeded);
}
