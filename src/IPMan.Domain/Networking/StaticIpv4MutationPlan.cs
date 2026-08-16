namespace IPMan.Domain.Networking;

/// <summary>
/// Validated low-level plan. <see cref="GatewayMutationMode.Clear"/> uses an
/// exact-interface, store-aware default-route clear operation, while
/// <see cref="GatewayMutationMode.LeaveAbsent"/> is permitted only when the
/// fresh snapshot already contained no gateway and
/// <see cref="GatewayMutationMode.LeaveUnchanged"/> preserves an existing exact
/// gateway. <see cref="ApplyIpv4Address"/> explicitly controls whether the
/// address/subnet provider call is required. A <see cref="GatewayMutationMode.Set"/>
/// plan carries the exact metric selected by Application policy so Infrastructure
/// never has to infer whether a metric is preserved or defaulted.
/// </summary>
public sealed record StaticIpv4MutationPlan(
    StaticIpv4Configuration Configuration,
    bool ApplyIpv4Address,
    GatewayMutationMode GatewayMode,
    ushort? GatewayMetric,
    DnsMutationMode DnsMode,
    NetworkConfigurationMode PreviousMode);
