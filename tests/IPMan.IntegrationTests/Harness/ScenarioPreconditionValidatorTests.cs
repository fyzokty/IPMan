using IPMan.Domain.Networking;
using Xunit;

namespace IPMan.IntegrationTests.Harness;

public sealed class ScenarioPreconditionValidatorTests
{
    private const string Address = "192.0.2.10";
    private const string Mask = "255.255.255.0";
    private const string Gateway = "192.0.2.1";

    private static readonly string[] OneManualDns = { "1.1.1.1" };

    [Fact]
    public void Validate_SetGateway_WhenOnlyGatewayChanges_Allows()
    {
        NetworkAdapterRecoverySnapshot before = Before(gateway: null);
        StaticIpv4Configuration requested = Configuration(gateway: Gateway);

        ScenarioPreconditionResult result = Validate(Sprint08Scenario.SetGateway, before, requested);

        Assert.True(result.IsAllowed);
        Assert.Equal(new[] { ScenarioPreconditionValidator.GatewayDimension }, result.RequestedDimensions);
    }

    [Fact]
    public void Validate_SetGateway_WhenIpv4OrDnsAlsoChanges_Refuses()
    {
        NetworkAdapterRecoverySnapshot before = Before(gateway: null);
        StaticIpv4Configuration requested = new(
            "192.0.2.20",
            Mask,
            Gateway,
            "8.8.8.8",
            null);

        ScenarioPreconditionResult result = Validate(Sprint08Scenario.SetGateway, before, requested);

        Assert.False(result.IsAllowed);
        Assert.Contains(ScenarioPreconditionValidator.Ipv4AddressDimension, result.RequestedDimensions);
        Assert.Contains(ScenarioPreconditionValidator.DnsDimension, result.RequestedDimensions);
    }

    [Fact]
    public void Validate_ClearGateway_WhenOnlyGatewayChanges_Allows()
    {
        NetworkAdapterRecoverySnapshot before = Before(gateway: Gateway);
        StaticIpv4Configuration requested = Configuration(gateway: null);

        ScenarioPreconditionResult result = Validate(Sprint08Scenario.ClearGateway, before, requested);

        Assert.True(result.IsAllowed);
        Assert.Equal(new[] { ScenarioPreconditionValidator.GatewayDimension }, result.RequestedDimensions);
    }

    [Fact]
    public void Validate_ClearGateway_WhenDnsAlsoChanges_Refuses()
    {
        NetworkAdapterRecoverySnapshot before = Before(gateway: Gateway);
        StaticIpv4Configuration requested = new(Address, Mask, null, "8.8.8.8", null);

        ScenarioPreconditionResult result = Validate(Sprint08Scenario.ClearGateway, before, requested);

        Assert.False(result.IsAllowed);
        Assert.Contains(ScenarioPreconditionValidator.DnsDimension, result.RequestedDimensions);
    }

    [Fact]
    public void Validate_ManualDnsOne_WhenOnlyDnsChanges_Allows()
    {
        NetworkAdapterRecoverySnapshot before = Before(gateway: Gateway);
        StaticIpv4Configuration requested = new(Address, Mask, Gateway, "1.1.1.1", null);

        ScenarioPreconditionResult result = Validate(Sprint08Scenario.ManualDnsOne, before, requested);

        Assert.True(result.IsAllowed);
        Assert.Equal(new[] { ScenarioPreconditionValidator.DnsDimension }, result.RequestedDimensions);
    }

    [Fact]
    public void Validate_ManualDnsTwo_WhenGatewayAlsoChanges_Refuses()
    {
        NetworkAdapterRecoverySnapshot before = Before(gateway: Gateway);
        StaticIpv4Configuration requested = new(
            Address,
            Mask,
            "192.0.2.254",
            "1.1.1.1",
            "8.8.8.8");

        ScenarioPreconditionResult result = Validate(Sprint08Scenario.ManualDnsTwo, before, requested);

        Assert.False(result.IsAllowed);
        Assert.Contains(ScenarioPreconditionValidator.GatewayDimension, result.RequestedDimensions);
    }

    [Fact]
    public void Validate_AutomaticDns_WhenOnlyManualDnsIsCleared_Allows()
    {
        NetworkAdapterRecoverySnapshot before = Before(
            gateway: Gateway,
            dnsMode: DnsConfigurationMode.Manual,
            configuredDns: OneManualDns);
        StaticIpv4Configuration requested = Configuration(gateway: Gateway);

        ScenarioPreconditionResult result = Validate(Sprint08Scenario.AutomaticDns, before, requested);

        Assert.True(result.IsAllowed);
        Assert.Equal(new[] { ScenarioPreconditionValidator.DnsDimension }, result.RequestedDimensions);
    }

    [Fact]
    public void Validate_AutomaticDns_WhenIpv4AlsoChanges_Refuses()
    {
        NetworkAdapterRecoverySnapshot before = Before(
            gateway: Gateway,
            dnsMode: DnsConfigurationMode.Manual,
            configuredDns: OneManualDns);
        StaticIpv4Configuration requested = new("192.0.2.20", Mask, Gateway, null, null);

        ScenarioPreconditionResult result = Validate(Sprint08Scenario.AutomaticDns, before, requested);

        Assert.False(result.IsAllowed);
        Assert.Contains(ScenarioPreconditionValidator.Ipv4AddressDimension, result.RequestedDimensions);
    }

    [Fact]
    public void Validate_FocusedDns_WhenExistingGatewayMetricIsNonDefault_AllowsPreservation()
    {
        NetworkAdapterRecoverySnapshot before = Before(gateway: Gateway, gatewayMetric: 25);
        StaticIpv4Configuration requested = new(Address, Mask, Gateway, "1.1.1.1", null);

        ScenarioPreconditionResult result = Validate(Sprint08Scenario.ManualDnsOne, before, requested);

        Assert.True(result.IsAllowed);
        Assert.Equal(new[] { ScenarioPreconditionValidator.DnsDimension }, result.RequestedDimensions);
    }

    [Fact]
    public void Validate_StaticToStatic_RecordsEveryRequestedDimension()
    {
        NetworkAdapterRecoverySnapshot before = Before(gateway: Gateway);
        StaticIpv4Configuration requested = new(
            "192.0.2.20",
            "255.255.255.128",
            null,
            "1.1.1.1",
            null);

        ScenarioPreconditionResult result = Validate(Sprint08Scenario.StaticToStatic, before, requested);

        Assert.True(result.IsAllowed);
        Assert.Equal(
            new[]
            {
                ScenarioPreconditionValidator.Ipv4AddressDimension,
                ScenarioPreconditionValidator.SubnetMaskDimension,
                ScenarioPreconditionValidator.GatewayDimension,
                ScenarioPreconditionValidator.DnsDimension
            },
            result.RequestedDimensions);
    }

    private static ScenarioPreconditionResult Validate(
        Sprint08Scenario scenario,
        NetworkAdapterRecoverySnapshot before,
        StaticIpv4Configuration requested) =>
        ScenarioPreconditionValidator.Validate(scenario, before, requested);

    private static StaticIpv4Configuration Configuration(string? gateway) =>
        new(Address, Mask, gateway, null, null);

    private static NetworkAdapterRecoverySnapshot Before(
        string? gateway,
        DnsConfigurationMode dnsMode = DnsConfigurationMode.Automatic,
        string[]? configuredDns = null,
        ushort gatewayMetric = 1)
    {
        configuredDns ??= Array.Empty<string>();
        string[] gateways = gateway is null ? Array.Empty<string>() : new[] { gateway };
        NetworkAdapterSnapshot adapter = new(
            new NetworkAdapterId("{11111111-2222-3333-4444-555555555555}"),
            "Adapter",
            "Disposable adapter",
            "00-11-22-33-44-55",
            true,
            1_000_000_000,
            NetworkConfigurationMode.Static,
            Address,
            Mask,
            gateway,
            configuredDns.FirstOrDefault(),
            configuredDns.Skip(1).FirstOrDefault(),
            new Ipv4AddressCollection(new[] { new Ipv4AddressAssignment(Address, Mask) }),
            new Ipv4AddressValueCollection(gateways),
            new Ipv4AddressValueCollection(configuredDns));

        return new NetworkAdapterRecoverySnapshot(
            adapter,
            dnsMode,
            configuredDns,
            gateways
                .Select(value => new Ipv4GatewayRecoveryState(value, gatewayMetric))
                .ToArray());
    }
}
