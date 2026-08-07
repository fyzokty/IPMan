namespace IPMan.Application.Networking;

/// <summary>Singleton coordinator shared by every network mutation workflow.</summary>
public sealed class NetworkMutationCoordinator : INetworkMutationCoordinator, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _isDisposed;

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Lease(_gate);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _gate.Dispose();
    }

    private sealed class Lease : IDisposable
    {
        private SemaphoreSlim? _gate;

        public Lease(SemaphoreSlim gate) => _gate = gate;

        public void Dispose() => Interlocked.Exchange(ref _gate, null)?.Release();
    }
}
