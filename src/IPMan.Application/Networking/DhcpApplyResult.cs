using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>Outcome of the complete DHCP apply transaction.</summary>
/// <param name="Status">The application-level outcome.</param>
/// <param name="SafetyBlock">The safety condition that stopped the operation, if any.</param>
/// <param name="Recovery">The snapshot persisted before mutation, if any.</param>
/// <param name="Mutation">The low-level mutation result, if mutation was reached.</param>
/// <param name="ActualSnapshot">The last state read from Windows after mutation, if available.</param>
public sealed record DhcpApplyResult(
    DhcpApplyStatus Status,
    StaticIpv4SafetyBlock SafetyBlock = StaticIpv4SafetyBlock.None,
    RecoverySnapshotReference? Recovery = null,
    NetworkApplyResult? Mutation = null,
    NetworkAdapterSnapshot? ActualSnapshot = null);
