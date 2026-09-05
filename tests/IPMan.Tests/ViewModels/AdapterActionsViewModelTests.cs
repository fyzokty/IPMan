using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.ViewModels;

public sealed class AdapterActionsViewModelTests
{
    [Fact]
    public async Task ApplyStatic_WithInvalidDraft_ShowsErrorsWithoutCallingServiceOrRefreshing()
    {
        using ActionsContext context = CreateContext();
        context.Adapter.Draft.Ipv4Address = string.Empty;
        context.Adapter.Draft.SubnetMask = string.Empty;

        await context.Actions.ApplyStaticCommand.ExecuteAsync(null);

        Assert.Empty(context.StaticApply.Requests);
        Assert.Equal(ApplyStatusSeverity.Error, context.Actions.StatusSeverity);
        Assert.Contains(Strings.ApplyValidationFailed, context.Actions.StatusMessage, StringComparison.Ordinal);
        Assert.NotEmpty(context.Adapter.Draft.Ipv4AddressError);
        Assert.NotEmpty(context.Adapter.Draft.SubnetMaskError);
        Assert.Empty(context.RefreshCoordinator.RefreshRequests);
    }

    [Fact]
    public async Task ApplyStatic_WithValidDraft_SendsNormalizedConfigurationAndCleansDraft()
    {
        using ActionsContext context = CreateContext();
        context.Adapter.Draft.Ipv4Address = " 192.168.20.25 ";
        context.Adapter.Draft.SubnetMask = " 255.255.255.0 ";
        context.Adapter.Draft.Gateway = " 192.168.20.1 ";
        context.Adapter.Draft.PrimaryDns = " 1.1.1.1 ";
        context.Adapter.Draft.SecondaryDns = string.Empty;

        await context.Actions.ApplyStaticCommand.ExecuteAsync(null);

        StaticIpv4ApplyRequest request = Assert.Single(context.StaticApply.Requests);
        Assert.Equal(context.Adapter.Id, request.AdapterId);
        Assert.Equal("192.168.20.25", request.DesiredConfiguration.Ipv4Address);
        Assert.Equal("255.255.255.0", request.DesiredConfiguration.SubnetMask);
        Assert.Equal("192.168.20.1", request.DesiredConfiguration.Gateway);
        Assert.Equal("1.1.1.1", request.DesiredConfiguration.PrimaryDns);
        Assert.Null(request.DesiredConfiguration.SecondaryDns);
        Assert.Equal(Strings.ApplyStaticSuccess, context.Actions.StatusMessage);
        Assert.Equal(ApplyStatusSeverity.Success, context.Actions.StatusSeverity);
        Assert.False(context.Adapter.Draft.IsDirty);
        AssertConfigurationRefreshRequested(context);
    }

    [Fact]
    public async Task ApplyStatic_WhenNoChange_ReportsNoChangeAndCleansDraftAfterOneCall()
    {
        using ActionsContext context = CreateContext();
        context.StaticApply.Result = new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.NoChange);
        context.Adapter.Draft.Ipv4Address = "192.168.1.60";

        await context.Actions.ApplyStaticCommand.ExecuteAsync(null);

        Assert.Single(context.StaticApply.Requests);
        Assert.Equal(Strings.ApplyStaticNoChange, context.Actions.StatusMessage);
        Assert.False(context.Adapter.Draft.IsDirty);
    }

    [Fact]
    public async Task ApplyStatic_WhenConflictIsAccepted_RetriesWithConflictFlag()
    {
        using ActionsContext context = CreateContext();
        context.StaticApply.QueuedResults.Enqueue(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.ConflictConfirmationRequired));
        context.StaticApply.QueuedResults.Enqueue(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.VerifiedSuccess));
        context.Confirmation.Answers.Enqueue(true);

        await context.Actions.ApplyStaticCommand.ExecuteAsync(null);

        Assert.Equal(2, context.StaticApply.Requests.Count);
        Assert.False(context.StaticApply.Requests[0].ConfirmPotentialConflict);
        Assert.True(context.StaticApply.Requests[1].ConfirmPotentialConflict);
        Assert.Single(context.Confirmation.Requests);
        AssertConfigurationRefreshRequested(context);
    }

    [Fact]
    public async Task ApplyStatic_WhenConflictIsDeclined_DoesNotRetryAndReportsDecline()
    {
        using ActionsContext context = CreateContext();
        context.StaticApply.Result = new StaticIpv4ApplyResult(
            StaticIpv4ApplyStatus.ConflictConfirmationRequired);
        context.Confirmation.Answers.Enqueue(false);

        await context.Actions.ApplyStaticCommand.ExecuteAsync(null);

        Assert.Single(context.StaticApply.Requests);
        Assert.Equal(Strings.ActionDeclined, context.Actions.StatusMessage);
        Assert.Equal(ApplyStatusSeverity.Information, context.Actions.StatusSeverity);
        Assert.Empty(context.RefreshCoordinator.RefreshRequests);
    }

    [Fact]
    public async Task ApplyStatic_WhenProbeIsIndeterminateAndAccepted_RetriesWithProbeFlag()
    {
        using ActionsContext context = CreateContext();
        context.StaticApply.QueuedResults.Enqueue(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.ProbeIndeterminateConfirmationRequired));
        context.StaticApply.QueuedResults.Enqueue(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.VerifiedSuccess));
        context.Confirmation.Answers.Enqueue(true);

        await context.Actions.ApplyStaticCommand.ExecuteAsync(null);

        Assert.Equal(2, context.StaticApply.Requests.Count);
        Assert.False(context.StaticApply.Requests[0].ContinueAfterIndeterminateProbe);
        Assert.True(context.StaticApply.Requests[1].ContinueAfterIndeterminateProbe);
    }

    [Fact]
    public async Task ApplyStatic_WithTwoConfirmationStatuses_AccumulatesFlagsAndStopsAfterThirdCall()
    {
        using ActionsContext context = CreateContext();
        context.StaticApply.QueuedResults.Enqueue(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.ConflictConfirmationRequired));
        context.StaticApply.QueuedResults.Enqueue(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.ProbeIndeterminateConfirmationRequired));
        context.StaticApply.QueuedResults.Enqueue(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.VerifiedSuccess));
        context.StaticApply.Result = new StaticIpv4ApplyResult(
            StaticIpv4ApplyStatus.ConflictConfirmationRequired);
        context.Confirmation.Answers.Enqueue(true);
        context.Confirmation.Answers.Enqueue(true);

        await context.Actions.ApplyStaticCommand.ExecuteAsync(null);

        Assert.Equal(3, context.StaticApply.Requests.Count);
        StaticIpv4ApplyRequest third = context.StaticApply.Requests[2];
        Assert.True(third.ConfirmPotentialConflict);
        Assert.True(third.ContinueAfterIndeterminateProbe);
    }

    [Fact]
    public async Task ApplyDhcp_SendsAdapterIdentityWithoutConfirmationAndRefreshes()
    {
        using ActionsContext context = CreateContext();

        await context.Actions.ApplyDhcpCommand.ExecuteAsync(null);

        DhcpApplyRequest request = Assert.Single(context.DhcpApply.Requests);
        Assert.Equal(context.Adapter.Id, request.AdapterId);
        Assert.Empty(context.Confirmation.Requests);
        Assert.Equal(Strings.ApplyDhcpSuccess, context.Actions.StatusMessage);
        AssertConfigurationRefreshRequested(context);
    }

    [Fact]
    public async Task RestoreLast_AsksFirstAndDeclineSkipsService()
    {
        using ActionsContext context = CreateContext();
        context.Confirmation.Answer = false;

        await context.Actions.RestoreLastCommand.ExecuteAsync(null);

        Assert.Single(context.Confirmation.Requests);
        Assert.Empty(context.RecoveryRestore.Requests);
        Assert.Equal(Strings.ActionDeclined, context.Actions.StatusMessage);
        Assert.Empty(context.RefreshCoordinator.RefreshRequests);
    }

    [Fact]
    public async Task RestoreLast_WhenConflictIsDeclined_DoesNotRetryOrRefresh()
    {
        using ActionsContext context = CreateContext();
        context.RecoveryRestore.Result = new RecoveryRestoreResult(
            RecoveryRestoreStatus.ConflictConfirmationRequired);
        context.Confirmation.Answers.Enqueue(true);
        context.Confirmation.Answers.Enqueue(false);

        await context.Actions.RestoreLastCommand.ExecuteAsync(null);

        Assert.Single(context.RecoveryRestore.Requests);
        Assert.Equal(2, context.Confirmation.Requests.Count);
        Assert.Equal(Strings.ActionDeclined, context.Actions.StatusMessage);
        Assert.Empty(context.RefreshCoordinator.RefreshRequests);
        Assert.False(context.Actions.IsBusy);
    }

    [Fact]
    public async Task RestoreLast_WhenConflictIsAccepted_RetriesAndRefreshes()
    {
        using ActionsContext context = CreateContext();
        context.RecoveryRestore.QueuedResults.Enqueue(
            new RecoveryRestoreResult(RecoveryRestoreStatus.ConflictConfirmationRequired));
        context.RecoveryRestore.QueuedResults.Enqueue(
            new RecoveryRestoreResult(RecoveryRestoreStatus.VerifiedSuccess));
        context.Confirmation.Answers.Enqueue(true);
        context.Confirmation.Answers.Enqueue(true);

        await context.Actions.RestoreLastCommand.ExecuteAsync(null);

        Assert.Equal(2, context.RecoveryRestore.Requests.Count);
        Assert.False(context.RecoveryRestore.Requests[0].ConfirmPotentialConflict);
        Assert.True(context.RecoveryRestore.Requests[1].ConfirmPotentialConflict);
        Assert.Equal(2, context.Confirmation.Requests.Count);
        AssertConfigurationRefreshRequested(context);
    }

    [Fact]
    public async Task RestoreLast_WhenNoSnapshotExists_UsesRestoreOutcomeAndRefreshes()
    {
        using ActionsContext context = CreateContext();
        context.Confirmation.Answer = true;
        context.RecoveryRestore.Result = new RecoveryRestoreResult(
            RecoveryRestoreStatus.NoSnapshotFound);

        await context.Actions.RestoreLastCommand.ExecuteAsync(null);

        Assert.Single(context.RecoveryRestore.Requests);
        Assert.Equal(Strings.RestoreNoSnapshotFound, context.Actions.StatusMessage);
        Assert.NotEqual(Strings.ActionDeclined, context.Actions.StatusMessage);
        AssertConfigurationRefreshRequested(context);
    }

    [Fact]
    public void WithoutCurrentAdapter_AllCommandsAreDisabled()
    {
        using ActionsContext context = CreateContext(attachAdapter: false);

        Assert.False(context.Actions.ApplyStaticCommand.CanExecute(null));
        Assert.False(context.Actions.ApplyDhcpCommand.CanExecute(null));
        Assert.False(context.Actions.RestoreLastCommand.CanExecute(null));
    }

    [Fact]
    public void WhileBusy_AllCommandsAreDisabled()
    {
        using ActionsContext context = CreateContext();

        context.Actions.IsBusy = true;

        Assert.False(context.Actions.ApplyStaticCommand.CanExecute(null));
        Assert.False(context.Actions.ApplyDhcpCommand.CanExecute(null));
        Assert.False(context.Actions.RestoreLastCommand.CanExecute(null));
    }

    private static ActionsContext CreateContext(bool attachAdapter = true)
    {
        FakeStaticIpv4ApplyService staticApply = new();
        FakeDhcpApplyService dhcpApply = new();
        FakeRecoveryRestoreService recoveryRestore = new();
        FakeUserConfirmationService confirmation = new();
        FakeAdapterRefreshCoordinator refreshCoordinator = new();
        AdapterActionsViewModel actions = new(
            staticApply,
            dhcpApply,
            recoveryRestore,
            confirmation,
            refreshCoordinator);
        AdapterViewModel adapter = new(
            TestData.Snapshot(id: "{A}"),
            new FakeClipboardService(),
            new StaticIpv4ConfigurationValidator());

        if (attachAdapter)
        {
            actions.Attach(adapter);
        }

        return new ActionsContext(
            actions,
            adapter,
            staticApply,
            dhcpApply,
            recoveryRestore,
            confirmation,
            refreshCoordinator);
    }

    private static void AssertConfigurationRefreshRequested(ActionsContext context) =>
        Assert.Equal(
            new[] { NetworkChangeReason.ConfigurationApplied },
            context.RefreshCoordinator.RefreshRequests);

    private sealed record ActionsContext(
        AdapterActionsViewModel Actions,
        AdapterViewModel Adapter,
        FakeStaticIpv4ApplyService StaticApply,
        FakeDhcpApplyService DhcpApply,
        FakeRecoveryRestoreService RecoveryRestore,
        FakeUserConfirmationService Confirmation,
        FakeAdapterRefreshCoordinator RefreshCoordinator) : IDisposable
    {
        public void Dispose() => Actions.Dispose();
    }
}
