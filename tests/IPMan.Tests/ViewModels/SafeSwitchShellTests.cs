using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.ViewModels;

public sealed class SafeSwitchShellTests
{
    private const string OtherAdapterId = "{22222222-3333-4444-5555-666666666666}";

    [Fact]
    public async Task LinkDropDuringSwitch_DoesNotCancelButAdapterRemovalDoes()
    {
        using HomeFixture fixture = new([]);
        fixture.Safety.PendingOutcome = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task apply = fixture.Main.Actions.ApplyProfileCommand.ExecuteAsync(TestData.Profile(name: "Saha"));
        Assert.True(fixture.Main.IsBusy);

        fixture.Coordinator.PublishRefresh(TestData.Snapshot(isConnected: false));
        Assert.False(fixture.Safety.LastToken.IsCancellationRequested);
        Assert.NotEqual(Strings.AdapterActionUnavailable, fixture.Main.Actions.StatusMessage);

        fixture.Coordinator.PublishRefresh();
        Assert.True(fixture.Safety.LastToken.IsCancellationRequested);
        fixture.Safety.PendingOutcome.SetResult(new(SafeSwitchStatus.RevertFailed, Strings.SafeSwitchRevertFailed));
        await apply;
        Assert.Equal(Strings.SafeSwitchRevertFailed, fixture.Main.Actions.StatusMessage);
    }

    [Fact]
    public async Task SelectAdapter_WhileBusy_IsIgnored()
    {
        using HomeFixture fixture = new([]);
        fixture.Coordinator.PublishRefresh(TestData.Snapshot(), TestData.Snapshot(id: OtherAdapterId, name: "Wi-Fi"));
        AdapterViewModel selected = fixture.Main.SelectedAdapter!;
        fixture.Safety.PendingOutcome = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task apply = fixture.Main.Actions.ApplyProfileCommand.ExecuteAsync(TestData.Profile());
        fixture.Main.SelectAdapter(OtherAdapterId);
        Assert.Same(selected, fixture.Main.SelectedAdapter);
        Assert.Same(selected, fixture.Main.Actions.CurrentAdapter);
        fixture.Safety.PendingOutcome.SetResult(new(SafeSwitchStatus.Kept));
        await apply;
        fixture.Main.SelectAdapter(OtherAdapterId);
        Assert.Equal(OtherAdapterId, fixture.Main.SelectedAdapter!.Id.Value, ignoreCase: true);
    }

    [Fact]
    public async Task DetailsApply_KeptButUnverified_ShowsHonestBand()
    {
        using HomeFixture fixture = new([]);
        fixture.Coordinator.PublishRefresh(TestData.Snapshot(mode: NetworkConfigurationMode.Static));
        NetworkApplyResult mutation = new(new(NetworkMutationStepStatus.Succeeded), new(NetworkMutationStepStatus.Failed),
            NetworkMutationStepResult.NotAttempted(), NetworkMutationFailureKind.OperationalFailure);
        fixture.Static.Result = new(StaticIpv4ApplyStatus.PartialFailure,
            Recovery: new("snapshot-1", "backup.json"), Mutation: mutation);
        fixture.Safety.Outcome = new(SafeSwitchStatus.Kept, Unverified: true);
        fixture.Main.OpenDetailsCommand.Execute(null);
        AdapterDetailsPageViewModel page = fixture.Main.DetailsPage!;
        page.Fields[0].Value = "192.168.1.60";
        await page.ApplyCommand.ExecuteAsync(null);
        Assert.Equal(SafeSwitchApplyResult.AppliedUnverified, Assert.Single(fixture.Safety.ApplyResults));
        Assert.True(page.HasFailure);
        Assert.Equal(Strings.FormatDetailsUnverifiedTitle(Strings.SafeSwitchManualSetting), page.Failure!.Title);
        Assert.Equal(Strings.DetailsPartial, page.Failure.NetworkState);
        Assert.Equal(Strings.FormatSafeSwitchKeptUnverified(Strings.SafeSwitchManualSetting), fixture.Main.Actions.StatusMessage);
    }
}
