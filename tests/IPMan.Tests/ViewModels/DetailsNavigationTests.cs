using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.ViewModels;

public sealed class DetailsNavigationTests
{
    [Theory]
    [InlineData(NetworkConfigurationMode.Static)]
    [InlineData(NetworkConfigurationMode.Dhcp)]
    public void OpenDetails_LoadsSelectedConnectionAndBackDisposesPage(NetworkConfigurationMode mode)
    {
        using HomeFixture fixture = new([]);
        fixture.Coordinator.PublishRefresh(TestData.Snapshot(mode: mode, secondaryDns: "8.8.8.8"));
        fixture.Main.OpenDetailsCommand.Execute(null);
        AdapterDetailsPageViewModel page = fixture.Main.DetailsPage!;
        Assert.True(fixture.Main.IsDetailsOpen);
        Assert.Same(fixture.Main.SelectedAdapter, page.Adapter);
        Assert.Equal(mode, page.Mode);
        Assert.Equal(DraftValues.FromSnapshot(page.Adapter.Snapshot), page.CreateRequest().Values);
        Assert.Equal(0, page.ChangeCount);
        page.BackCommand.Execute(null);
        Assert.True(fixture.Main.IsHomeVisible);
        Assert.Null(fixture.Main.DetailsPage);
        int notifications = 0;
        page.PropertyChanged += (_, _) => notifications++;
        fixture.Coordinator.PublishRefresh(page.Adapter.Snapshot with { Ipv4Address = "192.168.1.70" });
        fixture.Main.Actions.StatusMessage = "Updated";
        Assert.Equal(0, notifications);
        Assert.Empty(fixture.Confirmation.Requests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DirtyBack_UsesUnappliedChangesPromptAndHonorsAnswer(bool accepted)
    {
        using HomeFixture fixture = new([]);
        fixture.Main.OpenDetailsCommand.Execute(null);
        AdapterDetailsPageViewModel page = fixture.Main.DetailsPage!;
        page.IsStatic = true;
        fixture.Confirmation.Answer = accepted;
        page.BackCommand.Execute(null);
        Assert.Equal(accepted, fixture.Main.IsHomeVisible);
        Assert.Equal(Strings.DetailsDiscardMessage, Assert.Single(fixture.Confirmation.Requests).Message);
        if (!accepted) Assert.Same(page, fixture.Main.DetailsPage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Apply_UsesSafetyCoordinatorWithImmutableStaticOrDhcpTarget(bool automatic)
    {
        var previous = TestData.Profile(name: "Ofis", primaryDns: "192.168.1.1", secondaryDns: null);
        using HomeFixture fixture = new([previous]);
        fixture.Coordinator.PublishRefresh(TestData.Snapshot(mode: NetworkConfigurationMode.Static));
        fixture.Confirmation.Answer = true;
        fixture.Safety.PendingOutcome = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Main.OpenDetailsCommand.Execute(null);
        AdapterDetailsPageViewModel page = fixture.Main.DetailsPage!;
        page.IsAutomatic = automatic;
        if (!automatic) page.Fields[0].Value = "192.168.1.60";
        Task applying = page.ApplyCommand.ExecuteAsync(null);
        SafeSwitchRequest request = Assert.Single(fixture.Safety.Requests);
        Assert.Equal(page.Adapter.Id, request.Adapter.Id);
        Assert.Equal(previous.Name, request.PreviousSettingName);
        Assert.Equal(automatic ? Strings.SafeSwitchAutomatic : Strings.SafeSwitchManualSetting, request.SettingName);
        if (automatic)
        {
            Assert.Null(request.Target);
            Assert.Single(fixture.Dhcp.Requests);
            Assert.Empty(fixture.Static.Requests);
        }
        else
        {
            Assert.Equal("192.168.1.60", request.Target!.Ipv4Address);
            Assert.Equal(request.Target, Assert.Single(fixture.Static.Requests).DesiredConfiguration);
            Assert.Empty(fixture.Dhcp.Requests);
        }
        Assert.True(fixture.Safety.IsCountdownActive);
        Assert.True(page.IsBusy);
        Assert.False(page.ApplyCommand.CanExecute(null));
        Assert.False(page.SaveAsSettingCommand.CanExecute(null));
        Assert.False(fixture.Main.IsConnectionSelectionEnabled);
        page.BackCommand.Execute(null);
        Assert.Same(page, fixture.Main.DetailsPage);
        fixture.Safety.PendingOutcome.SetResult(new(SafeSwitchStatus.Kept));
        await applying;
        Assert.False(page.IsBusy);
        Assert.True(page.HasActionStatus);
        Assert.False(page.HasFailure);
        Assert.Equal(Strings.FormatSafeSwitchKept(request.SettingName), fixture.Main.Actions.StatusMessage);
        Assert.False(fixture.Main.HasUnappliedChanges);
        Assert.False(page.ApplyCommand.CanExecute(null));
        fixture.Confirmation.Requests.Clear();
        page.BackCommand.Execute(null);
        Assert.True(fixture.Main.IsHomeVisible);
        Assert.Empty(fixture.Confirmation.Requests);
    }

    [Fact]
    public void Cancel_RestoresLatestValuesAndModeWithoutMutatingNetwork()
    {
        using HomeFixture fixture = new([]);
        fixture.Main.OpenDetailsCommand.Execute(null);
        AdapterDetailsPageViewModel page = fixture.Main.DetailsPage!;
        page.IsStatic = true;
        page.Fields[0].Value = "invalid";
        fixture.Coordinator.PublishRefresh(page.Adapter.Snapshot with { Ipv4Address = "192.168.1.70" });
        Assert.Equal("invalid", page.Fields[0].Value);
        page.CancelCommand.Execute(null);
        Assert.True(page.IsAutomatic);
        Assert.Equal(DraftValues.FromSnapshot(page.Adapter.Snapshot), page.CreateRequest().Values);
        Assert.Equal(0, page.ChangeCount);
        Assert.False(fixture.Main.HasUnappliedChanges);
        Assert.Empty(fixture.Safety.Requests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveAsSetting_OpensPrefilledNewEditorAndSaveReturnsHome(bool automatic)
    {
        using HomeFixture fixture = new([]);
        fixture.Main.OpenDetailsCommand.Execute(null);
        AdapterDetailsPageViewModel page = fixture.Main.DetailsPage!;
        page.IsStatic = true;
        string[] values = ["192.168.1.60", "255.255.255.0", "192.168.1.2", "1.1.1.1", "8.8.8.8"];
        for (int index = 0; index < values.Length; index++) page.Fields[index].Value = values[index];
        page.IsAutomatic = automatic;
        ManualSettingsRequest request = page.CreateRequest();
        page.SaveAsSettingCommand.Execute(null);
        ProfileEditorViewModel editor = fixture.Main.ProfileEditor!;
        Assert.Null(fixture.Main.DetailsPage);
        Assert.True(fixture.Main.IsProfileEditorOpen);
        Assert.Equal(Strings.ProfileEditorNewTitle, editor.Title);
        Assert.Empty(editor.Name);
        Assert.Equal(automatic, editor.IsAutomatic);
        Assert.Equal(request.Values.Ipv4Address, editor.Ipv4Address);
        Assert.Equal(request.Values.SubnetMask, editor.SubnetMask);
        Assert.Equal(request.Values.Gateway, editor.Gateway);
        Assert.Equal(request.Values.PrimaryDns, editor.PrimaryDns);
        Assert.Equal(request.Values.SecondaryDns, editor.SecondaryDns);
        Assert.Empty(fixture.Confirmation.Requests);
        editor.Name = "Saved manual setting";
        await editor.SaveOnlyCommand.ExecuteAsync(null);
        Assert.True(fixture.Main.IsHomeVisible);
        Assert.Equal(request.Mode, Assert.Single(fixture.Catalog.Saved).Mode);
        Assert.Empty(fixture.Safety.Requests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreMutationFailure_PresentsUnchangedPreviousSettingAndRetryUsesSameTarget(bool automatic)
    {
        using HomeFixture fixture = new([TestData.Profile(name: "Ofis", primaryDns: "192.168.1.1", secondaryDns: null)]);
        fixture.Coordinator.PublishRefresh(TestData.Snapshot(mode: NetworkConfigurationMode.Static));
        fixture.Confirmation.Answer = true;
        fixture.Static.Result = new(StaticIpv4ApplyStatus.RecoveryCaptureFailed);
        fixture.Dhcp.Result = new(DhcpApplyStatus.RecoveryCaptureFailed);
        fixture.Main.OpenDetailsCommand.Execute(null);
        AdapterDetailsPageViewModel page = fixture.Main.DetailsPage!;
        page.IsAutomatic = automatic;
        if (!automatic) page.Fields[0].Value = "192.168.1.60";
        await page.ApplyCommand.ExecuteAsync(null);
        Assert.Equal(Strings.FormatDetailsUnchanged("Ofis"), page.Failure!.NetworkState);
        Assert.False(page.HasActionStatus);
        Assert.True(page.RetryCommand.CanExecute(null));
        page.IsStatic = true;
        page.Fields[0].Value = "invalid";
        fixture.Static.Result = new(StaticIpv4ApplyStatus.VerifiedSuccess);
        fixture.Dhcp.Result = new(DhcpApplyStatus.VerifiedSuccess);
        await page.RetryCommand.ExecuteAsync(null);
        Assert.Equal(2, fixture.Safety.Requests.Count);
        Assert.Equal(fixture.Safety.Requests[0].Target, fixture.Safety.Requests[1].Target);
        Assert.Equal(automatic, page.IsAutomatic);
        Assert.False(page.HasFailure);
        Assert.True(page.HasActionStatus);
    }

    [Theory]
    [InlineData(StaticIpv4ApplyStatus.MutationFailed)]
    [InlineData(StaticIpv4ApplyStatus.PartialFailure)]
    [InlineData(StaticIpv4ApplyStatus.VerificationFailed)]
    public async Task FailureAfterPossibleMutation_DoesNotPromiseUnchangedNetwork(StaticIpv4ApplyStatus status)
    {
        using HomeFixture fixture = new([]);
        fixture.Static.Result = new(status);
        fixture.Main.OpenDetailsCommand.Execute(null);
        AdapterDetailsPageViewModel page = fixture.Main.DetailsPage!;
        page.IsStatic = true;
        await page.ApplyCommand.ExecuteAsync(null);
        Assert.True(page.HasFailure);
        Assert.DoesNotContain("Ağınız değişmedi", page.Failure!.NetworkState, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(SafeSwitchStatus.Reverted)]
    [InlineData(SafeSwitchStatus.RevertFailed)]
    public async Task RollbackOutcome_IsShownOnDetailsPage(SafeSwitchStatus status)
    {
        using HomeFixture fixture = new([]);
        fixture.Safety.Outcome = new(status);
        fixture.Main.OpenDetailsCommand.Execute(null);
        AdapterDetailsPageViewModel page = fixture.Main.DetailsPage!;
        page.IsStatic = true;
        await page.ApplyCommand.ExecuteAsync(null);
        if (status == SafeSwitchStatus.Reverted)
        {
            Assert.False(page.HasFailure);
            Assert.True(page.HasActionStatus);
            Assert.Equal(Strings.FormatSafeSwitchReverted(Strings.SafeSwitchAutomatic), fixture.Main.Actions.StatusMessage);
        }
        else
        {
            Assert.True(page.HasFailure);
            Assert.Equal(Strings.DetailsUncertain, page.Failure!.NetworkState);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AdapterDisappears_ReturnsHomeSafelyEvenWhenAnotherConnectionRemains(bool anotherRemains)
    {
        using HomeFixture fixture = new([]);
        fixture.Main.OpenDetailsCommand.Execute(null);
        AdapterDetailsPageViewModel page = fixture.Main.DetailsPage!;
        page.IsStatic = true;
        page.Fields[0].Value = "invalid";
        fixture.Coordinator.PublishRefresh(anotherRemains ? [TestData.Snapshot(id: "{B}")] : []);
        Assert.True(fixture.Main.IsHomeVisible);
        Assert.Null(fixture.Main.DetailsPage);
        page.BackCommand.Execute(null);
        page.SaveAsSettingCommand.Execute(null);
        Assert.True(fixture.Main.IsHomeVisible);
        Assert.Null(fixture.Main.ProfileEditor);
        Assert.Empty(fixture.Confirmation.Requests);
    }

    [Fact]
    public void SelectingAnotherConnection_ReturnsHomeAndDisposesOldPage()
    {
        using HomeFixture fixture = new([]);
        fixture.Coordinator.PublishRefresh(TestData.Snapshot(), TestData.Snapshot(id: "{B}"));
        fixture.Main.OpenDetailsCommand.Execute(null);
        fixture.Main.SelectedAdapter = fixture.Main.Adapters[1];
        Assert.True(fixture.Main.IsHomeVisible);
        Assert.Null(fixture.Main.DetailsPage);
        fixture.Main.OpenDetailsCommand.Execute(null);
        Assert.Same(fixture.Main.Adapters[1], fixture.Main.DetailsPage!.Adapter);
    }

    [Fact]
    public async Task RestoreLast_UsesExistingRestoreWithoutOpeningCountdown()
    {
        using HomeFixture fixture = new([]);
        fixture.Confirmation.Answer = true;
        fixture.Main.OpenDetailsCommand.Execute(null);
        AdapterDetailsPageViewModel page = fixture.Main.DetailsPage!;
        await page.Actions.RestoreLastCommand.ExecuteAsync(null);
        Assert.Equal(page.Adapter.Id, Assert.Single(fixture.Restore.Requests).AdapterId);
        Assert.Empty(fixture.Safety.Requests);
        Assert.True(page.HasActionStatus);
    }

    [Fact]
    public async Task AdapterLostDuringPendingSwitch_LateResultDoesNotRestoreDisposedPage()
    {
        using HomeFixture fixture = new([]);
        fixture.Safety.PendingOutcome = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Main.OpenDetailsCommand.Execute(null);
        AdapterDetailsPageViewModel page = fixture.Main.DetailsPage!;
        page.IsStatic = true;
        Task applying = page.ApplyCommand.ExecuteAsync(null);
        Assert.True(page.IsBusy);
        fixture.Coordinator.PublishRefresh();
        Assert.True(fixture.Main.IsHomeVisible);
        Assert.Null(fixture.Main.DetailsPage);
        fixture.Safety.PendingOutcome.SetResult(new(SafeSwitchStatus.Reverted));
        await applying;
        Assert.True(fixture.Main.IsHomeVisible);
        Assert.Null(fixture.Main.DetailsPage);
        Assert.False(fixture.Main.IsBusy);
    }
}
