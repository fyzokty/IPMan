using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>Outcome of restoring an adapter from its latest recovery snapshot.</summary>
/// <param name="Status">The restore outcome.</param>
/// <param name="RestoredFrom">The snapshot selected for restoration, when available.</param>
/// <param name="SafetyBlock">The safety condition that stopped restoration, if any.</param>
/// <param name="StaticOutcome">The delegated static apply outcome, when the static path ran.</param>
/// <param name="DhcpOutcome">The delegated DHCP apply outcome, when the DHCP path ran.</param>
public sealed record RecoveryRestoreResult(
    RecoveryRestoreStatus Status,
    RecoverySnapshot? RestoredFrom = null,
    StaticIpv4SafetyBlock SafetyBlock = StaticIpv4SafetyBlock.None,
    StaticIpv4ApplyResult? StaticOutcome = null,
    DhcpApplyResult? DhcpOutcome = null);
