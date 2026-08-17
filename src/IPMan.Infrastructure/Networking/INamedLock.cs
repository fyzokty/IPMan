namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Smallest abstraction over a named operating-system lock so cross-process
/// coordination can be tested deterministically. Intentionally internal: it is
/// an infrastructure detail, not part of the application architecture.
/// </summary>
internal interface INamedLock
{
    /// <summary>
    /// Acquires the lock and reports whether ownership followed abandonment by
    /// the previous owner.
    /// </summary>
    Task<NamedLockAcquisition> AcquireAsync(CancellationToken cancellationToken);

    /// <summary>Releases the currently owned lock.</summary>
    void Release();
}

/// <summary>Describes how ownership of a named lock was obtained.</summary>
internal readonly record struct NamedLockAcquisition(bool WasAbandoned);
