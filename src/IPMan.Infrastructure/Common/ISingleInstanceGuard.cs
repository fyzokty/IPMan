using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("IPMan")]

namespace IPMan.Infrastructure.Common;

/// <summary>
/// Smallest abstraction over interactive-instance ownership so application
/// startup can be tested without an operating-system mutex. Intentionally
/// internal: it is a process-lifecycle detail, not an application contract.
/// </summary>
internal interface ISingleInstanceGuard : IDisposable
{
    /// <summary>Attempts to become the interactive instance without waiting.</summary>
    SingleInstanceAcquisition TryAcquire();

    /// <summary>Releases interactive-instance ownership when currently held.</summary>
    void Release();
}

/// <summary>Describes the result of attempting interactive-instance ownership.</summary>
internal enum SingleInstanceAcquisition
{
    Unavailable,
    Acquired,
    AcquiredAfterAbandonment
}
