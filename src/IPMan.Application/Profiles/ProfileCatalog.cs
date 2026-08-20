using IPMan.Domain.Profiles;

namespace IPMan.Application.Profiles;

/// <summary>
/// Maintains the ordered in-memory profile view and refreshes it after persistence changes.
/// </summary>
public sealed class ProfileCatalog : IProfileCatalog
{
    private static readonly IReadOnlyList<NetworkProfile> EmptyProfiles =
        Array.Empty<NetworkProfile>();
    private static readonly IReadOnlyList<NetworkProfileProblem> EmptyProblems =
        Array.Empty<NetworkProfileProblem>();

    private readonly IProfileRepository _repository;
    private readonly IProfileDirectoryWatcher _watcher;
    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private readonly object _sync = new();

    private IReadOnlyList<NetworkProfile> _profiles = EmptyProfiles;
    private IReadOnlyList<NetworkProfileProblem> _problems = EmptyProblems;
    private CancellationTokenSource? _watchingCancellation;
    private bool _isWatching;
    private bool _isDisposed;

    /// <summary>Initializes the catalog with its persistence and directory dependencies.</summary>
    public ProfileCatalog(
        IProfileRepository repository,
        IProfileDirectoryWatcher watcher)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(watcher);

        _repository = repository;
        _watcher = watcher;
    }

    /// <inheritdoc />
    public IReadOnlyList<NetworkProfile> Profiles
    {
        get
        {
            lock (_sync)
            {
                return _profiles;
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<NetworkProfileProblem> Problems
    {
        get
        {
            lock (_sync)
            {
                return _problems;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken) =>
        ReloadAsync(cancellationToken);

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

            _watchingCancellation = new CancellationTokenSource();
            _isWatching = true;
            _watcher.Changed += OnDirectoryChanged;

            try
            {
                _watcher.StartWatching();
            }
            catch
            {
                _watcher.Changed -= OnDirectoryChanged;
                _isWatching = false;
                _watchingCancellation.Dispose();
                _watchingCancellation = null;
                throw;
            }
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
            _watcher.Changed -= OnDirectoryChanged;
            _watcher.StopWatching();

            cancellation = _watchingCancellation;
            _watchingCancellation = null;
        }

        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    /// <inheritdoc />
    public async Task<ProfileSaveResult> SaveAsync(
        NetworkProfile profile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);

        ProfileSaveResult result = await _repository
            .SaveAsync(profile, cancellationToken)
            .ConfigureAwait(false);
        await ReloadAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <inheritdoc />
    public async Task<ProfileDeleteResult> DeleteAsync(
        string profileId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);

        ProfileDeleteResult result = await _repository
            .DeleteAsync(profileId, cancellationToken)
            .ConfigureAwait(false);
        await ReloadAsync(cancellationToken).ConfigureAwait(false);
        return result;
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
        _watcher.Dispose();

        // The gate is not disposed because an asynchronous reload may still be
        // unwinding after StopWatching cancels its generation.
    }

    private void OnDirectoryChanged(object? sender, EventArgs e)
    {
        CancellationToken cancellationToken;

        lock (_sync)
        {
            if (!_isWatching || _watchingCancellation is null)
            {
                return;
            }

            cancellationToken = _watchingCancellation.Token;
        }

        _ = ReloadFromWatcherAsync(cancellationToken);
    }

    private async Task ReloadFromWatcherAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ReloadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Stopping a watcher generation cancels any queued reload.
        }
        catch (ObjectDisposedException)
        {
            // Teardown can race an event already delivered by the watcher.
        }
    }

    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        await _reloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            ProfileLoadResult result;

            try
            {
                result = await _repository
                    .LoadAllAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                Changed?.Invoke(this, EventArgs.Empty);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<NetworkProfile> orderedProfiles =
                ProfileOrdering.Order(result.Profiles);
            IReadOnlyList<NetworkProfileProblem> problems = result.Problems.ToArray();

            lock (_sync)
            {
                _profiles = orderedProfiles;
                _problems = problems;
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _reloadGate.Release();
        }
    }
}
