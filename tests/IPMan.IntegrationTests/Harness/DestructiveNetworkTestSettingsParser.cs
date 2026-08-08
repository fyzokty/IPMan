using System.Net;
using System.Net.Sockets;
using IPMan.Domain.Networking;

namespace IPMan.IntegrationTests.Harness;

public static class DestructiveNetworkTestSettingsParser
{
    public const string EnableVariable = "IPMAN_DESTRUCTIVE_NETWORK_TESTS";
    public const string AdapterVariable = "IPMAN_TEST_ADAPTER_ID";
    public const string IsolatedVariable = "IPMAN_TEST_ADAPTER_IS_ISOLATED";
    public const string FilterVariable = "IPMAN_DESTRUCTIVE_FILTER_ACK";
    public const string ConflictVariable = "IPMAN_TEST_CONFLICT_RISK_ACK";
    public const string ScenarioVariable = "IPMAN_TEST_SCENARIO";
    public const string Ipv4Variable = "IPMAN_TEST_IPV4";
    public const string SubnetVariable = "IPMAN_TEST_SUBNET_MASK";
    public const string GatewayVariable = "IPMAN_TEST_GATEWAY";
    public const string DnsVariable = "IPMAN_TEST_DNS";
    public const string EvidenceRootVariable = "IPMAN_TEST_EVIDENCE_ROOT";

    public const string IsolatedAcknowledgement = "YES_DISPOSABLE_ISOLATED_ADAPTER";
    public const string FilterAcknowledgement = "DESTRUCTIVE_NETWORK_FILTER_APPLIED";
    public const string ConflictAcknowledgement = "YES_ACCEPT_IP_CONFLICT_RISK";

    private const string NoneValue = "NONE";

    public static DestructiveNetworkTestSettingsResult Parse(
        IReadOnlyDictionary<string, string?> values,
        string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        List<string> reasons = new();

        RequireExact(values, EnableVariable, "1", reasons);
        RequireExact(values, IsolatedVariable, IsolatedAcknowledgement, reasons);
        RequireExact(values, FilterVariable, FilterAcknowledgement, reasons);
        RequireExact(values, ConflictVariable, ConflictAcknowledgement, reasons);

        NetworkAdapterId? adapterId = ParseAdapterId(Get(values, AdapterVariable), reasons);
        Sprint08Scenario? scenario = ParseScenario(Get(values, ScenarioVariable), reasons);
        string? ipv4 = ParseRequiredIpv4(Get(values, Ipv4Variable), Ipv4Variable, reasons);
        string? subnet = ParseRequiredIpv4(Get(values, SubnetVariable), SubnetVariable, reasons);
        string? gateway = ParseOptionalIpv4(Get(values, GatewayVariable), GatewayVariable, reasons);
        string[]? dns = ParseDns(Get(values, DnsVariable), reasons);

        if (adapterId is null || scenario is null || ipv4 is null || subnet is null || dns is null)
        {
            return new DestructiveNetworkTestSettingsResult(null, reasons);
        }

        ValidateScenarioShape(scenario.Value, gateway, dns, reasons);

        if (reasons.Count > 0)
        {
            return new DestructiveNetworkTestSettingsResult(null, reasons);
        }

        string evidenceRoot = Get(values, EvidenceRootVariable) is { Length: > 0 } configuredRoot
            ? Path.GetFullPath(configuredRoot)
            : Path.Combine(repositoryRoot, "artifacts", "integration", "sprint08");

        StaticIpv4Configuration desired = new(
            ipv4,
            subnet,
            gateway,
            dns.Length > 0 ? dns[0] : null,
            dns.Length > 1 ? dns[1] : null);

        return new DestructiveNetworkTestSettingsResult(
            new DestructiveNetworkTestSettings(adapterId.Value, scenario.Value, desired, evidenceRoot),
            Array.Empty<string>());
    }

    private static NetworkAdapterId? ParseAdapterId(string? value, List<string> reasons)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            reasons.Add($"Missing {AdapterVariable}; an exact adapter GUID is required.");
            return null;
        }

        if (!Guid.TryParse(value, out Guid parsed) || parsed == Guid.Empty)
        {
            reasons.Add($"Invalid {AdapterVariable}; supply one non-empty interface GUID.");
            return null;
        }

        return new NetworkAdapterId(parsed.ToString("B"));
    }

    private static Sprint08Scenario? ParseScenario(string? value, List<string> reasons)
    {
        if (!Enum.TryParse(value, ignoreCase: false, out Sprint08Scenario scenario) ||
            !Enum.IsDefined(scenario))
        {
            reasons.Add($"Missing or invalid {ScenarioVariable}; choose one documented Sprint 08 scenario.");
            return null;
        }

        return scenario;
    }

    private static string? ParseRequiredIpv4(
        string? value,
        string variable,
        List<string> reasons)
    {
        if (TryNormalizeIpv4(value, out string? normalized))
        {
            return normalized;
        }

        reasons.Add($"Missing or invalid {variable}; a valid IPv4 value is required.");
        return null;
    }

    private static string? ParseOptionalIpv4(
        string? value,
        string variable,
        List<string> reasons)
    {
        if (string.Equals(value, NoneValue, StringComparison.Ordinal))
        {
            return null;
        }

        if (TryNormalizeIpv4(value, out string? normalized))
        {
            return normalized;
        }

        reasons.Add($"Missing or invalid {variable}; use NONE or one IPv4 value.");
        return null;
    }

    private static string[]? ParseDns(string? value, List<string> reasons)
    {
        if (string.Equals(value, NoneValue, StringComparison.Ordinal))
        {
            return Array.Empty<string>();
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            reasons.Add($"Missing {DnsVariable}; use NONE or one/two comma-separated IPv4 values.");
            return null;
        }

        string[] candidates = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (candidates.Length is < 1 or > 2 ||
            candidates.Any(candidate => !TryNormalizeIpv4(candidate, out _)))
        {
            reasons.Add($"Invalid {DnsVariable}; use NONE or one/two comma-separated IPv4 values.");
            return null;
        }

        return candidates
            .Select(candidate => TryNormalizeIpv4(candidate, out string? normalized) ? normalized! : string.Empty)
            .ToArray();
    }

    private static void ValidateScenarioShape(
        Sprint08Scenario scenario,
        string? gateway,
        IReadOnlyList<string> dns,
        List<string> reasons)
    {
        if (scenario == Sprint08Scenario.ClearGateway && gateway is not null)
        {
            reasons.Add("ClearGateway requires IPMAN_TEST_GATEWAY=NONE.");
        }

        if (scenario == Sprint08Scenario.SetGateway && gateway is null)
        {
            reasons.Add("SetGateway requires an explicit gateway IPv4 value.");
        }

        if (scenario == Sprint08Scenario.ManualDnsOne && dns.Count != 1)
        {
            reasons.Add("ManualDnsOne requires exactly one DNS server.");
        }

        if (scenario == Sprint08Scenario.ManualDnsTwo && dns.Count != 2)
        {
            reasons.Add("ManualDnsTwo requires exactly two DNS servers.");
        }

        if (scenario == Sprint08Scenario.AutomaticDns && dns.Count != 0)
        {
            reasons.Add("AutomaticDns requires IPMAN_TEST_DNS=NONE.");
        }
    }

    private static void RequireExact(
        IReadOnlyDictionary<string, string?> values,
        string variable,
        string expected,
        List<string> reasons)
    {
        if (!string.Equals(Get(values, variable), expected, StringComparison.Ordinal))
        {
            reasons.Add($"{variable} must exactly equal {expected}.");
        }
    }

    private static string? Get(IReadOnlyDictionary<string, string?> values, string key) =>
        values.TryGetValue(key, out string? value) ? value : null;

    private static bool TryNormalizeIpv4(string? value, out string? normalized)
    {
        if (IPAddress.TryParse(value, out IPAddress? parsed) &&
            parsed.AddressFamily == AddressFamily.InterNetwork)
        {
            normalized = parsed.ToString();
            return true;
        }

        normalized = null;
        return false;
    }
}
