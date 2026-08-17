using IPMan.Application.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeNetworkMutationCoordinator : INetworkMutationCoordinator
{
    public Exception? AcquireException { get; set; }

    public Action? OnAcquire { get; set; }

    public Action? OnRelease { get; set; }

    public int AcquireCount { get; private set; }

    public int ReleaseCount { get; private set; }

    public Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AcquireCount++;
        OnAcquire?.Invoke();

        if (AcquireException is not null)
        {
            return Task.FromException<IDisposable>(AcquireException);
        }

        return Task.FromResult<IDisposable>(new Lease(this));
    }

    private sealed class Lease : IDisposable
    {
        private FakeNetworkMutationCoordinator? _owner;

        public Lease(FakeNetworkMutationCoordinator owner) => _owner = owner;

        public void Dispose()
        {
            FakeNetworkMutationCoordinator? owner = Interlocked.Exchange(ref _owner, null);

            if (owner is null)
            {
                return;
            }

            owner.ReleaseCount++;
            owner.OnRelease?.Invoke();
        }
    }
}
