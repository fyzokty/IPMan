using IPMan.Domain.Networking;

namespace IPMan.IntegrationTests.Harness;

public static class ScenarioPreconditionValidator
{
    public const string ModeDimension = "Mode";
    public const string Ipv4AddressDimension = "Ipv4Address";
    public const string SubnetMaskDimension = "SubnetMask";
    public const string GatewayDimension = "Gateway";
    public const string DnsDimension = "Dns";

    public static ScenarioPreconditionResult Validate(
        Sprint08Scenario scenario,
        NetworkAdapterRecoverySnapshot before,
        StaticIpv4Configuration requested)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(requested);
        List<string> reasons = new();

        ValidateTopology(before, reasons);
        RequireStartingMode(scenario, before, reasons);

        bool addressMatches = string.Equals(
            before.Adapter.Ipv4Address,
            requested.Ipv4Address,
            StringComparison.Ordinal);
        bool maskMatches = string.Equals(
            before.Adapter.SubnetMask,
            requested.SubnetMask,
            StringComparison.Ordinal);
        bool gatewayMatches = GatewayMatches(before, requested);
        bool dnsMatches = DnsMatches(before, requested);
        IReadOnlyList<string> dimensions = GetRequestedDimensions(
            before,
            addressMatches,
            maskMatches,
            gatewayMatches,
            dnsMatches);

        switch (scenario)
        {
            case Sprint08Scenario.StaticToStatic:
                if (dimensions.Count == 0)
                {
                    reasons.Add("StaticToStatic requires at least one requested configuration change.");
                }

                break;
            case Sprint08Scenario.DhcpToStatic:
                break;
            case Sprint08Scenario.SetGateway:
                RequireFocusedGatewayState(addressMatches, maskMatches, dnsMatches, reasons);

                if (before.Ipv4Gateways.Length != 0 || requested.Gateway is null)
                {
                    reasons.Add("SetGateway requires absent -> explicit gateway semantics.");
                }

                RequireOnlyDimension(dimensions, GatewayDimension, reasons);
                break;
            case Sprint08Scenario.ClearGateway:
                RequireFocusedGatewayState(addressMatches, maskMatches, dnsMatches, reasons);

                if (before.Ipv4Gateways.Length != 1 || requested.Gateway is not null)
                {
                    reasons.Add("ClearGateway requires one gateway -> absent semantics.");
                }

                RequireOnlyDimension(dimensions, GatewayDimension, reasons);
                break;
            case Sprint08Scenario.ManualDnsOne:
                ValidateFocusedDns(
                    before,
                    requested,
                    addressMatches,
                    maskMatches,
                    gatewayMatches,
                    expectedDnsCount: 1,
                    dimensions,
                    reasons);
                break;
            case Sprint08Scenario.ManualDnsTwo:
                ValidateFocusedDns(
                    before,
                    requested,
                    addressMatches,
                    maskMatches,
                    gatewayMatches,
                    expectedDnsCount: 2,
                    dimensions,
                    reasons);
                break;
            case Sprint08Scenario.AutomaticDns:
                ValidateFocusedDns(
                    before,
                    requested,
                    addressMatches,
                    maskMatches,
                    gatewayMatches,
                    expectedDnsCount: 0,
                    dimensions,
                    reasons);

                if (before.DnsMode != DnsConfigurationMode.Manual)
                {
                    reasons.Add("AutomaticDns requires a manual -> automatic DNS transition.");
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario));
        }

        return new ScenarioPreconditionResult(reasons, dimensions);
    }

    private static void ValidateTopology(
        NetworkAdapterRecoverySnapshot before,
        List<string> reasons)
    {
        if (before.Adapter.Ipv4Addresses.Count != 1 || before.Adapter.SubnetMask is null)
        {
            reasons.Add("The exact adapter must have one IPv4 address with a readable subnet mask.");
        }

        if (before.Ipv4Gateways.Length > 1)
        {
            reasons.Add("The exact adapter has multiple IPv4 gateways.");
        }

        if (before.Adapter.Ipv4DnsServers.Count > 2)
        {
            reasons.Add("The exact adapter has more than two IPv4 DNS servers.");
        }
    }

    private static void RequireStartingMode(
        Sprint08Scenario scenario,
        NetworkAdapterRecoverySnapshot before,
        List<string> reasons)
    {
        NetworkConfigurationMode required = scenario == Sprint08Scenario.DhcpToStatic
            ? NetworkConfigurationMode.Dhcp
            : NetworkConfigurationMode.Static;

        if (before.Adapter.Mode != required)
        {
            reasons.Add($"The scenario requires starting mode {required}.");
        }
    }

    private static void RequireFocusedGatewayState(
        bool addressMatches,
        bool maskMatches,
        bool dnsMatches,
        List<string> reasons)
    {
        if (!addressMatches || !maskMatches)
        {
            reasons.Add("A focused gateway scenario must preserve IPv4 address and subnet mask.");
        }

        if (!dnsMatches)
        {
            reasons.Add("A focused gateway scenario must preserve DNS source/list semantics.");
        }
    }

    private static void ValidateFocusedDns(
        NetworkAdapterRecoverySnapshot before,
        StaticIpv4Configuration requested,
        bool addressMatches,
        bool maskMatches,
        bool gatewayMatches,
        int expectedDnsCount,
        IReadOnlyList<string> dimensions,
        List<string> reasons)
    {
        if (!addressMatches || !maskMatches)
        {
            reasons.Add("A focused DNS scenario must preserve IPv4 address and subnet mask.");
        }

        if (!gatewayMatches)
        {
            reasons.Add("A focused DNS scenario must preserve gateway semantics.");
        }

        if (RequestedDns(requested).Length != expectedDnsCount)
        {
            reasons.Add($"The selected DNS scenario requires exactly {expectedDnsCount} requested DNS servers.");
        }

        RequireOnlyDimension(dimensions, DnsDimension, reasons);
    }

    private static void RequireOnlyDimension(
        IReadOnlyList<string> dimensions,
        string expected,
        List<string> reasons)
    {
        if (dimensions.Count != 1 || !string.Equals(dimensions[0], expected, StringComparison.Ordinal))
        {
            reasons.Add($"The focused scenario must change only the {expected} dimension.");
        }
    }

    private static List<string> GetRequestedDimensions(
        NetworkAdapterRecoverySnapshot before,
        bool addressMatches,
        bool maskMatches,
        bool gatewayMatches,
        bool dnsMatches)
    {
        List<string> dimensions = new();

        if (before.Adapter.Mode != NetworkConfigurationMode.Static)
        {
            dimensions.Add(ModeDimension);
        }

        if (!addressMatches)
        {
            dimensions.Add(Ipv4AddressDimension);
        }

        if (!maskMatches)
        {
            dimensions.Add(SubnetMaskDimension);
        }

        if (!gatewayMatches)
        {
            dimensions.Add(GatewayDimension);
        }

        if (!dnsMatches)
        {
            dimensions.Add(DnsDimension);
        }

        return dimensions;
    }

    private static bool GatewayMatches(
        NetworkAdapterRecoverySnapshot before,
        StaticIpv4Configuration requested)
    {
        string[] requestedGateways = requested.Gateway is null
            ? Array.Empty<string>()
            : new[] { requested.Gateway };

        return before.Ipv4Gateways
            .Select(gateway => gateway.Address)
            .SequenceEqual(requestedGateways, StringComparer.Ordinal);
    }

    private static bool DnsMatches(
        NetworkAdapterRecoverySnapshot before,
        StaticIpv4Configuration requested)
    {
        string[] requestedDns = RequestedDns(requested);

        return requestedDns.Length == 0
            ? before.DnsMode == DnsConfigurationMode.Automatic
            : before.DnsMode == DnsConfigurationMode.Manual &&
                before.ConfiguredIpv4DnsServers.SequenceEqual(requestedDns, StringComparer.Ordinal);
    }

    private static string[] RequestedDns(StaticIpv4Configuration requested) =>
        new[] { requested.PrimaryDns, requested.SecondaryDns }
            .Where(value => value is not null)
            .Select(value => value!)
            .ToArray();
}
