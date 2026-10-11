using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Application.Settings;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Presentation;

public sealed class SafeSwitchCoordinatorTests
{
    [Fact]
    public async Task RunAsync_ConfirmationCancelled_DoesNotApplyOrSaveOrStartCountdown()
    {
        Fixture fixture = new();
        fixture.Confirmation.Result = new(false, true);
        SafeSwitchOutcome outcome = await fixture.Run();
        Assert.Equal(SafeSwitchStatus.Cancelled, outcome.Status);
        Assert.Equal(0, fixture.ApplyCount);
        Assert.Empty(fixture.Settings.Saved);
        Assert.Empty(fixture.Dialogs.Countdowns);
        Assert.Empty(fixture.Restore.Requests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunAsync_ConfirmationAccepted_PersistsOptOutOnlyWhenSelected(bool doNotAsk)
    {
        Fixture fixture = new();
        fixture.Confirmation.Result = new(true, doNotAsk);
        List<IPMan.Domain.Settings.AppSettings> settingsEvents = [];
        fixture.Coordinator.SettingsChanged += (_, settings) => settingsEvents.Add(settings);
        Task<SafeSwitchOutcome> run = fixture.Run();
        Assert.Single(fixture.Confirmation.Requests);
        Assert.Equal(1, fixture.ApplyCount);
        Assert.Equal(doNotAsk, fixture.Settings.Settings.SkipInternetLossConfirmation);
        Assert.Equal(doNotAsk ? 1 : 0, fixture.Settings.Saved.Count);
        Assert.Equal(doNotAsk ? 1 : 0, settingsEvents.Count);
        fixture.Countdown.KeepCommand.Execute(null);
        Assert.Equal(SafeSwitchStatus.Kept, (await run).Status);
        Assert.Empty(fixture.Restore.Requests);
        Assert.False(fixture.Coordinator.IsCountdownActive);
        Assert.Equal(0, fixture.Timer.ActiveSubscriptions);
        fixture.Timer.Advance(TimeSpan.FromSeconds(30));
        Assert.Empty(fixture.Restore.Requests);
    }

    [Fact]
    public async Task RunAsync_PersistedOptOut_SkipsConfirmationButStillStartsCountdown()
    {
        Fixture fixture = new();
        fixture.Settings.Settings = fixture.Settings.Settings with { SkipInternetLossConfirmation = true };
        Task<SafeSwitchOutcome> run = fixture.Run();
        Assert.Empty(fixture.Confirmation.Requests);
        Assert.True(fixture.Coordinator.IsCountdownActive);
        fixture.Countdown.KeepCommand.Execute(null);
        Assert.Equal(SafeSwitchStatus.Kept, (await run).Status);
    }

    [Fact]
    public async Task RunAsync_OptOutSaveFails_ReportsWarningAndProtectsTheSwitch()
    {
        Fixture fixture = new();
        fixture.Confirmation.Result = new(true, true);
        fixture.Settings.SaveResult = SettingsSaveResult.Failed(SettingsSaveStatus.IoFailure);
        Task<SafeSwitchOutcome> run = fixture.Run();
        fixture.Countdown.KeepCommand.Execute(null);
        Assert.True((await run).PreferenceSaveFailed);
        Assert.False(fixture.Settings.Settings.SkipInternetLossConfirmation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunAsync_NonRiskyStaticOrDhcp_SkipsConfirmationAndStillProtectsSwitch(bool dhcp)
    {
        Fixture fixture = new();
        fixture.Request = fixture.Request with
        {
            Target = dhcp ? null : fixture.Request.Target! with { Gateway = "192.168.0.1" }
        };
        Task<SafeSwitchOutcome> run = fixture.Run();
        Assert.Empty(fixture.Confirmation.Requests);
        fixture.Countdown.KeepCommand.Execute(null);
        Assert.Equal(SafeSwitchStatus.Kept, (await run).Status);
    }

    [Fact]
    public async Task RunAsync_FifteenSecondsElapse_RestoresLatestForRequestedAdapter()
    {
        Fixture fixture = new();
        Task<SafeSwitchOutcome> run = fixture.Run();
        fixture.Timer.Advance(TimeSpan.FromSeconds(14));
        Assert.Equal(1, fixture.Countdown.RemainingSeconds);
        Assert.Equal(1d / 15, fixture.Countdown.Progress, 8);
        Assert.Empty(fixture.Restore.Requests);
        fixture.Timer.Advance(TimeSpan.FromSeconds(1));
        SafeSwitchOutcome outcome = await run;
        Assert.Equal(SafeSwitchStatus.Reverted, outcome.Status);
        Assert.Equal(fixture.Request.Adapter.Id, Assert.Single(fixture.Restore.Requests).AdapterId);
        Assert.True(fixture.Restore.Requests[0].ConfirmPotentialConflict);
        Assert.True(fixture.Restore.Requests[0].ContinueAfterIndeterminateProbe);
        Assert.Same(fixture.Restore.Result, outcome.RestoreResult);
        Assert.Equal(0, fixture.Countdown.RemainingSeconds);
        Assert.Equal(0, fixture.Countdown.Progress);
    }

    [Fact]
    public async Task RunAsync_DelayedTimerTick_UsesDeadlineInsteadOfCountingTicks()
    {
        Fixture fixture = new();
        Task<SafeSwitchOutcome> run = fixture.Run();
        fixture.Timer.Advance(TimeSpan.FromSeconds(20));
        Assert.Equal(SafeSwitchStatus.Reverted, (await run).Status);
        Assert.Single(fixture.Restore.Requests);
    }

    [Fact]
    public async Task RunAsync_RevertClicked_RestoresOnlyOnceEvenIfKeepOrTimeoutFollows()
    {
        Fixture fixture = new();
        Task<SafeSwitchOutcome> run = fixture.Run();
        fixture.Countdown.RevertCommand.Execute(null);
        fixture.Countdown.KeepCommand.Execute(null);
        fixture.Timer.Advance(TimeSpan.FromSeconds(15));
        Assert.Equal(SafeSwitchStatus.Reverted, (await run).Status);
        RecoveryRestoreRequest restore = Assert.Single(fixture.Restore.Requests);
        Assert.True(restore.ConfirmPotentialConflict);
        Assert.True(restore.ContinueAfterIndeterminateProbe);
    }

    [Theory]
    [InlineData(RecoveryRestoreStatus.SafetyBlocked)]
    [InlineData(RecoveryRestoreStatus.NoSnapshotFound)]
    [InlineData(RecoveryRestoreStatus.VerificationFailed)]
    public async Task RunAsync_RestoreFails_ReturnsDiagnosticsAndCalmNextStep(RecoveryRestoreStatus status)
    {
        Fixture fixture = new();
        fixture.Restore.Result = new(status);
        Task<SafeSwitchOutcome> run = fixture.Run();
        fixture.Countdown.RevertCommand.Execute(null);
        SafeSwitchOutcome outcome = await run;
        Assert.Equal(SafeSwitchStatus.RevertFailed, outcome.Status);
        Assert.Equal(Strings.SafeSwitchRevertFailed, outcome.Message);
        Assert.Equal(status, outcome.RestoreResult!.Status);
        Assert.False(fixture.Coordinator.IsCountdownActive);
    }

    [Fact]
    public async Task RunAsync_RestoreThrows_ReturnsFailureAndReleasesGuard()
    {
        Fixture fixture = new();
        fixture.Restore.Failure = new InvalidOperationException("restore failure");
        Task<SafeSwitchOutcome> run = fixture.Run();
        fixture.Countdown.RevertCommand.Execute(null);
        SafeSwitchOutcome outcome = await run;
        Assert.Equal(SafeSwitchStatus.RevertFailed, outcome.Status);
        Assert.Same(fixture.Restore.Failure, outcome.Failure);
        Assert.False(fixture.Coordinator.IsCountdownActive);
    }

    [Fact]
    public async Task RunAsync_ApplyFails_DoesNotOpenCountdown()
    {
        Fixture fixture = new() { ApplyResult = SafeSwitchApplyResult.Failed };
        Assert.Equal(SafeSwitchStatus.ApplyFailed, (await fixture.Run()).Status);
        Assert.Empty(fixture.Dialogs.Countdowns);
        Assert.Empty(fixture.Restore.Requests);
    }

    [Fact]
    public async Task RunAsync_NoChange_DoesNotStartCountdownOrRestoreOlderSnapshotAndReleasesGuard()
    {
        Fixture fixture = new() { ApplyResult = SafeSwitchApplyResult.NoChange };
        fixture.Confirmation.Result = new(true, true);
        fixture.Settings.SaveResult = SettingsSaveResult.Failed(SettingsSaveStatus.IoFailure);
        SafeSwitchCoordinator coordinator = fixture.Coordinator;
        SafeSwitchOutcome outcome = await coordinator.RunAsync(fixture.Request,
            _ => Task.FromResult(fixture.ApplyResult), CancellationToken.None);
        Assert.Equal(SafeSwitchStatus.NoChange, outcome.Status);
        Assert.True(outcome.PreferenceSaveFailed);
        Assert.False(coordinator.IsCountdownActive);
        Assert.Empty(fixture.Dialogs.Countdowns);
        Assert.Equal(0, fixture.Timer.ActiveSubscriptions);
        fixture.Timer.Advance(TimeSpan.FromSeconds(30));
        Assert.Empty(fixture.Restore.Requests);
        Assert.True(await coordinator.RequestExitAsync(CancellationToken.None));
        Assert.Equal(0, fixture.Dialogs.ExitPromptCount);

        fixture.ApplyResult = SafeSwitchApplyResult.Applied;
        Task<SafeSwitchOutcome> next = fixture.Run();
        Assert.True(coordinator.IsCountdownActive);
        fixture.Countdown.KeepCommand.Execute(null);
        Assert.Equal(SafeSwitchStatus.Kept, (await next).Status);
        Assert.Empty(fixture.Restore.Requests);
    }

    [Fact]
    public async Task RunAsync_DialogFails_RestoresInsteadOfLeavingUnprotectedMutation()
    {
        Fixture fixture = new();
        fixture.Dialogs.ShowFailure = new InvalidOperationException("dialog failure");
        Assert.Equal(SafeSwitchStatus.Reverted, (await fixture.Run()).Status);
        Assert.Single(fixture.Restore.Requests);
        Assert.Equal(0, fixture.Timer.ActiveSubscriptions);
    }

    [Fact]
    public async Task RunAsync_AnotherCountdownActive_RejectsSecondApply()
    {
        Fixture fixture = new();
        Task<SafeSwitchOutcome> run = fixture.Run();
        Assert.Equal(SafeSwitchStatus.Busy, (await fixture.Run()).Status);
        Assert.Equal(1, fixture.ApplyCount);
        fixture.Countdown.KeepCommand.Execute(null);
        await run;
        Task<SafeSwitchOutcome> next = fixture.Run();
        fixture.Countdown.KeepCommand.Execute(null);
        Assert.Equal(SafeSwitchStatus.Kept, (await next).Status);
    }

    [Theory]
    [InlineData(SafeSwitchExitDecision.Keep, SafeSwitchStatus.Kept, true, 0)]
    [InlineData(SafeSwitchExitDecision.Revert, SafeSwitchStatus.Reverted, true, 1)]
    [InlineData(SafeSwitchExitDecision.Cancel, SafeSwitchStatus.Kept, false, 0)]
    public async Task RequestExitAsync_ActiveCountdown_ResolvesKeepRevertOrCancel(
        SafeSwitchExitDecision decision, SafeSwitchStatus expected, bool canExit, int restores)
    {
        Fixture fixture = new();
        fixture.Dialogs.ExitDecision = decision;
        Task<SafeSwitchOutcome> run = fixture.Run();
        Assert.Equal(canExit, await fixture.Coordinator.RequestExitAsync(CancellationToken.None));
        Assert.Equal(1, fixture.Dialogs.ExitPromptCount);
        if (!canExit)
        {
            Assert.True(fixture.Coordinator.IsCountdownActive);
            fixture.Countdown.KeepCommand.Execute(null);
        }
        Assert.Equal(expected, (await run).Status);
        Assert.Equal(restores, fixture.Restore.Requests.Count);
        if (decision == SafeSwitchExitDecision.Revert)
        {
            RecoveryRestoreRequest restore = Assert.Single(fixture.Restore.Requests);
            Assert.True(restore.ConfirmPotentialConflict);
            Assert.True(restore.ContinueAfterIndeterminateProbe);
        }
    }

    [Fact]
    public async Task RequestExitAsync_NoCountdown_AllowsExitWithoutPrompt()
    {
        Fixture fixture = new();
        Assert.True(await fixture.Coordinator.RequestExitAsync(CancellationToken.None));
        Assert.Equal(0, fixture.Dialogs.ExitPromptCount);
    }

    [Fact]
    public async Task RequestExitAsync_PromptOpen_PausesCountdownSoKeepIsHonoured()
    {
        Fixture fixture = new();
        fixture.Dialogs.ExitDecision = SafeSwitchExitDecision.Keep;
        bool pausedDuringPrompt = false;
        fixture.Dialogs.DuringExitPrompt = () =>
        {
            pausedDuringPrompt = fixture.Countdown.IsPaused;
            fixture.Timer.Advance(TimeSpan.FromSeconds(60));
        };
        Task<SafeSwitchOutcome> run = fixture.Run();
        Assert.True(await fixture.Coordinator.RequestExitAsync(CancellationToken.None));
        Assert.True(pausedDuringPrompt);
        Assert.Equal(SafeSwitchStatus.Kept, (await run).Status);
        Assert.Empty(fixture.Restore.Requests);
    }

    [Fact]
    public async Task RequestExitAsync_PromptCancelled_ResumesWithRemainingTimePreserved()
    {
        Fixture fixture = new();
        Task<SafeSwitchOutcome> run = fixture.Run();
        fixture.Timer.Advance(TimeSpan.FromSeconds(5));
        fixture.Dialogs.DuringExitPrompt = () => fixture.Timer.Advance(TimeSpan.FromSeconds(60));
        Assert.False(await fixture.Coordinator.RequestExitAsync(CancellationToken.None));
        Assert.False(fixture.Countdown.IsPaused);
        Assert.Equal(10, fixture.Countdown.RemainingSeconds);
        Assert.True(fixture.Coordinator.IsCountdownActive);
        fixture.Timer.Advance(TimeSpan.FromSeconds(9));
        Assert.Empty(fixture.Restore.Requests);
        fixture.Timer.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(SafeSwitchStatus.Reverted, (await run).Status);
        Assert.Single(fixture.Restore.Requests);
    }

    [Theory]
    [InlineData(SafeSwitchExitDecision.Keep, false)]
    [InlineData(SafeSwitchExitDecision.Revert, true)]
    public async Task RequestExitAsync_DecidedElsewhereWhilePromptOpen_ExitsOnlyWhenChoiceMatches(
        SafeSwitchExitDecision choice, bool canExit)
    {
        Fixture fixture = new();
        fixture.Dialogs.ExitDecision = choice;
        fixture.Dialogs.DuringExitPrompt = () => fixture.Countdown.Complete(SafetyCountdownDecision.Revert);
        Task<SafeSwitchOutcome> run = fixture.Run();
        Assert.Equal(canExit, await fixture.Coordinator.RequestExitAsync(CancellationToken.None));
        Assert.Equal(SafeSwitchStatus.Reverted, (await run).Status);
        Assert.Single(fixture.Restore.Requests);
    }

    [Fact]
    public async Task RequestExitAsync_ApplyRunning_TellsUserAndKeepsApplying()
    {
        Fixture fixture = new();
        TaskCompletionSource<SafeSwitchApplyResult> applied = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<SafeSwitchOutcome> run = fixture.Coordinator.RunAsync(fixture.Request, _ => applied.Task, CancellationToken.None);
        Assert.False(await fixture.Coordinator.RequestExitAsync(CancellationToken.None));
        Assert.Equal(1, fixture.Dialogs.ExitDeferredCount);
        Assert.Equal(0, fixture.Dialogs.ExitPromptCount);
        applied.SetResult(SafeSwitchApplyResult.Applied);
        await fixture.Dialogs.CountdownShown.Task.WaitAsync(TimeSpan.FromSeconds(5));
        fixture.Countdown.KeepCommand.Execute(null);
        Assert.Equal(SafeSwitchStatus.Kept, (await run).Status);
        Assert.True(await fixture.Coordinator.RequestExitAsync(CancellationToken.None));
        Assert.Equal(1, fixture.Dialogs.ExitDeferredCount);
    }

    [Fact]
    public async Task RunAsync_WallClockJumpsBack_CountdownStillTimesOut()
    {
        Fixture fixture = new();
        Task<SafeSwitchOutcome> run = fixture.Run();
        fixture.Clock.UtcNow -= TimeSpan.FromHours(2);
        fixture.Timer.Advance(TimeSpan.FromSeconds(14));
        Assert.Equal(1, fixture.Countdown.RemainingSeconds);
        fixture.Timer.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(SafeSwitchStatus.Reverted, (await run).Status);
    }

    [Fact]
    public async Task RunAsync_WallClockJumpsForward_DoesNotRevertEarly()
    {
        Fixture fixture = new();
        Task<SafeSwitchOutcome> run = fixture.Run();
        fixture.Clock.UtcNow += TimeSpan.FromHours(2);
        fixture.Timer.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(14, fixture.Countdown.RemainingSeconds);
        Assert.Empty(fixture.Restore.Requests);
        Assert.True(fixture.Coordinator.IsCountdownActive);
        fixture.Countdown.KeepCommand.Execute(null);
        Assert.Equal(SafeSwitchStatus.Kept, (await run).Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunAsync_AppliedUnverified_StartsCountdownWithHonestTitle(bool timeout)
    {
        Fixture fixture = new() { ApplyResult = SafeSwitchApplyResult.AppliedUnverified };
        Task<SafeSwitchOutcome> run = fixture.Run();
        Assert.True(fixture.Coordinator.IsCountdownActive);
        Assert.True(fixture.Countdown.IsUnverified);
        Assert.Equal(Strings.FormatSafeSwitchUnverifiedCountdownTitle("Saha PLC"), fixture.Countdown.Title);
        Assert.Equal(Strings.FormatSafeSwitchCountdownBody(15, "Ofis"), fixture.Countdown.Body);
        if (timeout)
        {
            fixture.Timer.Advance(TimeSpan.FromSeconds(15));
        }
        else
        {
            fixture.Countdown.KeepCommand.Execute(null);
        }
        SafeSwitchOutcome outcome = await run;
        Assert.Equal(timeout ? SafeSwitchStatus.Reverted : SafeSwitchStatus.Kept, outcome.Status);
        Assert.True(outcome.Unverified);
        Assert.Equal(timeout ? 1 : 0, fixture.Restore.Requests.Count);
    }

    [Fact]
    public async Task RunAsync_Applied_IsNotMarkedUnverified()
    {
        Fixture fixture = new();
        Task<SafeSwitchOutcome> run = fixture.Run();
        Assert.False(fixture.Countdown.IsUnverified);
        Assert.Equal(Strings.FormatSafeSwitchCountdownTitle("Saha PLC"), fixture.Countdown.Title);
        fixture.Countdown.KeepCommand.Execute(null);
        Assert.False((await run).Unverified);
    }

    [Fact]
    public async Task RunAsync_CancelledAfterApply_RestoresWithIndependentToken()
    {
        Fixture fixture = new();
        using CancellationTokenSource cancellation = new();
        Task<SafeSwitchOutcome> run = fixture.Coordinator.RunAsync(fixture.Request,
            _ => Task.FromResult(SafeSwitchApplyResult.Applied), cancellation.Token);
        cancellation.Cancel();
        Assert.Equal(SafeSwitchStatus.Reverted, (await run).Status);
        Assert.Single(fixture.Restore.Requests);
    }

    [Fact]
    public async Task RequestExitAsync_RestorePending_WaitsBeforeAllowingExitAndRejectsNewSwitch()
    {
        Fixture fixture = new();
        TaskCompletionSource<RecoveryRestoreResult> restored = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Restore.PendingResult = restored.Task;
        Task<SafeSwitchOutcome> run = fixture.Run();
        fixture.Countdown.RevertCommand.Execute(null);
        await fixture.Restore.RestoreStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(fixture.Coordinator.IsCountdownActive);
        Task<bool> exit = fixture.Coordinator.RequestExitAsync(CancellationToken.None);
        Assert.False(exit.IsCompleted);
        Assert.Equal(SafeSwitchStatus.Busy, (await fixture.Run()).Status);
        restored.SetResult(new(RecoveryRestoreStatus.VerifiedSuccess));
        Assert.True(await exit);
        Assert.Equal(SafeSwitchStatus.Reverted, (await run).Status);
        Assert.Equal(0, fixture.Dialogs.ExitPromptCount);
        Assert.Single(fixture.Restore.Requests);
    }

    [Fact]
    public async Task RequestExitAsync_ApplyPending_BlocksExitAndAnotherSwitch()
    {
        Fixture fixture = new();
        TaskCompletionSource<SafeSwitchApplyResult> applied = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<SafeSwitchOutcome> run = fixture.Coordinator.RunAsync(fixture.Request, _ => applied.Task, CancellationToken.None);
        Assert.False(fixture.Coordinator.IsCountdownActive);
        Assert.False(await fixture.Coordinator.RequestExitAsync(CancellationToken.None));
        Assert.Equal(SafeSwitchStatus.Busy, (await fixture.Run()).Status);
        applied.SetResult(SafeSwitchApplyResult.Applied);
        await fixture.Dialogs.CountdownShown.Task.WaitAsync(TimeSpan.FromSeconds(5));
        fixture.Countdown.KeepCommand.Execute(null);
        Assert.Equal(SafeSwitchStatus.Kept, (await run).Status);
    }

    [Fact]
    public async Task RunAsync_ApplyThrows_ReturnsFailureWithoutCountdown()
    {
        Fixture fixture = new();
        InvalidOperationException failure = new("apply failure");
        SafeSwitchOutcome outcome = await fixture.Coordinator.RunAsync(fixture.Request,
            _ => Task.FromException<SafeSwitchApplyResult>(failure), CancellationToken.None);
        Assert.Equal(SafeSwitchStatus.ApplyFailed, outcome.Status);
        Assert.Same(failure, outcome.Failure);
        Assert.Empty(fixture.Dialogs.Countdowns);
    }

    [Fact]
    public async Task RunAsync_CancelledBeforeApply_DoesNotPromptOrMutate()
    {
        Fixture fixture = new();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        SafeSwitchOutcome outcome = await fixture.Coordinator.RunAsync(fixture.Request,
            _ => throw new InvalidOperationException("must not apply"), cancellation.Token);
        Assert.Equal(SafeSwitchStatus.Cancelled, outcome.Status);
        Assert.Empty(fixture.Confirmation.Requests);
        Assert.Empty(fixture.Dialogs.Countdowns);
    }

    [Fact]
    public async Task RequestExitAsync_CancelledPrompt_KeepsCountdownRunning()
    {
        Fixture fixture = new();
        Task<SafeSwitchOutcome> run = fixture.Run();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        Assert.False(await fixture.Coordinator.RequestExitAsync(cancellation.Token));
        Assert.True(fixture.Coordinator.IsCountdownActive);
        fixture.Countdown.KeepCommand.Execute(null);
        Assert.Equal(SafeSwitchStatus.Kept, (await run).Status);
    }

    [Fact]
    public async Task RunAsync_RestoreAlreadyMatchesSnapshot_ReturnsReverted()
    {
        Fixture fixture = new();
        fixture.Restore.Result = new(RecoveryRestoreStatus.NoChange);
        Task<SafeSwitchOutcome> run = fixture.Run();
        fixture.Countdown.RevertCommand.Execute(null);
        Assert.Equal(SafeSwitchStatus.Reverted, (await run).Status);
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            Timer = new(Clock);
            Coordinator = new(Confirmation, Dialogs, Settings, Restore, Clock, Timer, new FakeUiDispatcher());
        }
        public FakeClock Clock { get; } = new(DateTimeOffset.UnixEpoch);
        public FakeTimerService Timer { get; }
        public FakeInternetLossConfirmationService Confirmation { get; } = new();
        public FakeSafetyCountdownDialogService Dialogs { get; } = new();
        public FakeSafeSwitchSettingsRepository Settings { get; } = new();
        public FakeRecoveryRestoreService Restore { get; } = new();
        public SafeSwitchCoordinator Coordinator { get; }
        public SafetyCountdownViewModel Countdown => Dialogs.Countdowns[^1];
        public int ApplyCount { get; private set; }
        public SafeSwitchApplyResult ApplyResult { get; set; } = SafeSwitchApplyResult.Applied;
        public SafeSwitchRequest Request { get; set; } = new(
            TestData.Snapshot() with { InternetConnectivity = InternetConnectivity.Internet }, "Saha PLC", "Ofis",
            new("192.168.0.50", "255.255.255.0", null, null, null));

        public Task<SafeSwitchOutcome> Run() => Coordinator.RunAsync(Request, _ =>
        {
            ApplyCount++;
            return Task.FromResult(ApplyResult);
        }, CancellationToken.None);
    }
}
