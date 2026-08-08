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
    public NetworkRecoveryCapabilityEvaluation RestoreCapability =>
        NetworkRecoveryCapabilityEvaluator.Evaluate(this);

    public bool IsRestoreCapable => RestoreCapability.IsCapable;
}
