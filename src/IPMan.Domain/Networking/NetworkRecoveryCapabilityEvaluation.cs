namespace IPMan.Domain.Networking;

/// <summary>Authoritative restore-capability evaluation for one recovery snapshot.</summary>
public sealed record NetworkRecoveryCapabilityEvaluation(
    IReadOnlyList<NetworkRecoveryCapabilityReason> BlockingReasons)
{
    public bool IsCapable => BlockingReasons.Count == 0;
}
