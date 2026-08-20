namespace IPMan.Application.Networking;

/// <summary>Application-level outcomes for restoring a recovery snapshot.</summary>
public enum RecoveryRestoreStatus
{
    /// <summary>Fresh Windows state confirms the restored configuration.</summary>
    VerifiedSuccess = 0,

    /// <summary>The adapter already matched the recorded configuration.</summary>
    NoChange = 1,

    /// <summary>No supported recovery snapshot exists for the adapter.</summary>
    NoSnapshotFound = 2,

    /// <summary>The snapshot store could not be read reliably.</summary>
    SnapshotUnreadable = 3,

    /// <summary>The snapshot cannot be represented by a supported apply request.</summary>
    UnsupportedSnapshot = 4,

    /// <summary>The selected adapter could not be found.</summary>
    AdapterUnavailable = 5,

    /// <summary>The selected adapter state could not be read reliably.</summary>
    AdapterReadFailed = 6,

    /// <summary>The freshly read adapter identity differs from the recorded identity.</summary>
    AdapterIdentityChanged = 7,

    /// <summary>A safety rule prevented mutation.</summary>
    SafetyBlocked = 8,

    /// <summary>A potential address conflict requires explicit caller confirmation.</summary>
    ConflictConfirmationRequired = 9,

    /// <summary>An indeterminate conflict probe requires explicit caller confirmation.</summary>
    ProbeIndeterminateConfirmationRequired = 10,

    /// <summary>The pre-mutation recovery state could not be persisted or established.</summary>
    RecoveryCaptureFailed = 11,

    /// <summary>The first required mutation step failed.</summary>
    MutationFailed = 12,

    /// <summary>A later mutation step failed after an earlier step succeeded.</summary>
    PartialFailure = 13,

    /// <summary>Bounded fresh-read verification did not confirm the restored state.</summary>
    VerificationFailed = 14,

    /// <summary>The adapter disappeared after mutation.</summary>
    AdapterUnavailableDuringVerification = 15,

    /// <summary>Cancellation was observed before the critical mutation began.</summary>
    Cancelled = 16,

    /// <summary>The current state was not complete enough for a reliable recovery snapshot.</summary>
    RecoveryStateUnavailable = 17
}
