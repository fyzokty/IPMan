namespace IPMan.Application.Networking;

/// <summary>
/// Tuning values for <see cref="AdapterRefreshCoordinator"/>.
/// </summary>
public sealed class AdapterRefreshCoordinatorOptions
{
    /// <summary>
    /// Window used to merge a burst of Windows network events into one discovery
    /// pass. This is event consolidation, not polling.
    /// </summary>
    public TimeSpan DebounceWindow { get; init; } = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Interval of the low-frequency reconciliation fallback (ADR-010). Windows
    /// network events remain the primary trigger; this only lets the application
    /// self-heal when an event is missed by Windows, a driver or the application.
    /// <para>
    /// A non-positive value disables the fallback. Sub-second values are rejected
    /// because they would amount to polling.
    /// </para>
    /// </summary>
    public TimeSpan ReconciliationInterval { get; init; } = TimeSpan.FromSeconds(15);
}
