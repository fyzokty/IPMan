namespace IPMan.Application.Networking;

public enum StaticIpv4ApplyStatus
{
    VerifiedSuccess = 0,
    NoChange = 1,
    ValidationFailed = 2,
    AdapterUnavailable = 3,
    AdapterReadFailed = 4,
    SafetyBlocked = 5,
    ConflictConfirmationRequired = 6,
    ProbeIndeterminateConfirmationRequired = 7,
    RollbackCaptureFailed = 8,
    MutationFailed = 9,
    PartialFailure = 10,
    VerificationFailed = 11,
    AdapterUnavailableDuringVerification = 12,
    Cancelled = 13,
    RecoveryStateUnavailable = 14
}
