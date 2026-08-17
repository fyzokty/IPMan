using IPMan.Application.Networking;

namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Coordinates network mutations across processes before entering the existing
/// process-local mutation coordinator.
/// </summary>
public sealed class CrossProcessNetworkMutationCoordinator : INetworkMutationCoordinator
{
    private readonly INamedLock _namedLock;
    private readonly INetworkMutationCoordinator _innerCoordinator;

    /// <summary>Creates a coordinator backed by the machine-wide IPMan mutex.</summary>
    /// <param name="innerCoordinator">The process-local mutation coordinator.</param>
    public CrossProcessNetworkMutationCoordinator(NetworkMutationCoordinator innerCoordinator)
        : this(new GlobalNamedMutexLock(), innerCoordinator)
    {
    }

    internal CrossProcessNetworkMutationCoordinator(
        INamedLock namedLock,
        INetworkMutationCoordinator innerCoordinator)
    {
        ArgumentNullException.ThrowIfNull(namedLock);
        ArgumentNullException.ThrowIfNull(innerCoordinator);

        _namedLock = namedLock;
        _innerCoordinator = innerCoordinator;
    }

    /// <inheritdoc />
    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        await _namedLock.AcquireAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            IDisposable innerLease = await _innerCoordinator
                .AcquireAsync(cancellationToken)
                .ConfigureAwait(false);
            return new Lease(innerLease, _namedLock);
        }
        catch
        {
            _namedLock.Release();
            throw;
        }
    }

    private sealed class Lease : IDisposable
    {
        private Resources? _resources;

        public Lease(IDisposable innerLease, INamedLock namedLock) =>
            _resources = new Resources(innerLease, namedLock);

        public void Dispose()
        {
            Resources? resources = Interlocked.Exchange(ref _resources, null);

            if (resources is null)
            {
                return;
            }

            try
            {
                resources.InnerLease.Dispose();
            }
            finally
            {
                resources.NamedLock.Release();
            }
        }

        private sealed record Resources(IDisposable InnerLease, INamedLock NamedLock);
    }
}
