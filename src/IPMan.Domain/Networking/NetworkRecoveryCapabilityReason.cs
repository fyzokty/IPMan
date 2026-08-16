namespace IPMan.Domain.Networking;

/// <summary>Stable reasons why a recovery snapshot cannot safely drive recovery.</summary>
public enum NetworkRecoveryCapabilityReason
{
    AdapterModeUnknown = 0,
    DnsModeUnknown = 1,
    ManualDnsServersMissing = 2,
    GatewayAddressFidelityMismatch = 3,
    StaticGatewayMetricMissing = 4,
    StaticIpv4SubnetMaskMissing = 5
}
