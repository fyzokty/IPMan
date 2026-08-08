namespace IPMan.Domain.Networking;

/// <summary>
/// Validated low-level plan. <see cref="GatewayMutationMode.Clear"/> uses the
/// Microsoft-documented SetGateways clearing sentinel, while
/// <see cref="GatewayMutationMode.LeaveAbsent"/> is permitted only when the
/// fresh snapshot already contained no gateway. A <see cref="GatewayMutationMode.Set"/>
/// plan carries the exact metric selected by Application policy so Infrastructure
/// never has to infer whether a metric is preserved or defaulted.
/// </summary>
public sealed record StaticIpv4MutationPlan(
    StaticIpv4Configuration Configuration,
    GatewayMutationMode GatewayMode,
    ushort? GatewayMetric,
    DnsMutationMode DnsMode,
    NetworkConfigurationMode PreviousMode);
