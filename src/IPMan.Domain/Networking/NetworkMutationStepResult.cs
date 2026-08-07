namespace IPMan.Domain.Networking;

public sealed record NetworkMutationStepResult(
    NetworkMutationStepStatus Status,
    uint? TechnicalCode = null)
{
    public bool IsSuccessful =>
        Status is NetworkMutationStepStatus.NotRequired or
            NetworkMutationStepStatus.Succeeded or
            NetworkMutationStepStatus.SucceededRestartRequired or
            NetworkMutationStepStatus.SucceededProvisionally;

    public bool WasAttempted => Status is not NetworkMutationStepStatus.NotAttempted and
        not NetworkMutationStepStatus.NotRequired;

    public bool RequiresRestart => Status == NetworkMutationStepStatus.SucceededRestartRequired;

    public static NetworkMutationStepResult NotAttempted() => new(NetworkMutationStepStatus.NotAttempted);

    public static NetworkMutationStepResult NotRequired() => new(NetworkMutationStepStatus.NotRequired);
}
