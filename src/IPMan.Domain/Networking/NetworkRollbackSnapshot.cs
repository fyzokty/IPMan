namespace IPMan.Domain.Networking;

/// <summary>Persisted recovery state captured before any Windows mutation begins.</summary>
public sealed record NetworkRollbackSnapshot(
    int SchemaVersion,
    string SnapshotId,
    DateTimeOffset CapturedAtUtc,
    RollbackSnapshotState State,
    NetworkAdapterId AdapterId,
    string AdapterName,
    string AdapterDescription,
    NetworkConfigurationMode Mode,
    Ipv4AddressAssignment[] Ipv4Addresses,
    Ipv4GatewayRecoveryState[] Ipv4Gateways,
    DnsConfigurationMode DnsMode,
    string[] ConfiguredIpv4DnsServers,
    string[] Ipv4DnsServers);
