namespace IPMan.Domain.Networking;

public static class NetworkRecoveryCapabilityEvaluator
{
    public static NetworkRecoveryCapabilityEvaluation Evaluate(
        NetworkAdapterRecoverySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        List<NetworkRecoveryCapabilityReason> reasons = new();

        if (snapshot.Adapter.Mode == NetworkConfigurationMode.Unknown)
        {
            reasons.Add(NetworkRecoveryCapabilityReason.AdapterModeUnknown);
        }

        if (snapshot.DnsMode == DnsConfigurationMode.Unknown)
        {
            reasons.Add(NetworkRecoveryCapabilityReason.DnsModeUnknown);
        }

        if (snapshot.DnsMode == DnsConfigurationMode.Manual &&
            snapshot.ConfiguredIpv4DnsServers.Length == 0)
        {
            reasons.Add(NetworkRecoveryCapabilityReason.ManualDnsServersMissing);
        }

        if (!snapshot.Adapter.Ipv4Gateways.SequenceEqual(
                snapshot.Ipv4Gateways.Select(gateway => gateway.Address),
                StringComparer.Ordinal))
        {
            reasons.Add(NetworkRecoveryCapabilityReason.GatewayAddressFidelityMismatch);
        }

        if (snapshot.Adapter.Mode == NetworkConfigurationMode.Static &&
            snapshot.Ipv4Gateways.Any(gateway => !gateway.Metric.HasValue))
        {
            reasons.Add(NetworkRecoveryCapabilityReason.StaticGatewayMetricMissing);
        }

        if (snapshot.Adapter.Mode == NetworkConfigurationMode.Static &&
            snapshot.Adapter.Ipv4Addresses.Any(address => address.SubnetMask is null))
        {
            reasons.Add(NetworkRecoveryCapabilityReason.StaticIpv4SubnetMaskMissing);
        }

        return new NetworkRecoveryCapabilityEvaluation(reasons.ToArray());
    }
}
