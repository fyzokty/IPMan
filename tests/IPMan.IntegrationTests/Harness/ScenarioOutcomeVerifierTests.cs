using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.IntegrationTests.Observation;
using Xunit;

namespace IPMan.IntegrationTests.Harness;

public sealed class ScenarioOutcomeVerifierTests
{
    private static readonly NetworkAdapterId AdapterId =
        new("{11111111-2222-3333-4444-555555555555}");

    [Fact]
    public void Verify_WhenGatewayWasNotRequested_PreservesAddressAndMetric()
    {
        StaticIpv4Configuration desired = Configuration("192.0.2.20", "192.0.2.1");
        NetworkAdapterRecoverySnapshot before = Recovery("192.0.2.10", "192.0.2.1", 25);
        NetworkAdapterRecoverySnapshot after = Recovery("192.0.2.20", "192.0.2.1", 25);

        IReadOnlyList<string> differences = Verify(
            desired,
            before,
            after,
            new[] { ScenarioPreconditionValidator.Ipv4AddressDimension });

        Assert.Empty(differences);
    }

    [Fact]
    public void Verify_WhenGatewayWasNotRequested_DetectsMetricChange()
    {
        StaticIpv4Configuration desired = Configuration("192.0.2.20", "192.0.2.1");
        NetworkAdapterRecoverySnapshot before = Recovery("192.0.2.10", "192.0.2.1", 25);
        NetworkAdapterRecoverySnapshot after = Recovery("192.0.2.20", "192.0.2.1", 1);

        IReadOnlyList<string> differences = Verify(
            desired,
            before,
            after,
            new[] { ScenarioPreconditionValidator.Ipv4AddressDimension });

        Assert.Contains(differences, difference =>
            difference.Contains("without a gateway request", StringComparison.Ordinal));
    }

    [Fact]
    public void Verify_WhenNewGatewayWasRequested_RequiresDefaultMetricOne()
    {
        StaticIpv4Configuration desired = Configuration("192.0.2.10", "192.0.2.254");
        NetworkAdapterRecoverySnapshot before = Recovery("192.0.2.10", "192.0.2.1", 25);
        NetworkAdapterRecoverySnapshot after = Recovery("192.0.2.10", "192.0.2.254", 1);

        IReadOnlyList<string> differences = Verify(
            desired,
            before,
            after,
            new[] { ScenarioPreconditionValidator.GatewayDimension });

        Assert.Empty(differences);
    }

    [Fact]
    public void Verify_WhenGatewayClearWasRequested_RequiresNoGateway()
    {
        StaticIpv4Configuration desired = Configuration("192.0.2.10", gateway: null);
        NetworkAdapterRecoverySnapshot before = Recovery("192.0.2.10", "192.0.2.1", 25);
        NetworkAdapterRecoverySnapshot after = Recovery("192.0.2.10", gateway: null, metric: null);

        IReadOnlyList<string> differences = Verify(
            desired,
            before,
            after,
            new[] { ScenarioPreconditionValidator.GatewayDimension });

        Assert.Empty(differences);
    }

    private static IReadOnlyList<string> Verify(
        StaticIpv4Configuration desired,
        NetworkAdapterRecoverySnapshot before,
        NetworkAdapterRecoverySnapshot after,
        IReadOnlyList<string> requestedDimensions) =>
        ScenarioOutcomeVerifier.Verify(
            new DestructiveNetworkTestSettings(
                AdapterId,
                Sprint08Scenario.StaticToStatic,
                desired,
                "evidence"),
            new StaticIpv4ApplyResult(
                StaticIpv4ApplyStatus.VerifiedSuccess,
                Rollback: new RollbackSnapshotReference("snapshot", "rollback.json")),
            before,
            NetworkAdapterRecoveryReadResult.Success(after),
            requestedDimensions,
            new ObservationComparisonResult(true, Array.Empty<string>()),
            rollbackMatchesBefore: true);

    private static StaticIpv4Configuration Configuration(string address, string? gateway) =>
        new(address, "255.255.255.0", gateway, null, null);

    private static NetworkAdapterRecoverySnapshot Recovery(
        string address,
        string? gateway,
        ushort? metric)
    {
        string[] gateways = gateway is null ? Array.Empty<string>() : new[] { gateway };
        NetworkAdapterSnapshot adapter = new(
            AdapterId,
            "Adapter",
            "Disposable adapter",
            "00-11-22-33-44-55",
            IsConnected: true,
            LinkSpeedBitsPerSecond: 1_000_000_000,
            NetworkConfigurationMode.Static,
            address,
            "255.255.255.0",
            gateway,
            PrimaryDns: null,
            SecondaryDns: null,
            new Ipv4AddressCollection(
                new[] { new Ipv4AddressAssignment(address, "255.255.255.0") }),
            new Ipv4AddressValueCollection(gateways),
            Ipv4AddressValueCollection.Empty);

        return new NetworkAdapterRecoverySnapshot(
            adapter,
            DnsConfigurationMode.Automatic,
            Array.Empty<string>(),
            gateway is null
                ? Array.Empty<Ipv4GatewayRecoveryState>()
                : new[] { new Ipv4GatewayRecoveryState(gateway, metric) });
    }
}
