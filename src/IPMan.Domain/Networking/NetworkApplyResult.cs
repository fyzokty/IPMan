namespace IPMan.Domain.Networking;

/// <summary>Low-level mutation result retaining each WMI step and diagnostic return code.</summary>
public sealed record NetworkApplyResult(
    NetworkMutationStepResult Ipv4Step,
    NetworkMutationStepResult GatewayStep,
    NetworkMutationStepResult DnsStep,
    NetworkMutationFailureKind FailureKind,
    string? TechnicalMessage = null)
{
    public bool IsSuccess => FailureKind == NetworkMutationFailureKind.None &&
        Ipv4Step.IsSuccessful && GatewayStep.IsSuccessful && DnsStep.IsSuccessful;

    public bool IsPartialFailure =>
        Ipv4Step.IsSuccessful &&
        (!GatewayStep.IsSuccessful || !DnsStep.IsSuccessful);

    public bool WasMutationAttempted =>
        Ipv4Step.WasAttempted || GatewayStep.WasAttempted || DnsStep.WasAttempted;

    public bool RequiresRestart =>
        Ipv4Step.RequiresRestart || GatewayStep.RequiresRestart || DnsStep.RequiresRestart;
}
