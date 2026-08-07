using IPMan.Application.Common;
using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>
/// Default refresh coordinator.
/// <para>
/// Network event handlers only record a pending reason and release a signal.
/// A single background worker debounces the signal, runs one discovery pass at
/// a time and publishes added/removed/changed results.
/// </para>
/// <para>
/// A low-frequency reconciliation fallback (ADR-010) requests a pass through the
/// same coalescing path, so it can never overlap an active discovery pass. It is
/// a self-healing safety net for missed Windows events, not a polling loop.
/// </para>
/// </summary>
public sealed class AdapterRefreshCoordinator : IAdapterRefreshCoordinator
{
    /// <summary>
    /// Reconciliation intervals below this are rejected as polling (ADR-004).
    /// </summary>
    public static readonly TimeSpan MinimumReconciliationInterval = TimeSpan.FromSeconds(1);

    private static readonly IReadOnlyList<NetworkAdapterSnapshot> EmptyAdapters =
        Array.Empty<NetworkAdapterSnapshot>();

    private readonly INetworkAdapterReader _reader;
    private readonly INetworkChangeMonitor _monitor;
    private readonly IDelayProvider _delayProvider;
    private readonly TimeSpan _debounceWindow;
    private readonly TimeSpan _reconciliationInterval;

    private readonly object _sync = new();

    // Async auto-reset event with at most one pending signal. A fresh instance is
    // created per start/stop generation so a worker that is still unwinding after
    // a stop cannot consume the signal belonging to the next generation.
    private SemaphoreSlim? _signal;
    private CancellationTokenSource? _cancellation;
    private bool _isRunning;
    private bool _isDisposed;
    private string _pendingReason = NetworkChangeReason.ManualRefresh;

    // Only touched by the worker loop.
    private IReadOnlyList<NetworkAdapterSnapshot> _lastPublished = EmptyAdapters;

    public AdapterRefreshCoordinator(
        INetworkAdapterReader reader,
        INetworkChangeMonitor monitor,
        IDelayProvider delayProvider,
        AdapterRefreshCoordinatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(delayProvider);
        ArgumentNullException.ThrowIfNull(options);

        if (options.ReconciliationInterval > TimeSpan.Zero &&
            options.ReconciliationInterval < MinimumReconciliationInterval)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.ReconciliationInterval,
                "Sub-second reconciliation would be polling, which the architecture forbids.");
        }

        _reader = reader;
        _monitor = monitor;
        _delayProvider = delayProvider;
        _debounceWindow = options.DebounceWindow;
        _reconciliationInterval = options.ReconciliationInterval;
    }

    public event EventHandler<AdapterRefreshedEventArgs>? Refreshed;

    public event EventHandler<AdapterRefreshFailedEventArgs>? RefreshFailed;

    public void StartCoordinating()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (_isRunning)
            {
                return;
            }

            _cancellation = new CancellationTokenSource();
            _signal = new SemaphoreSlim(0, 1);
            _isRunning = true;

            _monitor.Changed += OnNetworkEnvironmentChanged;
            _monitor.StartMonitoring();

            SemaphoreSlim signal = _signal;
            CancellationToken token = _cancellation.Token;
            _ = Task.Run(() => RunAsync(signal, token), CancellationToken.None);

            if (_reconciliationInterval > TimeSpan.Zero)
            {
                _ = Task.Run(() => RunReconciliationAsync(token), CancellationToken.None);
            }
        }

        RequestRefresh(NetworkChangeReason.InitialDiscovery);
    }

    public void StopCoordinating()
    {
        CancellationTokenSource? cancellation;

        lock (_sync)
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
            _monitor.Changed -= OnNetworkEnvironmentChanged;
            _monitor.StopMonitoring();

            cancellation = _cancellation;
            _cancellation = null;
            _signal = null;
        }

        // The worker observes cancellation and stops publishing. This method does
        // not block, so it stays safe to call from the UI thread during shutdown.
        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    public void RequestRefresh(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        lock (_sync)
        {
            if (!_isRunning || _signal is null)
            {
                return;
            }

            _pendingReason = reason;

            if (_signal.CurrentCount == 0)
            {
                _signal.Release();
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
        }

        StopCoordinating();

        // The generation's signal is intentionally not disposed: the background
        // worker may still be unwinding, and SemaphoreSlim only requires disposal
        // when its AvailableWaitHandle is used, which it is not here.
    }

    private void OnNetworkEnvironmentChanged(object? sender, NetworkEnvironmentChangedEventArgs e)
    {
        // Deliberately minimal: no discovery work on the OS event thread.
        RequestRefresh(e.Reason);
    }

    private async Task RunAsync(SemaphoreSlim signal, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await signal.WaitAsync(cancellationToken).ConfigureAwait(false);
                await _delayProvider.DelayAsync(_debounceWindow, cancellationToken).ConfigureAwait(false);

                // Absorb every signal raised during the debounce window so a
                // burst of Windows events results in a single discovery pass.
                DrainPendingSignals(signal);

                string reason = TakePendingReason();
                await RefreshOnceAsync(reason, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Reliability fallback: periodically asks for a pass through the normal
    /// coalescing path. It never reads adapters itself, so it cannot overlap or
    /// race an in-flight discovery pass, and it stops with the generation token.
    /// </summary>
    private async Task RunReconciliationAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _delayProvider
                    .DelayAsync(_reconciliationInterval, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            RequestRefresh(NetworkChangeReason.ScheduledReconciliation);
        }
    }

    private async Task RefreshOnceAsync(string reason, CancellationToken cancellationToken)
    {
        IReadOnlyList<NetworkAdapterSnapshot> adapters;

        try
        {
            adapters = await _reader.GetAdaptersAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception failure)
        {
            // A failed discovery pass must not terminate network observation.
            if (!cancellationToken.IsCancellationRequested)
            {
                RefreshFailed?.Invoke(this, new AdapterRefreshFailedEventArgs(reason, failure));
            }

            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        IReadOnlyList<NetworkAdapterSnapshot> previous = _lastPublished;
        _lastPublished = adapters;

        Refreshed?.Invoke(
            this,
            new AdapterRefreshedEventArgs(
                reason,
                adapters,
                FindAdded(previous, adapters),
                FindRemoved(previous, adapters),
                FindChanged(previous, adapters)));
    }

    private static void DrainPendingSignals(SemaphoreSlim signal)
    {
        while (signal.Wait(0))
        {
            // Discard: the pending reason already reflects the newest request.
        }
    }

    private string TakePendingReason()
    {
        lock (_sync)
        {
            return _pendingReason;
        }
    }

    private static NetworkAdapterSnapshot[] FindAdded(
        IReadOnlyList<NetworkAdapterSnapshot> previous,
        IReadOnlyList<NetworkAdapterSnapshot> current)
    {
        HashSet<NetworkAdapterId> previousIds = ToIdSet(previous);
        return current.Where(adapter => !previousIds.Contains(adapter.Id)).ToArray();
    }

    private static NetworkAdapterSnapshot[] FindRemoved(
        IReadOnlyList<NetworkAdapterSnapshot> previous,
        IReadOnlyList<NetworkAdapterSnapshot> current)
    {
        HashSet<NetworkAdapterId> currentIds = ToIdSet(current);
        return previous.Where(adapter => !currentIds.Contains(adapter.Id)).ToArray();
    }

    private static NetworkAdapterSnapshot[] FindChanged(
        IReadOnlyList<NetworkAdapterSnapshot> previous,
        IReadOnlyList<NetworkAdapterSnapshot> current)
    {
        Dictionary<NetworkAdapterId, NetworkAdapterSnapshot> previousById = previous
            .GroupBy(adapter => adapter.Id)
            .ToDictionary(group => group.Key, group => group.First());

        return current
            .Where(adapter =>
                previousById.TryGetValue(adapter.Id, out NetworkAdapterSnapshot? before) &&
                before != adapter)
            .ToArray();
    }

    private static HashSet<NetworkAdapterId> ToIdSet(IReadOnlyList<NetworkAdapterSnapshot> adapters) =>
        adapters.Select(adapter => adapter.Id).ToHashSet();
}
