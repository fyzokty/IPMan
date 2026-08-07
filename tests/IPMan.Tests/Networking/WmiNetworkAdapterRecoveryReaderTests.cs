using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class WmiNetworkAdapterRecoveryReaderTests
{
    private static readonly NetworkAdapterId AdapterId = new("{11111111-2222-3333-4444-555555555555}");

    private static readonly string[] ExpectedIpv4Addresses = { "192.168.1.50" };

    private static readonly string[] ExpectedIpv4Gateways = { "192.168.1.1" };

    private static readonly string[] ExpectedIpv4Dns = { "1.1.1.1", "8.8.8.8" };

    private static readonly string[] RawAddresses = { "192.168.1.50", "2001:db8::1" };

    private static readonly string[] RawSubnets = { "255.255.255.0", "64" };

    private static readonly string[] RawGateways = { "192.168.1.1", "fe80::1" };

    private static readonly ushort[] RawGatewayMetrics = { 35, 90 };

    private static readonly string[] RawDns = { "1.1.1.1", "2001:4860:4860::8888", "8.8.8.8" };

    [Theory]
    [InlineData(0, null, DnsConfigurationMode.Automatic)]
    [InlineData(0x0002, "1.1.1.1, 8.8.8.8", DnsConfigurationMode.Manual)]
    [InlineData(0x0002, null, DnsConfigurationMode.Unknown)]
    [InlineData(0x0200, "9.9.9.9", DnsConfigurationMode.Unknown)]
    internal void DnsSettings_MapOnlyRestoreCapableSourceSemantics(
        ulong flags,
        string? nameServers,
        DnsConfigurationMode expected)
    {
        Assert.Equal(expected, WindowsDnsRecoveryStateProbe.MapSettings(flags, nameServers).Mode);
    }

    [Fact]
    public async Task ReadAsync_MapsRestoreCapableStateAndPreservesGatewayMetricPairing()
    {
        FakeWmiSession session = CompleteSession();
        FakeDnsModeProbe dns = new(DnsConfigurationMode.Manual);
        WmiNetworkAdapterRecoveryReader reader = new(
            new FakeWmiFactory(new WmiAdapterResolution(WmiAdapterResolutionStatus.Found, session)),
            dns);

        NetworkAdapterRecoveryReadResult result = await reader.ReadAsync(
            AdapterId,
            CancellationToken.None);

        Assert.Equal(NetworkAdapterRecoveryReadStatus.Success, result.Status);
        NetworkAdapterRecoverySnapshot snapshot = Assert.IsType<NetworkAdapterRecoverySnapshot>(result.Snapshot);
        Assert.True(snapshot.IsRestoreCapable);
        Assert.Equal(AdapterId, snapshot.Adapter.Id);
        Assert.Equal(NetworkConfigurationMode.Static, snapshot.Adapter.Mode);
        Assert.Equal(DnsConfigurationMode.Manual, snapshot.DnsMode);
        Assert.Equal(ExpectedIpv4Dns, snapshot.ConfiguredIpv4DnsServers);
        Assert.Equal(ExpectedIpv4Addresses, snapshot.Adapter.Ipv4Addresses.Select(value => value.Address));
        Assert.Equal(ExpectedIpv4Gateways, snapshot.Adapter.Ipv4Gateways);
        Ipv4GatewayRecoveryState gateway = Assert.Single(snapshot.Ipv4Gateways);
        Assert.Equal("192.168.1.1", gateway.Address);
        Assert.Equal((ushort)35, gateway.Metric);
        Assert.Equal(ExpectedIpv4Dns, snapshot.Adapter.Ipv4DnsServers);
        Assert.Equal(AdapterId.Value, dns.LastAdapterId);
    }

    [Fact]
    public async Task ReadAsync_WhenGatewayMetricIsMissing_MarksSnapshotNotRestoreCapable()
    {
        FakeWmiSession session = CompleteSession();
        session.Properties["GatewayCostMetric"] = Array.Empty<ushort>();
        WmiNetworkAdapterRecoveryReader reader = new(
            new FakeWmiFactory(new WmiAdapterResolution(WmiAdapterResolutionStatus.Found, session)),
            new FakeDnsModeProbe(DnsConfigurationMode.Automatic));

        NetworkAdapterRecoveryReadResult result = await reader.ReadAsync(
            AdapterId,
            CancellationToken.None);

        Assert.False(result.Snapshot!.IsRestoreCapable);
        Assert.Null(Assert.Single(result.Snapshot.Ipv4Gateways).Metric);
    }

    [Fact]
    public async Task ReadAsync_WhenDnsSourceCannotBeDetermined_MarksSnapshotNotRestoreCapable()
    {
        WmiNetworkAdapterRecoveryReader reader = new(
            new FakeWmiFactory(new WmiAdapterResolution(
                WmiAdapterResolutionStatus.Found,
                CompleteSession())),
            new FakeDnsModeProbe(DnsConfigurationMode.Unknown));

        NetworkAdapterRecoveryReadResult result = await reader.ReadAsync(
            AdapterId,
            CancellationToken.None);

        Assert.False(result.Snapshot!.IsRestoreCapable);
        Assert.Equal(DnsConfigurationMode.Unknown, result.Snapshot.DnsMode);
    }

    [Theory]
    [InlineData(WmiAdapterResolutionStatus.Unavailable, NetworkAdapterRecoveryReadStatus.AdapterUnavailable)]
    [InlineData(WmiAdapterResolutionStatus.Ambiguous, NetworkAdapterRecoveryReadStatus.AdapterMappingAmbiguous)]
    internal async Task ReadAsync_WhenExactIdentityDoesNotResolve_ReturnsTypedStatus(
        WmiAdapterResolutionStatus resolutionStatus,
        NetworkAdapterRecoveryReadStatus expected)
    {
        WmiNetworkAdapterRecoveryReader reader = new(
            new FakeWmiFactory(new WmiAdapterResolution(resolutionStatus)),
            new FakeDnsModeProbe(DnsConfigurationMode.Automatic));

        NetworkAdapterRecoveryReadResult result = await reader.ReadAsync(
            AdapterId,
            CancellationToken.None);

        Assert.Equal(expected, result.Status);
        Assert.Null(result.Snapshot);
    }

    private static FakeWmiSession CompleteSession() =>
        new(new Dictionary<string, object?>
        {
            ["IPAddress"] = RawAddresses,
            ["IPSubnet"] = RawSubnets,
            ["DefaultIPGateway"] = RawGateways,
            ["GatewayCostMetric"] = RawGatewayMetrics,
            ["DNSServerSearchOrder"] = RawDns,
            ["DHCPEnabled"] = false,
            ["Description"] = "Contoso Adapter",
            ["Caption"] = "Ethernet",
            ["MACAddress"] = "00:11:22:33:44:55"
        });

    private sealed class FakeWmiFactory : IWmiNetworkAdapterSessionFactory
    {
        private readonly WmiAdapterResolution _resolution;

        public FakeWmiFactory(WmiAdapterResolution resolution) => _resolution = resolution;

        public WmiAdapterResolution ResolveBySettingId(string adapterId)
        {
            Assert.Equal(AdapterId.Value, adapterId);
            return _resolution;
        }
    }

    private sealed class FakeWmiSession : IWmiNetworkAdapterSession
    {
        public FakeWmiSession(Dictionary<string, object?> properties) => Properties = properties;

        public Dictionary<string, object?> Properties { get; }

        public object? ReadProperty(string propertyName) => Properties[propertyName];

        public uint Invoke(string methodName, IReadOnlyDictionary<string, object?>? parameters) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }
    }

    private sealed class FakeDnsModeProbe : IDnsRecoveryStateProbe
    {
        private readonly DnsConfigurationMode _mode;

        public FakeDnsModeProbe(DnsConfigurationMode mode) => _mode = mode;

        public string? LastAdapterId { get; private set; }

        public DnsRecoveryState ReadIpv4State(string adapterId)
        {
            LastAdapterId = adapterId;
            return new DnsRecoveryState(
                _mode,
                _mode == DnsConfigurationMode.Manual
                    ? ExpectedIpv4Dns
                    : Array.Empty<string>());
        }
    }
}
