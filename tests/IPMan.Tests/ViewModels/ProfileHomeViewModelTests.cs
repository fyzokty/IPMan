using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;
using IPMan.Domain.Settings;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.ViewModels;

public sealed class ProfileHomeViewModelTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(6, false)]
    [InlineData(7, true)]
    public void AutoView_UsesSavedCountAndIgnoresFilter(int count, bool list)
    {
        using HomeFixture fixture = new(Enumerable.Range(0, count).Select(index => TestData.Profile(profileId: index.ToString(System.Globalization.CultureInfo.InvariantCulture), name: $"Setting {index}")).ToArray());
        Assert.Equal(list, fixture.Main.IsListView);
        fixture.Profiles.SearchText = "unmatched";
        Assert.Equal(list, fixture.Main.IsListView);
        Assert.True(Assert.Single(fixture.Main.Settings).IsBuiltIn);
        fixture.Main.ChooseCardViewCommand.Execute(null);
        Assert.Equal(PreferredViewMode.Card, fixture.Main.PreferredViewMode);
        Assert.True(fixture.Main.IsCardView);
        fixture.Catalog.ProfileItems = Enumerable.Range(0, 9).Select(index => TestData.Profile(profileId: index.ToString(System.Globalization.CultureInfo.InvariantCulture))).ToArray();
        fixture.Catalog.RaiseChanged();
        Assert.True(fixture.Main.IsCardView);
    }

    [Theory]
    [InlineData("plc")]
    [InlineData("192.168.2.50")]
    [InlineData("factory")]
    public void Search_FiltersImmediatelyAndKeepsAutomaticFirst(string query)
    {
        using HomeFixture fixture = new([TestData.Profile(profileId: "target", name: "PLC", ipv4Address: "192.168.2.50", description: "Factory"), TestData.Profile(profileId: "other", name: "Office", description: "")]);
        fixture.Profiles.SearchText = query;
        Assert.Equal("target", Assert.Single(fixture.Main.Settings.Skip(1)).StoredSetting!.Id);
        Assert.True(fixture.Main.Settings[0].IsBuiltIn);
        Assert.False(fixture.Main.Settings[0].CanManage);
        Assert.Null(fixture.Main.Settings[0].StoredSetting);
        Assert.Equal(2, fixture.Catalog.ProfileItems.Count);
        Assert.Null(fixture.Profiles.SelectedProfile);
    }

    [Theory]
    [InlineData(InternetConnectivity.Internet, "İnternet çalışıyor")]
    [InlineData(InternetConnectivity.LocalNetwork, "Yerel ağ — internet yok")]
    [InlineData(InternetConnectivity.None, "Yerel ağ — internet yok")]
    [InlineData(InternetConnectivity.Unknown, "İnternet durumu bilinmiyor")]
    public void StatusCard_UsesWindowsConnectivity(InternetConnectivity connectivity, string expected)
    {
        using HomeFixture fixture = new([]);
        fixture.Coordinator.PublishRefresh(TestData.Snapshot() with { InternetConnectivity = connectivity });
        Assert.Equal("Şu an: Otomatik ağındasınız", fixture.Main.StatusHeadline);
        Assert.Equal($"Kablolu bağlantı · {expected}", fixture.Main.StatusDescription);
        Assert.True(fixture.Main.Settings[0].IsActive);
    }

    [Fact]
    public void StatusCard_MatchesSavedValuesAndRefreshesAfterCatalogChanges()
    {
        NetworkProfile profile = TestData.Profile(name: "Ofis", primaryDns: "192.168.1.1", secondaryDns: null);
        using HomeFixture fixture = new([profile]);
        fixture.Coordinator.PublishRefresh(TestData.Snapshot(mode: NetworkConfigurationMode.Static));
        Assert.Equal("Şu an: Ofis ağındasınız", fixture.Main.StatusHeadline);
        Assert.True(fixture.Main.Settings[1].IsActive);
        fixture.Catalog.ProfileItems = [profile with { Name = "Ofis yeni" }];
        fixture.Catalog.RaiseChanged();
        Assert.Contains("Ofis yeni", fixture.Main.StatusHeadline, StringComparison.Ordinal);
        fixture.Profiles.SearchText = "unmatched";
        Assert.Contains("Ofis yeni", fixture.Main.StatusHeadline, StringComparison.Ordinal);
        fixture.Coordinator.PublishRefresh(TestData.Snapshot(mode: NetworkConfigurationMode.Static, primaryDns: "9.9.9.9"));
        Assert.Contains("Elle ayarlanmış adres · 192.168.1.50", fixture.Main.StatusHeadline, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true, true, false, true)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(true, true, true, false)]
    public void CableShortcut_RequiresDisconnectedWiredAndVisibleConnectedWireless(bool cableOut, bool wifiConnected, bool virtualWifi, bool expected)
    {
        using HomeFixture fixture = new([TestData.Profile()]);
        NetworkAdapterSnapshot wired = TestData.Snapshot(isConnected: !cableOut, mode: NetworkConfigurationMode.Static);
        NetworkAdapterSnapshot wireless = TestData.Snapshot(id: "{22222222-2222-3333-4444-555555555555}",
            name: virtualWifi ? "Virtual Wi-Fi" : "Wi-Fi", isConnected: wifiConnected, description: virtualWifi ? "Virtual Wireless Adapter" : "Wireless Adapter");
        fixture.Coordinator.PublishRefresh(wired, wireless);
        Assert.Equal(expected, fixture.Main.HasWirelessShortcut);
        if (cableOut)
        {
            Assert.Equal(Strings.ConnectionStatusCableDisconnected, fixture.Main.StatusHeadline);
            Assert.Equal(Strings.CableDisconnectedGuidance, fixture.Main.StatusDescription);
            Assert.All(fixture.Main.Settings, setting => Assert.True(setting.SwitchCommand.CanExecute(null)));
        }
        if (expected)
        {
            fixture.Main.SwitchToWirelessCommand.Execute(null);
            Assert.Equal(wireless.Id, fixture.Main.SelectedAdapter!.Id);
            Assert.Empty(fixture.Static.Requests);
            Assert.Empty(fixture.Dhcp.Requests);
            Assert.Equal(wireless.Id.Value, fixture.Main.LastSelectedAdapterId);
        }
    }

    [Fact]
    public async Task SwitchStatic_UsesProfileValuesWithConflictConfirmationAndPreservesDirtyDraft()
    {
        NetworkProfile profile = TestData.Profile(ipv4Address: "10.0.0.50", gateway: null);
        using HomeFixture fixture = new([profile]);
        fixture.Main.SelectedAdapter!.Draft.Ipv4Address = "invalid";
        fixture.Static.QueuedResults.Enqueue(new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.ConflictConfirmationRequired));
        fixture.Static.QueuedResults.Enqueue(new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.VerifiedSuccess));
        fixture.Confirmation.Answer = true;
        await fixture.Main.Settings[1].SwitchCommand.ExecuteAsync(null);
        Assert.Equal(2, fixture.Static.Requests.Count);
        Assert.All(fixture.Static.Requests, request => Assert.Equal(profile.ToStaticConfiguration(), request.DesiredConfiguration));
        Assert.False(fixture.Static.Requests[0].ConfirmPotentialConflict);
        Assert.True(fixture.Static.Requests[1].ConfirmPotentialConflict);
        Assert.Empty(fixture.Dhcp.Requests);
        Assert.Equal("invalid", fixture.Main.SelectedAdapter.Draft.Ipv4Address);
        Assert.True(fixture.Main.HasDirtyDrafts);
        Assert.Equal(ApplyStatusSeverity.Success, fixture.Main.Actions.StatusSeverity);
        Assert.Single(fixture.Coordinator.RefreshRequests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SwitchAutomatic_UsesDhcpServiceForBuiltInAndSavedSetting(bool saved)
    {
        using HomeFixture fixture = new([TestData.Profile(mode: NetworkConfigurationMode.Dhcp)]);
        fixture.Coordinator.PublishRefresh(TestData.Snapshot(mode: NetworkConfigurationMode.Static, isConnected: false));
        fixture.Confirmation.Answer = true;
        await fixture.Main.Settings[saved ? 1 : 0].SwitchCommand.ExecuteAsync(null);
        Assert.Equal(fixture.Main.SelectedAdapter!.Id, Assert.Single(fixture.Dhcp.Requests).AdapterId);
        Assert.Empty(fixture.Static.Requests);
    }

    [Fact]
    public void BusyAndActiveSetting_DisableSwitchButtons()
    {
        using HomeFixture fixture = new([TestData.Profile()]);
        Assert.False(fixture.Main.Settings[0].SwitchCommand.CanExecute(null));
        Assert.True(fixture.Main.Settings[1].SwitchCommand.CanExecute(null));
        int notifications = 0;
        fixture.Main.Settings[1].SwitchCommand.CanExecuteChanged += (_, _) => notifications++;
        fixture.Main.Actions.IsBusy = true;
        Assert.False(fixture.Main.IsConnectionSelectionEnabled);
        Assert.All(fixture.Main.Settings, setting => Assert.False(setting.SwitchCommand.CanExecute(null)));
        Assert.False(fixture.Main.Actions.ApplyProfileCommand.CanExecute(null));
        fixture.Main.Actions.IsBusy = false;
        Assert.Equal(2, notifications);
        Assert.True(fixture.Main.Settings[1].SwitchCommand.CanExecute(null));
    }

    [Fact]
    public void EditingAndNewSetting_UseDetailsPageAndBackPreservesDraft()
    {
        using HomeFixture fixture = new([TestData.Profile(ipv4Address: "10.0.0.50")]);
        fixture.Main.EditSettingCommand.Execute(fixture.Profiles.Profiles[0]);
        Assert.True(fixture.Main.IsDetailsOpen);
        Assert.Equal("10.0.0.50", fixture.Main.SelectedAdapter!.Draft.Ipv4Address);
        Assert.Equal(Strings.UpdateSetting, fixture.Profiles.SaveLabel);
        fixture.Main.SelectedAdapter.Draft.Ipv4Address = "10.0.0.51";
        fixture.Main.GoBackCommand.Execute(null);
        Assert.True(fixture.Main.IsHomeVisible);
        Assert.True(fixture.Main.HasDirtyDrafts);
        fixture.Main.NewSettingCommand.Execute(null);
        Assert.True(fixture.Main.IsDetailsOpen);
        Assert.Equal("10.0.0.51", fixture.Main.SelectedAdapter.Draft.Ipv4Address);
        Assert.Empty(fixture.Profiles.NewProfileName);
        Assert.Equal(Strings.CommandSaveProfile, fixture.Profiles.SaveLabel);
        Assert.Empty(fixture.Static.Requests);
        Assert.Empty(fixture.Dhcp.Requests);
    }

    private sealed class HomeFixture : IDisposable
    {
        public HomeFixture(IReadOnlyList<NetworkProfile> profiles)
        {
            Catalog.ProfileItems = profiles;
            FakeWindowProfileDialogs dialogs = new();
            Profiles = new(Catalog, dialogs, dialogs, Confirmation, new FakeUiDispatcher());
            AdapterActionsViewModel actions = new(Static, Dhcp, new FakeRecoveryRestoreService(), Confirmation, Coordinator);
            Main = new(Coordinator, new FakeUiDispatcher(), new FakeClipboardService(),
                new FakeClock(DateTimeOffset.UtcNow), new FakeElevationStateProvider(true), new FakeApplicationVersionProvider("1.0"),
                actions, new StaticIpv4ConfigurationValidator(), Profiles);
            Main.Initialize();
            Coordinator.PublishRefresh(TestData.Snapshot());
        }
        public FakeProfileCatalog Catalog { get; } = new();
        public FakeAdapterRefreshCoordinator Coordinator { get; } = new();
        public FakeStaticIpv4ApplyService Static { get; } = new();
        public FakeDhcpApplyService Dhcp { get; } = new();
        public FakeUserConfirmationService Confirmation { get; } = new();
        public ProfilePanelViewModel Profiles { get; }
        public MainWindowViewModel Main { get; }
        public void Dispose() => Main.Dispose();
    }
}
