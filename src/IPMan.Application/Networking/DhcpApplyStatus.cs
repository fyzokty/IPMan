namespace IPMan.Application.Networking;

/// <summary>Application-level outcomes for a DHCP transition.</summary>
public enum DhcpApplyStatus
{
    /// <summary>Fresh Windows state confirms DHCP and automatic DNS.</summary>
    VerifiedSuccess = 0,

    /// <summary>The adapter already used DHCP and automatic DNS.</summary>
    NoChange = 1,

    /// <summary>The selected adapter could not be found.</summary>
    AdapterUnavailable = 2,

    /// <summary>The selected adapter state could not be read reliably.</summary>
    AdapterReadFailed = 3,

    /// <summary>A safety rule prevented mutation.</summary>
    SafetyBlocked = 4,

    /// <summary>The pre-mutation recovery snapshot could not be persisted.</summary>
    RecoveryCaptureFailed = 5,

    /// <summary>The first required mutation step failed.</summary>
    MutationFailed = 6,

    /// <summary>A later mutation step failed after DHCP had been enabled.</summary>
    PartialFailure = 7,

    /// <summary>Bounded fresh-read verification did not confirm the target state.</summary>
    VerificationFailed = 8,

    /// <summary>The adapter disappeared after mutation.</summary>
    AdapterUnavailableDuringVerification = 9,

    /// <summary>Cancellation was observed before the critical mutation began.</summary>
    Cancelled = 10,

    /// <summary>The current state was not complete enough for a reliable recovery snapshot.</summary>
    RecoveryStateUnavailable = 11
}
