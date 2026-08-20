namespace IPMan.Infrastructure.Profiles;

/// <summary>
/// Smallest abstraction over <see cref="FileSystemWatcher"/> so profile watcher
/// debounce and lifecycle behavior can be tested without observing a real directory.
/// </summary>
internal interface IProfileDirectoryEventSource : IDisposable
{
    /// <summary>Attaches and starts delivering raw directory change notifications.</summary>
    void Subscribe(Action onDirectoryChanged);

    /// <summary>Detaches the handler and stops delivering notifications.</summary>
    void Unsubscribe(Action onDirectoryChanged);
}

/// <summary>Produces raw notifications from a real profile directory.</summary>
internal sealed class SystemProfileDirectoryEventSource : IProfileDirectoryEventSource
{
    private readonly string _profilesDirectory;
    private readonly object _sync = new();

    private FileSystemWatcher? _watcher;
    private Action? _handler;
    private bool _isDisposed;

    public SystemProfileDirectoryEventSource(string profilesDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profilesDirectory);
        _profilesDirectory = Path.GetFullPath(profilesDirectory);
    }

    public void Subscribe(Action onDirectoryChanged)
    {
        ArgumentNullException.ThrowIfNull(onDirectoryChanged);

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (_handler is not null)
            {
                return;
            }

            Directory.CreateDirectory(_profilesDirectory);

            FileSystemWatcher watcher = new(_profilesDirectory, "*.json")
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            watcher.Created += OnChanged;
            watcher.Changed += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnRenamed;

            _handler = onDirectoryChanged;
            _watcher = watcher;
            watcher.EnableRaisingEvents = true;
        }
    }

    public void Unsubscribe(Action onDirectoryChanged)
    {
        ArgumentNullException.ThrowIfNull(onDirectoryChanged);

        lock (_sync)
        {
            if (_handler is null)
            {
                return;
            }

            StopWatchingCore();
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
            StopWatchingCore();
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs e) => Notify();

    private void OnRenamed(object sender, RenamedEventArgs e) => Notify();

    private void Notify()
    {
        Action? handler;

        lock (_sync)
        {
            handler = _handler;
        }

        handler?.Invoke();
    }

    private void StopWatchingCore()
    {
        FileSystemWatcher? watcher = _watcher;
        _watcher = null;
        _handler = null;

        if (watcher is null)
        {
            return;
        }

        watcher.EnableRaisingEvents = false;
        watcher.Created -= OnChanged;
        watcher.Changed -= OnChanged;
        watcher.Deleted -= OnChanged;
        watcher.Renamed -= OnRenamed;
        watcher.Dispose();
    }
}
