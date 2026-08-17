using IPMan.Infrastructure.Networking;

namespace IPMan.Tests.Fakes;

internal sealed class FakeNamedLock : INamedLock
{
    public NamedLockAcquisition Result { get; set; } = new(WasAbandoned: false);

    public TaskCompletionSource<NamedLockAcquisition>? PendingAcquisition { get; set; }

    public Action? OnAcquire { get; set; }

    public Action? OnRelease { get; set; }

    public int AcquireCount { get; private set; }

    public int ReleaseCount { get; private set; }

    public NamedLockAcquisition? LastAcquisition { get; private set; }

    public async Task<NamedLockAcquisition> AcquireAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AcquireCount++;
        OnAcquire?.Invoke();

        NamedLockAcquisition acquisition = PendingAcquisition is null
            ? Result
            : await PendingAcquisition.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        LastAcquisition = acquisition;
        return acquisition;
    }

    public void Release()
    {
        ReleaseCount++;
        OnRelease?.Invoke();
    }
}
