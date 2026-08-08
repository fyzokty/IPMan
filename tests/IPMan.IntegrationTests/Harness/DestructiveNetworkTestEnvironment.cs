namespace IPMan.IntegrationTests.Harness;

public static class DestructiveNetworkTestEnvironment
{
    private static readonly string[] VariableNames =
    {
        DestructiveNetworkTestSettingsParser.EnableVariable,
        DestructiveNetworkTestSettingsParser.AdapterVariable,
        DestructiveNetworkTestSettingsParser.IsolatedVariable,
        DestructiveNetworkTestSettingsParser.FilterVariable,
        DestructiveNetworkTestSettingsParser.ConflictVariable,
        DestructiveNetworkTestSettingsParser.ScenarioVariable,
        DestructiveNetworkTestSettingsParser.Ipv4Variable,
        DestructiveNetworkTestSettingsParser.SubnetVariable,
        DestructiveNetworkTestSettingsParser.GatewayVariable,
        DestructiveNetworkTestSettingsParser.DnsVariable,
        DestructiveNetworkTestSettingsParser.EvidenceRootVariable
    };

    public static Dictionary<string, string?> Read() =>
        VariableNames.ToDictionary(
            name => name,
            Environment.GetEnvironmentVariable,
            StringComparer.Ordinal);
}
