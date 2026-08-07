namespace IPMan.Domain.Networking;

/// <summary>
/// Validated low-level plan. <see cref="GatewayMutationMode.Clear"/> uses the
/// Microsoft-documented SetGateways clearing sentinel, while
/// <see cref="GatewayMutationMode.LeaveAbsent"/> is permitted only when the
/// fresh snapshot already contained no gateway.
/// </summary>
public sealed record StaticIpv4MutationPlan(
    StaticIpv4Configuration Configuration,
    GatewayMutationMode GatewayMode,
    NetworkConfigurationMode PreviousMode);
