namespace IPMan.Domain.Networking;

/// <summary>
/// Validated low-level plan. <see cref="GatewayMutationMode.Clear"/> uses an
/// exact-interface, store-aware default-route clear operation, while
/// <see cref="GatewayMutationMode.LeaveAbsent"/> is permitted only when the
/// fresh snapshot already contained no gateway and
/// <see cref="GatewayMutationMode.LeaveUnchanged"/> preserves an existing exact
/// gateway of an adapter that was already static (EnableStatic drops a
/// DHCP-provided gateway); its metric is the observed value, where 0 means the
/// Windows automatic metric, and is used only for verification.
/// <see cref="ApplyIpv4Address"/> explicitly controls whether the
/// address/subnet provider call is required. A <see cref="GatewayMutationMode.Set"/>
/// plan carries the exact metric (1..9999) selected by Application policy so
/// Infrastructure never has to infer whether a metric is preserved or defaulted.
/// </summary>
public sealed record StaticIpv4MutationPlan(
    StaticIpv4Configuration Configuration,
    bool ApplyIpv4Address,
    GatewayMutationMode GatewayMode,
    ushort? GatewayMetric,
    DnsMutationMode DnsMode,
    NetworkConfigurationMode PreviousMode);
