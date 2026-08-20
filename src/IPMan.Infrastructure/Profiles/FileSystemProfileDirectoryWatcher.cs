using IPMan.Application.Common;
using IPMan.Application.Profiles;

namespace IPMan.Infrastructure.Profiles;

/// <summary>Debounces profile document directory notifications.</summary>
public sealed class FileSystemProfileDirectoryWatcher : IProfileDirectoryWatcher
{
    /// <summary>Largest supported debounce window.</summary>
    public static readonly TimeSpan MaximumDebounceWindow = TimeSpan.FromSeconds(5);

    private readonly IProfileDirectoryEventSource _eventSource;
    private readonly IDelayProvider _delayProvider;
    private readonly TimeSpan _debounceWindow;
    private readonly Action _handler;
    private readonly object _sync = new();

    private SemaphoreSlim? _signal;
    private CancellationTokenSource? _cancellation;
    private bool _isWatching;
    private bool _isDisposed;

    /// <summary>Initializes a watcher for the configured profile directory.</summary>
    public FileSystemProfileDirectoryWatcher(
        IDelayProvider delayProvider,
        ProfileWatcherOptions options,
        ProfileRepositoryOptions repositoryOptions)
        : this(
            CreateEventSource(repositoryOptions),
            delayProvider,
            options)
    {
    }

    internal FileSystemProfileDirectoryWatcher(
        IProfileDirectoryEventSource eventSource,
        IDelayProvider delayProvider,
        ProfileWatcherOptions options)
    {
        ArgumentNullException.ThrowIfNull(eventSource);
        ArgumentNullException.ThrowIfNull(delayProvider);
        ArgumentNullException.ThrowIfNull(options);

        if (options.DebounceWindow <= TimeSpan.Zero ||
            options.DebounceWindow > MaximumDebounceWindow)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.DebounceWindow,
                "Profile watcher debounce window must be positive and bounded.");
        }

        _eventSource = eventSource;
        _delayProvider = delayProvider;
        _debounceWindow = options.DebounceWindow;
        _handler = OnDirectoryChanged;
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public void StartWatching()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (_isWatching)
            {
                return;
            }

            _cancellation = new CancellationTokenSource();
            _signal = new SemaphoreSlim(0, 1);
            _isWatching = true;

            try
            {
                _eventSource.Subscribe(_handler);
            }
            catch
            {
                _isWatching = false;
                _cancellation.Dispose();
                _cancellation = null;
                _signal = null;
                throw;
            }

            SemaphoreSlim signal = _signal;
            CancellationToken cancellationToken = _cancellation.Token;
            _ = Task.Run(
                () => RunAsync(signal, cancellationToken),
                CancellationToken.None);
        }
    }

    /// <inheritdoc />
    public void StopWatching()
    {
        CancellationTokenSource? cancellation;

        lock (_sync)
        {
            if (!_isWatching)
            {
                return;
            }

            _isWatching = false;
            _eventSource.Unsubscribe(_handler);

            cancellation = _cancellation;
            _cancellation = null;
            _signal = null;
        }

        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    /// <inheritdoc />
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

        StopWatching();
        _eventSource.Dispose();

        // The generation's signal remains undisposed while its worker unwinds;
        // SemaphoreSlim needs disposal only when AvailableWaitHandle is used.
    }

    private static SystemProfileDirectoryEventSource CreateEventSource(
        ProfileRepositoryOptions repositoryOptions)
    {
        ArgumentNullException.ThrowIfNull(repositoryOptions);
        return new SystemProfileDirectoryEventSource(repositoryOptions.ProfilesDirectory);
    }

    private void OnDirectoryChanged()
    {
        lock (_sync)
        {
            if (!_isWatching || _signal is null)
            {
                return;
            }

            if (_signal.CurrentCount == 0)
            {
                _signal.Release();
            }
        }
    }

    private async Task RunAsync(
        SemaphoreSlim signal,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await signal.WaitAsync(cancellationToken).ConfigureAwait(false);
                await _delayProvider
                    .DelayAsync(_debounceWindow, cancellationToken)
                    .ConfigureAwait(false);

                DrainPendingSignals(signal);

                if (!cancellationToken.IsCancellationRequested)
                {
                    RaiseChanged();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception)
            {
                // A faulty notification consumer must not terminate observation.
            }
        }
    }

    private void RaiseChanged()
    {
        EventHandler? handlers = Changed;

        if (handlers is null)
        {
            return;
        }

        foreach (EventHandler handler in handlers.GetInvocationList().Cast<EventHandler>())
        {
            try
            {
                handler(this, EventArgs.Empty);
            }
            catch (Exception)
            {
                // One consumer cannot prevent later consumers or future events.
            }
        }
    }

    private static void DrainPendingSignals(SemaphoreSlim signal)
    {
        while (signal.Wait(0))
        {
            // Discard: all raw events in this window represent one directory change.
        }
    }
}
