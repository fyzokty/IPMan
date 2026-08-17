namespace IPMan.Infrastructure.Common;

/// <summary>Owns the per-session IPMan interactive-instance mutex.</summary>
internal sealed class WindowsSingleInstanceGuard : ISingleInstanceGuard
{
    private const string MutexName = @"Local\IPMan.SingleInstance";

    private Mutex? _mutex;
    private bool _ownsMutex;
    private bool _isDisposed;

    public SingleInstanceAcquisition TryAcquire()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_ownsMutex)
        {
            return SingleInstanceAcquisition.Acquired;
        }

        _mutex ??= new Mutex(initiallyOwned: false, MutexName);

        try
        {
            _ownsMutex = _mutex.WaitOne(millisecondsTimeout: 0);
            return _ownsMutex
                ? SingleInstanceAcquisition.Acquired
                : SingleInstanceAcquisition.Unavailable;
        }
        catch (AbandonedMutexException)
        {
            // WaitOne grants ownership when it reports abandonment. The prior
            // instance crashed, so this process becomes the normal owner.
            _ownsMutex = true;
            return SingleInstanceAcquisition.AcquiredAfterAbandonment;
        }
    }

    public void Release()
    {
        if (!_ownsMutex)
        {
            return;
        }

        _mutex!.ReleaseMutex();
        _ownsMutex = false;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        Release();
        _mutex?.Dispose();
        _isDisposed = true;
    }
}
