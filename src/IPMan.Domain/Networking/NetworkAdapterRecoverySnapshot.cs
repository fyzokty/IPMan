namespace IPMan.Domain.Networking;

/// <summary>
/// Exact-identity Windows state used immediately before rollback capture and mutation.
/// </summary>
public sealed record NetworkAdapterRecoverySnapshot(
    NetworkAdapterSnapshot Adapter,
    DnsConfigurationMode DnsMode,
    string[] ConfiguredIpv4DnsServers,
    Ipv4GatewayRecoveryState[] Ipv4Gateways)
{
    public bool IsRestoreCapable =>
        Adapter.Mode != NetworkConfigurationMode.Unknown &&
        DnsMode != DnsConfigurationMode.Unknown &&
        (DnsMode != DnsConfigurationMode.Manual || ConfiguredIpv4DnsServers.Length > 0) &&
        Adapter.Ipv4Gateways.SequenceEqual(
            Ipv4Gateways.Select(gateway => gateway.Address),
            StringComparer.Ordinal) &&
        (Adapter.Mode != NetworkConfigurationMode.Static ||
            Ipv4Gateways.All(gateway => gateway.Metric.HasValue)) &&
        (Adapter.Mode != NetworkConfigurationMode.Static ||
            Adapter.Ipv4Addresses.All(address => address.SubnetMask is not null));
}
