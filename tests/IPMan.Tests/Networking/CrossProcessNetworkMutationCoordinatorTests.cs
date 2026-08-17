using IPMan.Infrastructure.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class CrossProcessNetworkMutationCoordinatorTests
{
    [Fact]
    public async Task AcquireAsync_AcquiresNamedLockBeforeInnerLease()
    {
        List<string> events = new();
        FakeNamedLock namedLock = new() { OnAcquire = () => events.Add("named-acquire") };
        FakeNetworkMutationCoordinator inner = new() { OnAcquire = () => events.Add("inner-acquire") };
        CrossProcessNetworkMutationCoordinator coordinator = new(namedLock, inner);

        using IDisposable lease = await coordinator.AcquireAsync(CancellationToken.None);

        Assert.Collection(
            events,
            item => Assert.Equal("named-acquire", item),
            item => Assert.Equal("inner-acquire", item));
    }

    [Fact]
    public async Task Dispose_ReleasesInnerLeaseBeforeNamedLock()
    {
        List<string> events = new();
        FakeNamedLock namedLock = new() { OnRelease = () => events.Add("named-release") };
        FakeNetworkMutationCoordinator inner = new() { OnRelease = () => events.Add("inner-release") };
        CrossProcessNetworkMutationCoordinator coordinator = new(namedLock, inner);
        IDisposable lease = await coordinator.AcquireAsync(CancellationToken.None);

        lease.Dispose();

        Assert.Collection(
            events,
            item => Assert.Equal("inner-release", item),
            item => Assert.Equal("named-release", item));
    }

    [Fact]
    public async Task AcquireAsync_WhenInnerAcquisitionFails_ReleasesNamedLock()
    {
        FakeNamedLock namedLock = new();
        FakeNetworkMutationCoordinator inner = new()
        {
            AcquireException = new InvalidOperationException("Inner acquisition failed.")
        };
        CrossProcessNetworkMutationCoordinator coordinator = new(namedLock, inner);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.AcquireAsync(CancellationToken.None));

        Assert.Equal(1, namedLock.AcquireCount);
        Assert.Equal(1, namedLock.ReleaseCount);
    }

    [Fact]
    public async Task Dispose_WhenCalledTwice_ReleasesEachLockOnce()
    {
        FakeNamedLock namedLock = new();
        FakeNetworkMutationCoordinator inner = new();
        CrossProcessNetworkMutationCoordinator coordinator = new(namedLock, inner);
        IDisposable lease = await coordinator.AcquireAsync(CancellationToken.None);

        lease.Dispose();
        lease.Dispose();

        Assert.Equal(1, inner.ReleaseCount);
        Assert.Equal(1, namedLock.ReleaseCount);
    }

    [Fact]
    public async Task AcquireAsync_WhenCancelledWhileWaiting_ThrowsOperationCanceledException()
    {
        TaskCompletionSource<NamedLockAcquisition> pending =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        FakeNamedLock namedLock = new() { PendingAcquisition = pending };
        FakeNetworkMutationCoordinator inner = new();
        CrossProcessNetworkMutationCoordinator coordinator = new(namedLock, inner);
        using CancellationTokenSource cancellation = new();

        Task<IDisposable> acquisition = coordinator.AcquireAsync(cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => acquisition);
        Assert.Equal(0, inner.AcquireCount);
        Assert.Equal(0, namedLock.ReleaseCount);
    }

    [Fact]
    public async Task AcquireAsync_WhenNamedLockWasAbandoned_ContinuesWithReportedOwnership()
    {
        FakeNamedLock namedLock = new()
        {
            Result = new NamedLockAcquisition(WasAbandoned: true)
        };
        FakeNetworkMutationCoordinator inner = new();
        CrossProcessNetworkMutationCoordinator coordinator = new(namedLock, inner);

        using IDisposable lease = await coordinator.AcquireAsync(CancellationToken.None);

        Assert.True(namedLock.LastAcquisition?.WasAbandoned);
        Assert.Equal(1, inner.AcquireCount);
    }
}
