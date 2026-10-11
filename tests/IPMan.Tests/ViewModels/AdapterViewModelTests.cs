using IPMan.App.Presentation;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.ViewModels;

public sealed class AdapterViewModelTests
{
    [Fact]
    public void Update_WithChangedSnapshot_NotifiesSnapshotAfterDerivedStateIsCurrent()
    {
        AdapterViewModel adapter = Create(TestData.Snapshot(id: "{A}", mode: NetworkConfigurationMode.Static));
        NetworkAdapterSnapshot next = TestData.Snapshot(
            id: "{A}",
            isConnected: false,
            mode: NetworkConfigurationMode.Dhcp,
            ipv4Address: "10.0.0.7");
        List<string?> observed = new();
        adapter.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(AdapterViewModel.Snapshot))
            {
                return;
            }

            observed.Add(e.PropertyName);
            Assert.Same(next, adapter.Snapshot);
            Assert.False(adapter.IsConnected);
            Assert.Equal("Kablo takılı değil", adapter.ConnectionState);
            Assert.Equal(
                AdapterDisplayFormatter.FormatConfigurationMode(NetworkConfigurationMode.Dhcp),
                adapter.Details.ConfigurationMode);
            Assert.Equal("10.0.0.7", adapter.Draft.Ipv4Address);
        };

        adapter.Update(next);

        Assert.Single(observed);
    }

    [Fact]
    public void Update_WithEqualSnapshot_DoesNotNotifySnapshot()
    {
        AdapterViewModel adapter = Create(TestData.Snapshot(id: "{A}"));
        int notifications = 0;
        adapter.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AdapterViewModel.Snapshot))
            {
                notifications++;
            }
        };

        adapter.Update(TestData.Snapshot(id: "{A}"));

        Assert.Equal(0, notifications);
    }

    private static AdapterViewModel Create(NetworkAdapterSnapshot snapshot) =>
        new(snapshot, new FakeClipboardService(), new StaticIpv4ConfigurationValidator());

    [Fact]
    public void Update_WithConnectivityOnlyChange_UpdatesTabDetailsAndAccessibility()
    {
        NetworkAdapterSnapshot initial = TestData.Snapshot();
        AdapterViewModel adapter = Create(initial);

        adapter.Update(initial with { InternetConnectivity = InternetConnectivity.Internet });

        Assert.Equal("Kablolu bağlantı (Ethernet)", adapter.DisplayName);
        Assert.Equal("İnternet çalışıyor", adapter.ConnectionState);
        Assert.Equal("İnternet çalışıyor", adapter.Details.ConnectionState);
        Assert.Contains(adapter.DisplayName, adapter.AccessibleDescription, StringComparison.Ordinal);
        Assert.Contains(adapter.ConnectionState, adapter.AccessibleDescription, StringComparison.Ordinal);
        Assert.Equal("Ethernet", adapter.Details.Name);
    }
}
