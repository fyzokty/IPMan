using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Tests.Fakes;

/// <summary>
/// Lets a test publish discovery results and failures directly, without running
/// the real coordinator's background worker.
/// </summary>
public sealed class FakeAdapterRefreshCoordinator : IAdapterRefreshCoordinator
{
    private IReadOnlyList<NetworkAdapterSnapshot> _lastPublished =
        Array.Empty<NetworkAdapterSnapshot>();

    public event EventHandler<AdapterRefreshedEventArgs>? Refreshed;

    public event EventHandler<AdapterRefreshFailedEventArgs>? RefreshFailed;

    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public int DisposeCount { get; private set; }

    public List<string> RefreshRequests { get; } = new();

    public void StartCoordinating() => StartCount++;

    public void StopCoordinating() => StopCount++;

    public void RequestRefresh(string reason) => RefreshRequests.Add(reason);

    public void Dispose() => DisposeCount++;

    /// <summary>Publishes a discovery result, computing the difference like the real coordinator.</summary>
    public void PublishRefresh(params NetworkAdapterSnapshot[] adapters)
    {
        PublishRefresh(NetworkChangeReason.NetworkAddressChanged, adapters);
    }

    public void PublishRefresh(string reason, params NetworkAdapterSnapshot[] adapters)
    {
        IReadOnlyList<NetworkAdapterSnapshot> previous = _lastPublished;
        _lastPublished = adapters;

        HashSet<NetworkAdapterId> previousIds = previous.Select(a => a.Id).ToHashSet();
        HashSet<NetworkAdapterId> currentIds = adapters.Select(a => a.Id).ToHashSet();

        Refreshed?.Invoke(
            this,
            new AdapterRefreshedEventArgs(
                reason,
                adapters,
                adapters.Where(a => !previousIds.Contains(a.Id)).ToArray(),
                previous.Where(a => !currentIds.Contains(a.Id)).ToArray(),
                adapters
                    .Where(a => previous.Any(before => before.Id == a.Id && before != a))
                    .ToArray()));
    }

    public void PublishFailure(Exception? failure = null) =>
        RefreshFailed?.Invoke(
            this,
            new AdapterRefreshFailedEventArgs(
                NetworkChangeReason.ScheduledReconciliation,
                failure ?? new InvalidOperationException("Discovery failed.")));
}
