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
    [InlineData(0, null, DnsConfigurationMode.Automatic, DnsRecoveryProbeStatus.Automatic, 0)]
    [InlineData(0x0002, "1.1.1.1, 8.8.8.8", DnsConfigurationMode.Manual, DnsRecoveryProbeStatus.Manual, 2)]
    [InlineData(0x0002, null, DnsConfigurationMode.Unknown, DnsRecoveryProbeStatus.ManualAdapterFlagWithoutUsableIpv4Servers, 0)]
    [InlineData(0x0200, "9.9.9.9", DnsConfigurationMode.Unknown, DnsRecoveryProbeStatus.ProfileOrPolicyDnsDetected, 0)]
    internal void DnsSettings_MapOnlyRestoreCapableSourceSemantics(
        ulong flags,
        string? nameServers,
        DnsConfigurationMode expectedMode,
        DnsRecoveryProbeStatus expectedStatus,
        int expectedServerCount)
    {
        DnsRecoveryState result = WindowsDnsRecoveryStateProbe.MapSettings(flags, nameServers);

        Assert.Equal(expectedMode, result.Mode);
        Assert.Equal(expectedStatus, result.Diagnostic.Status);
        Assert.Equal(expectedServerCount, result.Diagnostic.UsableIpv4ServerCount);
    }

    [Fact]
    public void DnsProbe_WhenAdapterIdIsNotGuid_ReturnsTypedUnknownWithoutNativeRead()
    {
        FakeDnsSettingsReader reader = new(_ => throw new InvalidOperationException("must not run"));
        WindowsDnsRecoveryStateProbe probe = new(reader);

        DnsRecoveryState result = probe.ReadIpv4State("not-a-guid");

        Assert.Equal(DnsConfigurationMode.Unknown, result.Mode);
        Assert.Equal(DnsRecoveryProbeStatus.InvalidAdapterGuid, result.Diagnostic.Status);
        Assert.Equal(0, reader.ReadCount);
    }

    [Fact]
    public void DnsProbe_WhenNativeLibraryIsUnavailable_ReturnsTypedUnknown()
    {
        WindowsDnsRecoveryStateProbe probe = new(
            new FakeDnsSettingsReader(_ => throw new DllNotFoundException("sensitive")));

        DnsRecoveryState result = probe.ReadIpv4State(AdapterId.Value);

        Assert.Equal(DnsConfigurationMode.Unknown, result.Mode);
        Assert.Equal(DnsRecoveryProbeStatus.NativeLibraryUnavailable, result.Diagnostic.Status);
        Assert.Null(result.Diagnostic.NativeResult);
    }

    [Fact]
    public void DnsProbe_WhenNativeEntryPointIsUnavailable_ReturnsTypedUnknown()
    {
        WindowsDnsRecoveryStateProbe probe = new(
            new FakeDnsSettingsReader(_ => throw new EntryPointNotFoundException("sensitive")));

        DnsRecoveryState result = probe.ReadIpv4State(AdapterId.Value);

        Assert.Equal(DnsConfigurationMode.Unknown, result.Mode);
        Assert.Equal(DnsRecoveryProbeStatus.NativeEntryPointUnavailable, result.Diagnostic.Status);
    }

    [Fact]
    public void DnsProbe_WhenNativeCallReturnsNonZero_RetainsNumericCodeAndUnknownMode()
    {
        WindowsDnsRecoveryStateProbe probe = new(
            new FakeDnsSettingsReader(_ => new DnsInterfaceSettingsReadResult(87, 0, null)));

        DnsRecoveryState result = probe.ReadIpv4State(AdapterId.Value);

        Assert.Equal(DnsConfigurationMode.Unknown, result.Mode);
        Assert.Equal(DnsRecoveryProbeStatus.NativeCallFailed, result.Diagnostic.Status);
        Assert.Equal((uint)87, result.Diagnostic.NativeResult);
    }

    [Fact]
    public void DnsNativeReader_WhenNativeCallSucceeds_FreesReturnedSettings()
    {
        FakeDnsNativeApi native = new(0);
        WindowsDnsInterfaceSettingsReader reader = new(native);

        DnsInterfaceSettingsReadResult result = reader.Read(Guid.Parse(AdapterId.Value));

        Assert.Equal((uint)0, result.NativeResult);
        Assert.Equal(1, native.FreeCount);
    }

    [Fact]
    public void DnsNativeReader_WhenNativeCallFails_DoesNotFreeUnreturnedSettings()
    {
        FakeDnsNativeApi native = new(87);
        WindowsDnsInterfaceSettingsReader reader = new(native);

        DnsInterfaceSettingsReadResult result = reader.Read(Guid.Parse(AdapterId.Value));

        Assert.Equal((uint)87, result.NativeResult);
        Assert.Equal(0, native.FreeCount);
    }

    [Fact]
    public async Task ReadAsync_MapsRestoreCapableStateAndPreservesGatewayMetricPairing()
    {
        FakeWmiSession session = CompleteSession();
        FakeDnsModeProbe dns = new(DnsConfigurationMode.Manual);
        WmiNetworkAdapterRecoveryReader reader = new(
            new FakeWmiFactory(new WmiAdapterResolution(WmiAdapterResolutionStatus.Found, session)),
            dns);

        NetworkAdapterRecoveryDiagnosticReadResult diagnostic = await reader.ReadDiagnosticAsync(
            AdapterId,
            CancellationToken.None);
        NetworkAdapterRecoveryReadResult result = diagnostic.RecoveryRead;

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
        Assert.Equal(DnsRecoveryProbeStatus.Manual, diagnostic.DnsProbe!.Status);
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
                    : Array.Empty<string>(),
                new DnsRecoveryProbeDiagnostic(
                    _mode switch
                    {
                        DnsConfigurationMode.Automatic => DnsRecoveryProbeStatus.Automatic,
                        DnsConfigurationMode.Manual => DnsRecoveryProbeStatus.Manual,
                        _ => DnsRecoveryProbeStatus.NativeCallFailed
                    },
                    NativeResult: _mode == DnsConfigurationMode.Unknown ? 87u : 0u,
                    AdapterManualServerFlag: _mode == DnsConfigurationMode.Manual,
                    ProfileServerFlag: false,
                    UsableIpv4ServerCount: _mode == DnsConfigurationMode.Manual
                        ? ExpectedIpv4Dns.Length
                        : 0));
        }
    }

    private sealed class FakeDnsSettingsReader : IDnsInterfaceSettingsReader
    {
        private readonly Func<Guid, DnsInterfaceSettingsReadResult> _handler;

        public FakeDnsSettingsReader(Func<Guid, DnsInterfaceSettingsReadResult> handler) =>
            _handler = handler;

        public int ReadCount { get; private set; }

        public DnsInterfaceSettingsReadResult Read(Guid interfaceId)
        {
            ReadCount++;
            return _handler(interfaceId);
        }
    }

    private sealed class FakeDnsNativeApi : IDnsInterfaceSettingsNativeApi
    {
        private readonly uint _result;

        public FakeDnsNativeApi(uint result) => _result = result;

        public int FreeCount { get; private set; }

        public uint Get(Guid interfaceId, ref DnsInterfaceSettingsV1 settings)
        {
            Assert.Equal(Guid.Parse(AdapterId.Value), interfaceId);
            settings.Flags = 0;
            return _result;
        }

        public void Free(ref DnsInterfaceSettingsV1 settings) => FreeCount++;
    }
}
