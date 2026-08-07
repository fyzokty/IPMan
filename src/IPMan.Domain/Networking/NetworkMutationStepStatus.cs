namespace IPMan.Domain.Networking;

public enum NetworkMutationStepStatus
{
    NotAttempted = 0,
    NotRequired = 1,
    Succeeded = 2,
    SucceededRestartRequired = 3,
    Failed = 4,
    /// <summary>
    /// The native method reported its documented prior-state-dependent success
    /// code. A fresh post-mutation verification remains authoritative.
    /// </summary>
    SucceededProvisionally = 5
}
