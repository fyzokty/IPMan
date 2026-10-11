using System.Windows.Forms;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Presentation;

[Collection(WpfTestCollectionDefinition.Name)]
public sealed class TrayMenuControllerTests
{
    [Fact]
    public void MenuProvider_UsesSelectedConnectionLatestSnapshotAndUnfilteredSettings() => WpfTestRunner.Run(() =>
    {
        using HomeFixture fixture = new([TestData.Profile(name: "Ofis", primaryDns: "192.168.1.1", secondaryDns: null)]);
        using TrayIconManager tray = new(new FakeApplicationVersionProvider("1.0"));
        using TrayMenuController controller = new(fixture.Main, tray, () => { });
        NetworkAdapterSnapshot first = TestData.Snapshot(name: "Birinci");
        NetworkAdapterSnapshot selected = TestData.Snapshot(id: "{22222222-2222-3333-4444-555555555555}",
            name: "İkinci", mode: NetworkConfigurationMode.Static) with { InternetConnectivity = InternetConnectivity.Internet };
        fixture.Coordinator.PublishRefresh(first, selected);
        fixture.Main.SelectAdapter(selected.Id.Value);
        fixture.Profiles.SearchText = "no matching profile";

        TrayMenuModel model = tray.MenuProvider!();
        Assert.Equal(Strings.FormatTrayCurrentStatus("İkinci", AdapterDisplayFormatter.FormatConnectionStatus(selected)), model.StatusText);
        Assert.Equal(TrayStatusColor.Green, model.StatusColor);
        Assert.True(model.Settings.Single(item => item.ProfileId == "profile-1").IsActive);
        Assert.False(model.Settings[0].IsActive);
        Assert.True(model.CanUndo);
        Assert.True(tray.AlwaysVisible);

        selected = selected with { Mode = NetworkConfigurationMode.Dhcp, InternetConnectivity = InternetConnectivity.LocalNetwork };
        fixture.Coordinator.PublishRefresh(first, selected);
        model = tray.MenuProvider();
        Assert.Equal(TrayStatusColor.Amber, model.StatusColor);
        Assert.True(model.Settings[0].IsActive);
        Assert.False(model.Settings[1].IsActive);
        Assert.Equal(selected.Id, fixture.Main.SelectedAdapter!.Id);
    }, "Tray provider test timed out.");

    [Fact]
    public void MenuProvider_WithoutSelectionUsesFirstVisibleConnectionAndHandlesRemoval() => WpfTestRunner.Run(() =>
    {
        using HomeFixture fixture = new([]);
        using TrayIconManager tray = new(new FakeApplicationVersionProvider("1.0"));
        using TrayMenuController controller = new(fixture.Main, tray, () => { });
        fixture.Coordinator.PublishRefresh(TestData.Snapshot(name: "Visible"),
            TestData.Snapshot(id: "{22222222-2222-3333-4444-555555555555}", description: "Hyper-V Virtual Ethernet Adapter"));
        fixture.Main.SelectedAdapter = null;
        TrayMenuModel model = tray.MenuProvider!();
        Assert.Equal("Visible", fixture.Main.SelectedAdapter!.Snapshot.Name);
        Assert.Single(fixture.Main.Adapters);
        Assert.True(model.Settings[0].IsActive);
        Assert.True(model.CanUndo);

        fixture.Coordinator.PublishRefresh();
        model = tray.MenuProvider();
        Assert.Equal(Strings.ConnectionStatusNoConnection, model.StatusText);
        Assert.All(model.Settings, item => Assert.False(item.IsEnabled));
        Assert.False(model.CanUndo);
    }, "Tray fallback test timed out.");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SwitchRequested_ExecutesSharedApplyProfileCommandForSelectedConnection(bool automatic) => WpfTestRunner.Run(() =>
    {
        var profile = TestData.Profile(name: "Saha", ipv4Address: "10.0.0.50");
        using HomeFixture fixture = new([profile]);
        fixture.Confirmation.Answer = true;
        using TrayIconManager tray = new(new FakeApplicationVersionProvider("1.0"));
        int opened = 0;
        using TrayMenuController controller = new(fixture.Main, tray, () => opened++);
        NetworkAdapterSnapshot selected = TestData.Snapshot(id: "{22222222-2222-3333-4444-555555555555}",
            name: "Selected", mode: NetworkConfigurationMode.Static);
        fixture.Coordinator.PublishRefresh(TestData.Snapshot(), selected);
        fixture.Main.SelectAdapter(selected.Id.Value);
        tray.RefreshMenu();
        FindItem(tray, automatic ? Strings.ModeDhcp : profile.Name).PerformClick();

        Assert.True(fixture.Main.Actions.ApplyProfileCommand.ExecutionTask!.IsCompletedSuccessfully);
        SafeSwitchRequest request = Assert.Single(fixture.Safety.Requests);
        Assert.Equal(selected, request.Adapter);
        if (automatic)
        {
            Assert.Null(request.Target);
            Assert.Equal(selected.Id, Assert.Single(fixture.Dhcp.Requests).AdapterId);
            Assert.Empty(fixture.Static.Requests);
        }
        else
        {
            Assert.Equal(profile.Name, request.SettingName);
            Assert.Equal(profile.ToStaticConfiguration(), request.Target);
            Assert.Equal(selected.Id, Assert.Single(fixture.Static.Requests).AdapterId);
            Assert.Empty(fixture.Dhcp.Requests);
        }
        Assert.Equal(0, opened);
    }, "Tray switch test timed out.");

    [Fact]
    public void StaleMenu_WhileBusyIgnoresSwitchAndUndoAndDisablesRefreshedItems() => WpfTestRunner.Run(() =>
    {
        using HomeFixture fixture = new([TestData.Profile()]);
        using TrayIconManager tray = new(new FakeApplicationVersionProvider("1.0"));
        using TrayMenuController controller = new(fixture.Main, tray, () => { });
        fixture.Coordinator.PublishRefresh(TestData.Snapshot(mode: NetworkConfigurationMode.Static));
        tray.RefreshMenu();
        fixture.Main.Actions.IsBusy = true;
        FindItem(tray, "PLC").PerformClick();
        FindItem(tray, Strings.ModeDhcp).PerformClick();
        FindItem(tray, Strings.TrayUndo).PerformClick();
        Assert.Empty(fixture.Safety.Requests);
        Assert.Empty(fixture.Static.Requests);
        Assert.Empty(fixture.Dhcp.Requests);
        Assert.Empty(fixture.Restore.Requests);
        Assert.Empty(fixture.Confirmation.Requests);
        TrayMenuModel model = tray.MenuProvider!();
        Assert.All(model.Settings, item => Assert.False(item.IsEnabled));
        Assert.False(model.CanUndo);
    }, "Tray busy test timed out.");

    [Fact]
    public void StaleMenu_WhenProfileWasDeletedDoesNotApplyAutomaticInstead() => WpfTestRunner.Run(() =>
    {
        using HomeFixture fixture = new([TestData.Profile()]);
        using TrayIconManager tray = new(new FakeApplicationVersionProvider("1.0"));
        using TrayMenuController controller = new(fixture.Main, tray, () => { });
        tray.RefreshMenu();
        fixture.Catalog.Profiles = [];
        fixture.Catalog.RaiseChanged();
        FindItem(tray, "PLC").PerformClick();
        Assert.Empty(fixture.Safety.Requests);
        Assert.Empty(fixture.Dhcp.Requests);
        Assert.Empty(fixture.Static.Requests);
    }, "Tray stale profile test timed out.");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UndoRequested_UsesExistingConfirmationAndRestoreCommand(bool accepted) => WpfTestRunner.Run(() =>
    {
        using HomeFixture fixture = new([]);
        using TrayIconManager tray = new(new FakeApplicationVersionProvider("1.0"));
        using TrayMenuController controller = new(fixture.Main, tray, () => { });
        fixture.Confirmation.Answer = accepted;
        tray.RefreshMenu();
        FindItem(tray, Strings.TrayUndo).PerformClick();
        Assert.True(fixture.Main.Actions.RestoreLastCommand.ExecutionTask!.IsCompletedSuccessfully);
        Assert.Equal(Strings.ConfirmRestoreTitle, Assert.Single(fixture.Confirmation.Requests).Title);
        if (accepted)
            Assert.Equal(fixture.Main.SelectedAdapter!.Id, Assert.Single(fixture.Restore.Requests).AdapterId);
        else
            Assert.Empty(fixture.Restore.Requests);
        Assert.Empty(fixture.Safety.Requests);
    }, "Tray undo test timed out.");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MoreOrOpenRequested_ActivatesWindowAndReturnsHome(bool more) => WpfTestRunner.Run(() =>
    {
        using HomeFixture fixture = new(Enumerable.Range(0, 10).Select(i => TestData.Profile(profileId: $"id-{i}", name: $"Setting {i}")).ToArray());
        using TrayIconManager tray = new(new FakeApplicationVersionProvider("1.0"));
        int opened = 0;
        using TrayMenuController controller = new(fixture.Main, tray, () => opened++);
        fixture.Main.OpenDetailsCommand.Execute(null);
        Assert.True(fixture.Main.IsDetailsOpen);
        tray.RefreshMenu();
        FindItem(tray, more ? Strings.TrayMoreSettings : Strings.TrayOpen).PerformClick();
        Assert.Equal(1, opened);
        Assert.True(fixture.Main.IsHomeVisible);
        Assert.Null(fixture.Main.DetailsPage);
    }, "Tray open test timed out.");

    [Fact]
    public void Dispose_UnsubscribesProviderAndMenuEvents() => WpfTestRunner.Run(() =>
    {
        using HomeFixture fixture = new([TestData.Profile()]);
        using TrayIconManager tray = new(new FakeApplicationVersionProvider("1.0"));
        int opened = 0;
        TrayMenuController controller = new(fixture.Main, tray, () => opened++);
        tray.RefreshMenu();
        controller.Dispose();
        Assert.Null(tray.MenuProvider);
        FindItem(tray, "PLC").PerformClick();
        FindItem(tray, Strings.TrayUndo).PerformClick();
        FindItem(tray, Strings.TrayOpen).PerformClick();
        Assert.Empty(fixture.Safety.Requests);
        Assert.Empty(fixture.Restore.Requests);
        Assert.Equal(0, opened);
    }, "Tray disposal test timed out.");

    private static ToolStripMenuItem FindItem(TrayIconManager tray, string text) =>
        tray.Menu.Items.OfType<ToolStripMenuItem>().Single(item => item.Text == text);
}
