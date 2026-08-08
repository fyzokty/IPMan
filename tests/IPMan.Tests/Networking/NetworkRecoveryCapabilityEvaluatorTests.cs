using IPMan.Domain.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class NetworkRecoveryCapabilityEvaluatorTests
{
    private static readonly string[] ConfiguredDnsServers = ["1.1.1.1"];

    [Fact]
    public void Evaluate_WhenStateIsComplete_ReturnsCapableAndMatchesConvenienceProjection()
    {
        NetworkAdapterRecoverySnapshot snapshot = Snapshot();

        NetworkRecoveryCapabilityEvaluation result = snapshot.RestoreCapability;

        Assert.True(result.IsCapable);
        Assert.Empty(result.BlockingReasons);
        Assert.Equal(result.IsCapable, snapshot.IsRestoreCapable);
    }

    [Fact]
    public void Evaluate_WhenAdapterModeIsUnknown_ReportsReason()
    {
        AssertOnlyReason(
            Snapshot(mode: NetworkConfigurationMode.Unknown),
            NetworkRecoveryCapabilityReason.AdapterModeUnknown);
    }

    [Fact]
    public void Evaluate_WhenDnsModeIsUnknown_ReportsReason()
    {
        AssertOnlyReason(
            Snapshot(dnsMode: DnsConfigurationMode.Unknown),
            NetworkRecoveryCapabilityReason.DnsModeUnknown);
    }

    [Fact]
    public void Evaluate_WhenManualDnsServersAreMissing_ReportsReason()
    {
        AssertOnlyReason(
            Snapshot(
                dnsMode: DnsConfigurationMode.Manual,
                configuredDns: Array.Empty<string>()),
            NetworkRecoveryCapabilityReason.ManualDnsServersMissing);
    }

    [Fact]
    public void Evaluate_WhenGatewayAddressesDoNotMatch_ReportsReason()
    {
        AssertOnlyReason(
            Snapshot(recoveryGateway: "192.0.2.254"),
            NetworkRecoveryCapabilityReason.GatewayAddressFidelityMismatch);
    }

    [Fact]
    public void Evaluate_WhenStaticGatewayMetricIsMissing_ReportsReason()
    {
        AssertOnlyReason(
            Snapshot(gatewayMetric: null),
            NetworkRecoveryCapabilityReason.StaticGatewayMetricMissing);
    }

    [Fact]
    public void Evaluate_WhenStaticSubnetMaskIsMissing_ReportsReason()
    {
        AssertOnlyReason(
            Snapshot(subnetMask: null),
            NetworkRecoveryCapabilityReason.StaticIpv4SubnetMaskMissing);
    }

    [Fact]
    public void Evaluate_AcrossSprint07SemanticCombinations_MatchesApprovedExpression()
    {
        foreach (NetworkConfigurationMode mode in Enum.GetValues<NetworkConfigurationMode>())
        {
            foreach (DnsConfigurationMode dnsMode in Enum.GetValues<DnsConfigurationMode>())
            {
                foreach (bool hasConfiguredDnsServer in new[] { false, true })
                {
                    foreach (bool gatewayAddressesMatch in new[] { false, true })
                    {
                        foreach (bool gatewayMetricIsPresent in new[] { false, true })
                        {
                            foreach (bool subnetMaskIsPresent in new[] { false, true })
                            {
                                NetworkAdapterRecoverySnapshot snapshot = Snapshot(
                                    mode,
                                    dnsMode,
                                    hasConfiguredDnsServer ? ConfiguredDnsServers : Array.Empty<string>(),
                                    adapterGateway: "192.0.2.1",
                                    recoveryGateway: gatewayAddressesMatch ? "192.0.2.1" : "192.0.2.254",
                                    gatewayMetric: gatewayMetricIsPresent ? (ushort)25 : null,
                                    subnetMask: subnetMaskIsPresent ? "255.255.255.0" : null);

                                bool expected = ApprovedSprint07IsRestoreCapable(snapshot);
                                bool actual = NetworkRecoveryCapabilityEvaluator.Evaluate(snapshot).IsCapable;

                                Assert.True(
                                    expected == actual,
                                    $"Capability mismatch for mode={mode}, dnsMode={dnsMode}, " +
                                    $"hasDns={hasConfiguredDnsServer}, gatewaysMatch={gatewayAddressesMatch}, " +
                                    $"hasMetric={gatewayMetricIsPresent}, hasMask={subnetMaskIsPresent}.");
                            }
                        }
                    }
                }
            }
        }
    }

    private static void AssertOnlyReason(
        NetworkAdapterRecoverySnapshot snapshot,
        NetworkRecoveryCapabilityReason expected)
    {
        NetworkRecoveryCapabilityEvaluation result = snapshot.RestoreCapability;

        Assert.False(result.IsCapable);
        Assert.Equal(expected, Assert.Single(result.BlockingReasons));
        Assert.Equal(result.IsCapable, snapshot.IsRestoreCapable);
    }

    private static bool ApprovedSprint07IsRestoreCapable(
        NetworkAdapterRecoverySnapshot snapshot) =>
        snapshot.Adapter.Mode != NetworkConfigurationMode.Unknown &&
        snapshot.DnsMode != DnsConfigurationMode.Unknown &&
        (snapshot.DnsMode != DnsConfigurationMode.Manual ||
            snapshot.ConfiguredIpv4DnsServers.Length > 0) &&
        snapshot.Adapter.Ipv4Gateways.SequenceEqual(
            snapshot.Ipv4Gateways.Select(gateway => gateway.Address),
            StringComparer.Ordinal) &&
        (snapshot.Adapter.Mode != NetworkConfigurationMode.Static ||
            snapshot.Ipv4Gateways.All(gateway => gateway.Metric.HasValue)) &&
        (snapshot.Adapter.Mode != NetworkConfigurationMode.Static ||
            snapshot.Adapter.Ipv4Addresses.All(address => address.SubnetMask is not null));

    private static NetworkAdapterRecoverySnapshot Snapshot(
        NetworkConfigurationMode mode = NetworkConfigurationMode.Static,
        DnsConfigurationMode dnsMode = DnsConfigurationMode.Automatic,
        string[]? configuredDns = null,
        string adapterGateway = "192.0.2.1",
        string recoveryGateway = "192.0.2.1",
        ushort? gatewayMetric = 25,
        string? subnetMask = "255.255.255.0")
    {
        configuredDns ??= Array.Empty<string>();
        NetworkAdapterSnapshot adapter = new(
            new NetworkAdapterId("{11111111-2222-3333-4444-555555555555}"),
            "Adapter",
            "Recovery test adapter",
            "00-11-22-33-44-55",
            IsConnected: true,
            LinkSpeedBitsPerSecond: 1_000_000_000,
            mode,
            "192.0.2.10",
            subnetMask,
            adapterGateway,
            PrimaryDns: null,
            SecondaryDns: null,
            new Ipv4AddressCollection(
                new[] { new Ipv4AddressAssignment("192.0.2.10", subnetMask) }),
            new Ipv4AddressValueCollection(new[] { adapterGateway }),
            Ipv4AddressValueCollection.Empty);

        return new NetworkAdapterRecoverySnapshot(
            adapter,
            dnsMode,
            configuredDns,
            new[] { new Ipv4GatewayRecoveryState(recoveryGateway, gatewayMetric) });
    }
}
