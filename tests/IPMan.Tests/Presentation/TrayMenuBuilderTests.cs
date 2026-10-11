using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;
using Xunit;

namespace IPMan.Tests.Presentation;

public sealed class TrayMenuBuilderTests
{
    [Fact]
    public void Build_OrdersAutomaticThenFavoritesThenOthersByName()
    {
        NetworkProfile[] profiles =
        [
            TestData.Profile(profileId: "z", name: "Z"),
            TestData.Profile(profileId: "b", name: "B", isFavorite: true),
            TestData.Profile(profileId: "a", name: "A"),
            TestData.Profile(profileId: "c", name: "C", isFavorite: true)
        ];
        TrayMenuModel model = new TrayMenuBuilder().Build(TestData.Snapshot(), profiles, false, false);
        Assert.Equal(new string?[] { null, "b", "c", "a", "z" }, model.Settings.Select(item => item.ProfileId));
        Assert.Equal(Strings.ModeDhcp, model.Settings[0].Name);
    }

    [Theory]
    [InlineData(0, 1, false)]
    [InlineData(9, 10, false)]
    [InlineData(10, 10, true)]
    [InlineData(25, 10, true)]
    public void Build_LimitsSettingsIncludingAutomatic(int count, int expected, bool more)
    {
        TrayMenuModel model = new TrayMenuBuilder().Build(TestData.Snapshot(),
            Enumerable.Range(0, count).Select(i => TestData.Profile(profileId: i.ToString(System.Globalization.CultureInfo.InvariantCulture))), false, false);
        Assert.Equal(expected, model.Settings.Count);
        Assert.Equal(more, model.HasMoreSettings);
    }

    [Theory]
    [InlineData(NetworkConfigurationMode.Dhcp, true, null)]
    [InlineData(NetworkConfigurationMode.Static, true, "match")]
    [InlineData(NetworkConfigurationMode.Static, false, null)]
    [InlineData(NetworkConfigurationMode.Unknown, true, null)]
    public void Build_ChecksOnlyMatchingSetting(NetworkConfigurationMode mode, bool matches, string? expectedId)
    {
        NetworkProfile profile = TestData.Profile(profileId: "match", primaryDns: "192.168.1.1", secondaryDns: null,
            ipv4Address: matches ? "192.168.1.50" : "192.168.1.99");
        TrayMenuModel model = new TrayMenuBuilder().Build(TestData.Snapshot(mode: mode), [profile], false, false);
        TraySettingItem? active = model.Settings.SingleOrDefault(item => item.IsActive);
        if (mode == NetworkConfigurationMode.Dhcp || expectedId is not null)
        {
            Assert.NotNull(active);
            Assert.Equal(expectedId, active.ProfileId);
            Assert.False(active.IsEnabled);
        }
        else
        {
            Assert.Null(active);
        }
    }

    [Theory]
    [InlineData(InternetConnectivity.Internet, true, AdapterMedium.Wired, TrayStatusColor.Green, "İnternet çalışıyor")]
    [InlineData(InternetConnectivity.LocalNetwork, true, AdapterMedium.Wired, TrayStatusColor.Amber, "Yerel ağ — internet yok")]
    [InlineData(InternetConnectivity.None, true, AdapterMedium.Wired, TrayStatusColor.Amber, "Yerel ağ — internet yok")]
    [InlineData(InternetConnectivity.Internet, false, AdapterMedium.Wired, TrayStatusColor.Gray, "Kablo takılı değil")]
    [InlineData(InternetConnectivity.None, false, AdapterMedium.Wireless, TrayStatusColor.Gray, "Bağlantı yok")]
    [InlineData(InternetConnectivity.Unknown, true, AdapterMedium.Wired, TrayStatusColor.Gray, "Bağlı")]
    public void Build_FormatsConnectionStatus(InternetConnectivity connectivity, bool connected, AdapterMedium medium,
        TrayStatusColor color, string expected)
    {
        NetworkAdapterSnapshot snapshot = TestData.Snapshot(name: "Ofis", isConnected: connected) with
        { InternetConnectivity = connectivity, Medium = medium };
        TrayMenuModel model = new TrayMenuBuilder().Build(snapshot, [], false, false);
        Assert.Equal($"Şu an: Ofis · {expected}", model.StatusText);
        Assert.Equal(color, model.StatusColor);
        Assert.Equal(Strings.ApplicationName, model.Title);
    }

    [Fact]
    public void Build_NoAdapter_DisablesSwitchesAndShowsPlainStatus()
    {
        TrayMenuModel model = new TrayMenuBuilder().Build(null, [TestData.Profile()], false, false);
        Assert.Equal(Strings.ConnectionStatusNoConnection, model.StatusText);
        Assert.Equal(TrayStatusColor.Gray, model.StatusColor);
        Assert.All(model.Settings, item => { Assert.False(item.IsEnabled); Assert.False(item.IsActive); });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Build_BusyAndUndoAvailability(bool busy, bool canUndo)
    {
        TrayMenuModel model = new TrayMenuBuilder().Build(TestData.Snapshot(mode: NetworkConfigurationMode.Static,
            isConnected: false), [TestData.Profile()], canUndo, busy);
        Assert.All(model.Settings, item => Assert.Equal(!busy, item.IsEnabled));
        Assert.Equal(canUndo && !busy, model.CanUndo);
    }
}

