using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public sealed record StaticIpv4ApplyResult(
    StaticIpv4ApplyStatus Status,
    NetworkConfigurationPreflightResult? Preflight = null,
    StaticIpv4SafetyBlock SafetyBlock = StaticIpv4SafetyBlock.None,
    RecoverySnapshotReference? Recovery = null,
    NetworkApplyResult? Mutation = null,
    NetworkAdapterSnapshot? ActualSnapshot = null,
    NetworkConfigurationComparisonResult? VerificationComparison = null);
