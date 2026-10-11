using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.Services;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.ViewModels;

public sealed class SafeSwitchActionsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ManualAndDhcp_CountdownKeepsBusyAndDefersSingleNotification(bool dhcp)
    {
        using Fixture fixture = new();
        Task apply = dhcp ? fixture.Actions.ApplyDhcpCommand.ExecuteAsync(null) : fixture.Actions.ApplyStaticCommand.ExecuteAsync(null);
        await fixture.Dialogs.CountdownShown.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(fixture.Actions.IsBusy);
        Assert.False(fixture.Actions.ApplyProfileCommand.CanExecute(null));
        Assert.False(fixture.Actions.RestoreLastCommand.CanExecute(null));
        Assert.Empty(fixture.Notifications.History);
        Assert.False(apply.IsCompleted);
        fixture.Countdown.KeepCommand.Execute(null);
        await apply;
        Assert.False(fixture.Actions.IsBusy);
        Assert.Equal(
            Strings.FormatSafeSwitchKept(dhcp ? Strings.SafeSwitchAutomatic : Strings.SafeSwitchManualSetting),
            fixture.Actions.StatusMessage);
        Assert.Equal(ApplyStatusSeverity.Information, fixture.Actions.StatusSeverity);
        Assert.True(Assert.Single(fixture.Notifications.History).Succeeded);
        Assert.Empty(fixture.Restore.Requests);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task NoChangeOrFailure_NeverShowsCountdownAndPreservesServiceMessage(bool dhcp, bool noChange)
    {
        using Fixture fixture = new();
        fixture.Static.Result = new(noChange ? StaticIpv4ApplyStatus.NoChange : StaticIpv4ApplyStatus.RecoveryCaptureFailed);
        fixture.Dhcp.Result = new(noChange ? DhcpApplyStatus.NoChange : DhcpApplyStatus.RecoveryCaptureFailed);
        string expected = dhcp ? ApplyResultMessageFormatter.Describe(fixture.Dhcp.Result).Text :
            ApplyResultMessageFormatter.Describe(fixture.Static.Result).Text;
        await (dhcp ? fixture.Actions.ApplyDhcpCommand.ExecuteAsync(null) : fixture.Actions.ApplyStaticCommand.ExecuteAsync(null));
        Assert.Empty(fixture.Dialogs.Countdowns);
        Assert.Empty(fixture.Restore.Requests);
        Assert.Equal(expected, fixture.Actions.StatusMessage);
        Assert.False(fixture.Actions.IsBusy);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task RevertOrTimeout_ShowsPreviousSettingOrCalmFailure(bool timeout, bool restoreFails)
    {
        using Fixture fixture = new();
        fixture.Actions.SetAvailableSettings([TestData.Profile(name: "Ofis", primaryDns: "192.168.1.1", secondaryDns: null)]);
        if (restoreFails)
        {
            fixture.Restore.Result = new(RecoveryRestoreStatus.MutationFailed);
        }
        Task apply = fixture.Actions.ApplyProfileCommand.ExecuteAsync(TestData.Profile(name: "Saha", ipv4Address: "10.0.0.50"));
        await fixture.Dialogs.CountdownShown.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (timeout)
        {
            fixture.Timer.Advance(TimeSpan.FromSeconds(15));
        }
        else
        {
            fixture.Countdown.RevertCommand.Execute(null);
        }
        await apply;
        Assert.Equal(fixture.Adapter.Id, Assert.Single(fixture.Restore.Requests).AdapterId);
        Assert.Equal(restoreFails ? Strings.SafeSwitchRevertFailed : Strings.FormatSafeSwitchReverted("Ofis"), fixture.Actions.StatusMessage);
        Assert.Equal(restoreFails ? ApplyStatusSeverity.Error : ApplyStatusSeverity.Information, fixture.Actions.StatusSeverity);
        OperationNotificationService.OperationNotificationRecord notification = Assert.Single(fixture.Notifications.History);
        Assert.Equal(Strings.NotificationOperationRestore, notification.Operation);
        Assert.Equal(!restoreFails, notification.Succeeded);
    }

    [Fact]
    public async Task ConfirmationCancelled_ClearsBannerAndDoesNotApplyOrNotify()
    {
        using Fixture fixture = new();
        fixture.Adapter.Update(fixture.Adapter.Snapshot with { InternetConnectivity = InternetConnectivity.Internet });
        fixture.Confirmation.Result = new(false);
        fixture.Actions.StatusSeverity = ApplyStatusSeverity.Error;
        fixture.Actions.StatusMessage = "old result";
        await fixture.Actions.ApplyProfileCommand.ExecuteAsync(TestData.Profile(gateway: null));
        Assert.Empty(fixture.Static.Requests);
        Assert.Empty(fixture.Dialogs.Countdowns);
        Assert.Empty(fixture.Notifications.History);
        Assert.False(fixture.Actions.HasStatusMessage);
        Assert.Empty(fixture.Actions.StatusMessage);
    }

    [Fact]
    public async Task PreferenceSaveFailure_AddsWarningWithoutLosingKeptResult()
    {
        using Fixture fixture = new();
        fixture.Adapter.Update(fixture.Adapter.Snapshot with { InternetConnectivity = InternetConnectivity.Internet });
        fixture.Confirmation.Result = new(true, true);
        fixture.Settings.SaveResult = IPMan.Application.Settings.SettingsSaveResult.Failed(IPMan.Application.Settings.SettingsSaveStatus.IoFailure);
        Task apply = fixture.Actions.ApplyProfileCommand.ExecuteAsync(TestData.Profile(name: "Saha", gateway: null));
        await fixture.Dialogs.CountdownShown.Task.WaitAsync(TimeSpan.FromSeconds(5));
        fixture.Countdown.KeepCommand.Execute(null);
        await apply;
        Assert.Equal("Saha ayarına geçildi." + Environment.NewLine + Strings.SettingsPersistenceWarning, fixture.Actions.StatusMessage);
        Assert.Equal(ApplyStatusSeverity.Warning, fixture.Actions.StatusSeverity);
    }

    [Fact]
    public async Task RestoreLast_DoesNotStartSafeSwitchCountdown()
    {
        using Fixture fixture = new();
        await fixture.Actions.RestoreLastCommand.ExecuteAsync(null);
        Assert.Single(fixture.Restore.Requests);
        Assert.Empty(fixture.Dialogs.Countdowns);
        Assert.Empty(fixture.Confirmation.Requests);
    }

    [Fact]
    public async Task RestorePending_KeepsActionsBusyUntilRollbackCompletes()
    {
        using Fixture fixture = new();
        TaskCompletionSource<RecoveryRestoreResult> restored = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Restore.PendingResult = restored.Task;
        Task apply = fixture.Actions.ApplyStaticCommand.ExecuteAsync(null);
        await fixture.Dialogs.CountdownShown.Task.WaitAsync(TimeSpan.FromSeconds(5));
        fixture.Countdown.RevertCommand.Execute(null);
        await fixture.Restore.RestoreStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(fixture.Actions.IsBusy);
        Assert.False(fixture.Actions.ApplyDhcpCommand.CanExecute(null));
        Assert.Empty(fixture.Notifications.History);
        Assert.False(apply.IsCompleted);
        restored.SetResult(new(RecoveryRestoreStatus.VerifiedSuccess));
        await apply;
        Assert.False(fixture.Actions.IsBusy);
        Assert.Equal(Strings.FormatSafeSwitchReverted(Strings.SafeSwitchManualSetting), fixture.Actions.StatusMessage);
    }

    [Fact]
    public async Task AdapterLostDuringCountdown_PreservesRollbackFailureGuidance()
    {
        using Fixture fixture = new();
        fixture.Restore.Result = new(RecoveryRestoreStatus.AdapterUnavailable);
        Task apply = fixture.Actions.ApplyStaticCommand.ExecuteAsync(null);
        await fixture.Dialogs.CountdownShown.Task.WaitAsync(TimeSpan.FromSeconds(5));
        fixture.Actions.CancelUnavailableAction();
        await apply;
        Assert.Single(fixture.Restore.Requests);
        Assert.Equal(Strings.SafeSwitchRevertFailed, fixture.Actions.StatusMessage);
        Assert.Equal(ApplyStatusSeverity.Error, fixture.Actions.StatusSeverity);
        Assert.False(fixture.Actions.IsBusy);
    }

    [Fact]
    public async Task BusyCoordinator_DoesNotApplyAndKeepsExistingCountdownProtected()
    {
        using Fixture fixture = new();
        Task first = fixture.Actions.ApplyStaticCommand.ExecuteAsync(null);
        await fixture.Dialogs.CountdownShown.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using AdapterActionsViewModel other = new(fixture.Static, fixture.Dhcp, fixture.Restore,
            new FakeUserConfirmationService { Answer = true }, new FakeAdapterRefreshCoordinator(), fixture.Coordinator);
        other.Attach(fixture.Adapter);
        await other.ApplyProfileCommand.ExecuteAsync(TestData.Profile());
        Assert.Equal(Strings.SafeSwitchBusy, other.StatusMessage);
        Assert.Single(fixture.Static.Requests);
        Assert.True(fixture.Actions.IsBusy);
        fixture.Countdown.KeepCommand.Execute(null);
        await first;
    }

    private static readonly RecoverySnapshotReference Snapshot = new("snapshot-1", "backup.json");

    private static NetworkApplyResult Mutation(bool attempted) => new(
        new(attempted ? NetworkMutationStepStatus.Succeeded : NetworkMutationStepStatus.NotAttempted),
        new(attempted ? NetworkMutationStepStatus.Failed : NetworkMutationStepStatus.NotAttempted),
        NetworkMutationStepResult.NotAttempted(),
        attempted ? NetworkMutationFailureKind.OperationalFailure : NetworkMutationFailureKind.AdapterUnavailable);

    [Theory]
    [InlineData(StaticIpv4ApplyStatus.PartialFailure)]
    [InlineData(StaticIpv4ApplyStatus.MutationFailed)]
    [InlineData(StaticIpv4ApplyStatus.VerificationFailed)]
    [InlineData(StaticIpv4ApplyStatus.AdapterUnavailableDuringVerification)]
    public async Task StaticMutationAttemptedButUnverified_OpensHonestCountdown(StaticIpv4ApplyStatus status)
    {
        using Fixture fixture = new();
        fixture.Static.Result = new(status, Recovery: Snapshot, Mutation: Mutation(attempted: true));
        await AssertUnverifiedCountdownAsync(fixture, fixture.Actions.ApplyStaticCommand.ExecuteAsync(null),
            Strings.SafeSwitchManualSetting);
    }

    [Theory]
    [InlineData(DhcpApplyStatus.PartialFailure)]
    [InlineData(DhcpApplyStatus.MutationFailed)]
    [InlineData(DhcpApplyStatus.VerificationFailed)]
    [InlineData(DhcpApplyStatus.AdapterUnavailableDuringVerification)]
    public async Task DhcpMutationAttemptedButUnverified_OpensHonestCountdown(DhcpApplyStatus status)
    {
        using Fixture fixture = new();
        fixture.Dhcp.Result = new(status, Recovery: Snapshot, Mutation: Mutation(attempted: true));
        await AssertUnverifiedCountdownAsync(fixture, fixture.Actions.ApplyDhcpCommand.ExecuteAsync(null),
            Strings.SafeSwitchAutomatic);
    }

    private static async Task AssertUnverifiedCountdownAsync(Fixture fixture, Task apply, string settingName)
    {
        await fixture.Dialogs.CountdownShown.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(fixture.Countdown.IsUnverified);
        Assert.Equal(Strings.FormatSafeSwitchUnverifiedCountdownTitle(settingName), fixture.Countdown.Title);
        fixture.Countdown.KeepCommand.Execute(null);
        await apply;
        Assert.Equal(Strings.FormatSafeSwitchKeptUnverified(settingName), fixture.Actions.StatusMessage);
        Assert.Equal(ApplyStatusSeverity.Warning, fixture.Actions.StatusSeverity);
        Assert.True(fixture.Actions.LastSwitchOutcome!.Unverified);
        Assert.False(Assert.Single(fixture.Notifications.History).Succeeded);
        Assert.Empty(fixture.Restore.Requests);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task FailureWithoutSnapshotProtectedMutation_StaysFailedWithoutCountdown(bool snapshot, bool attempted)
    {
        using Fixture fixture = new();
        fixture.Static.Result = new(StaticIpv4ApplyStatus.MutationFailed, Recovery: snapshot ? Snapshot : null,
            Mutation: Mutation(attempted));
        fixture.Dhcp.Result = new(DhcpApplyStatus.MutationFailed, Recovery: snapshot ? Snapshot : null,
            Mutation: Mutation(attempted));
        await fixture.Actions.ApplyStaticCommand.ExecuteAsync(null);
        Assert.Equal(SafeSwitchStatus.ApplyFailed, fixture.Actions.LastSwitchOutcome!.Status);
        await fixture.Actions.ApplyDhcpCommand.ExecuteAsync(null);
        Assert.Equal(SafeSwitchStatus.ApplyFailed, fixture.Actions.LastSwitchOutcome!.Status);
        Assert.Empty(fixture.Dialogs.Countdowns);
    }

    [Fact]
    public async Task UnverifiedTimeout_RevertsToPreviousSetting()
    {
        using Fixture fixture = new();
        fixture.Static.Result = new(StaticIpv4ApplyStatus.VerificationFailed, Recovery: Snapshot, Mutation: Mutation(true));
        Task apply = fixture.Actions.ApplyStaticCommand.ExecuteAsync(null);
        await fixture.Dialogs.CountdownShown.Task.WaitAsync(TimeSpan.FromSeconds(5));
        fixture.Timer.Advance(TimeSpan.FromSeconds(15));
        await apply;
        Assert.Single(fixture.Restore.Requests);
        Assert.Equal(Strings.FormatSafeSwitchReverted(Strings.SafeSwitchManualSetting), fixture.Actions.StatusMessage);
    }

    [Fact]
    public async Task LinkDropDuringCountdown_KeepsCountdownRunningAndResult()
    {
        using Fixture fixture = new();
        Task apply = fixture.Actions.ApplyStaticCommand.ExecuteAsync(null);
        await fixture.Dialogs.CountdownShown.Task.WaitAsync(TimeSpan.FromSeconds(5));
        fixture.Actions.CancelUnavailableAction(adapterRemoved: false);
        Assert.False(fixture.Countdown.Decision.IsCompleted);
        Assert.True(fixture.Coordinator.IsCountdownActive);
        Assert.NotEqual(Strings.AdapterActionUnavailable, fixture.Actions.StatusMessage);
        fixture.Countdown.KeepCommand.Execute(null);
        await apply;
        Assert.Empty(fixture.Restore.Requests);
        Assert.Equal(Strings.FormatSafeSwitchKept(Strings.SafeSwitchManualSetting), fixture.Actions.StatusMessage);
    }

    [Fact]
    public async Task AdapterLostDuringCountdown_RestoreSuccessMessageIsNotOverwritten()
    {
        using Fixture fixture = new();
        Task apply = fixture.Actions.ApplyStaticCommand.ExecuteAsync(null);
        await fixture.Dialogs.CountdownShown.Task.WaitAsync(TimeSpan.FromSeconds(5));
        fixture.Actions.CancelUnavailableAction();
        await apply;
        Assert.Single(fixture.Restore.Requests);
        Assert.Equal(Strings.FormatSafeSwitchReverted(Strings.SafeSwitchManualSetting), fixture.Actions.StatusMessage);
        Assert.Equal(ApplyStatusSeverity.Information, fixture.Actions.StatusSeverity);
    }

    [Fact]
    public async Task ApplyStatic_InvalidDraftAfterEarlierSwitch_ClearsStaleOutcome()
    {
        using Fixture fixture = new();
        Task apply = fixture.Actions.ApplyStaticCommand.ExecuteAsync(null);
        await fixture.Dialogs.CountdownShown.Task.WaitAsync(TimeSpan.FromSeconds(5));
        fixture.Countdown.KeepCommand.Execute(null);
        await apply;
        Assert.NotNull(fixture.Actions.LastSwitchOutcome);
        Assert.NotNull(fixture.Actions.LastStaticApplyResult);
        fixture.Adapter.Draft.Ipv4Address = "invalid";
        await fixture.Actions.ApplyStaticCommand.ExecuteAsync(null);
        Assert.Null(fixture.Actions.LastSwitchOutcome);
        Assert.Null(fixture.Actions.LastStaticApplyResult);
        Assert.Null(fixture.Actions.LastDhcpApplyResult);
    }

    private sealed class Fixture : IDisposable
    {
        public Fixture()
        {
            Timer = new(Clock);
            Coordinator = new(Confirmation, Dialogs, Settings, Restore, Clock, Timer, new FakeUiDispatcher());
            Notifications = new(Settings, Clock);
            Actions = new(Static, Dhcp, Restore, new FakeUserConfirmationService { Answer = true },
                new FakeAdapterRefreshCoordinator(), Coordinator, null, null, operationNotificationService: Notifications);
            Adapter = new(TestData.Snapshot(mode: NetworkConfigurationMode.Static), new FakeClipboardService(), new StaticIpv4ConfigurationValidator());
            Actions.Attach(Adapter);
        }
        public FakeClock Clock { get; } = new(DateTimeOffset.UnixEpoch);
        public FakeTimerService Timer { get; }
        public FakeInternetLossConfirmationService Confirmation { get; } = new();
        public FakeSafetyCountdownDialogService Dialogs { get; } = new();
        public FakeSafeSwitchSettingsRepository Settings { get; } = new();
        public FakeRecoveryRestoreService Restore { get; } = new();
        public FakeStaticIpv4ApplyService Static { get; } = new();
        public FakeDhcpApplyService Dhcp { get; } = new();
        public SafeSwitchCoordinator Coordinator { get; }
        public AdapterActionsViewModel Actions { get; }
        public AdapterViewModel Adapter { get; }
        public OperationNotificationService Notifications { get; }
        public SafetyCountdownViewModel Countdown => Assert.Single(Dialogs.Countdowns);
        public void Dispose() => Actions.Dispose();
    }
}
