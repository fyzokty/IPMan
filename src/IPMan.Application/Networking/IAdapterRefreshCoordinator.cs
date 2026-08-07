namespace IPMan.Application.Networking;

/// <summary>
/// Coalesces Windows network change notifications into a single, non-overlapping
/// adapter discovery pass and publishes the resulting adapter set.
/// </summary>
public interface IAdapterRefreshCoordinator : IDisposable
{
    event EventHandler<AdapterRefreshedEventArgs>? Refreshed;

    event EventHandler<AdapterRefreshFailedEventArgs>? RefreshFailed;

    /// <summary>Starts network observation and performs an initial discovery pass.</summary>
    void StartCoordinating();

    /// <summary>Stops network observation. Safe to call when not started.</summary>
    void StopCoordinating();

    /// <summary>
    /// Requests a discovery pass. Requests arriving inside the debounce window
    /// are merged into a single pass. Ignored when not started.
    /// </summary>
    void RequestRefresh(string reason);
}
