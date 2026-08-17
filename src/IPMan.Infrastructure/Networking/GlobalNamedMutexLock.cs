namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Owns the machine-wide network mutation mutex on a dedicated thread so the
/// thread-affine Windows mutex ownership never crosses an async continuation.
/// </summary>
internal sealed class GlobalNamedMutexLock : INamedLock
{
    private const string MutexName = @"Global\IPMan.NetworkMutation";

    private readonly object _sync = new();

    private AcquisitionRequest? _owner;

    public Task<NamedLockAcquisition> AcquireAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<NamedLockAcquisition>(cancellationToken);
        }

        AcquisitionRequest request = new(cancellationToken);
        Thread ownerThread = new(
            () => OwnMutex(request))
        {
            IsBackground = true,
            Name = "IPMan network mutation mutex owner"
        };

        try
        {
            ownerThread.Start();
        }
        catch (Exception exception)
        {
            request.Dispose();
            return Task.FromException<NamedLockAcquisition>(exception);
        }

        return request.Acquisition.Task;
    }

    public void Release()
    {
        AcquisitionRequest request;

        lock (_sync)
        {
            request = _owner ?? throw new SynchronizationLockException(
                "The global network mutation mutex is not owned.");
        }

        request.ReleaseRequested.Set();
        request.ReleaseCompleted.Task.GetAwaiter().GetResult();
    }

    private void OwnMutex(AcquisitionRequest request)
    {
        Mutex? mutex = null;
        bool ownsMutex = false;
        bool ownershipPublished = false;
        Exception? failure = null;

        try
        {
            mutex = new Mutex(initiallyOwned: false, MutexName);
            NamedLockWaitResult waitResult = WaitForOwnership(mutex, request.CancellationToken);

            if (!waitResult.Acquired)
            {
                request.Acquisition.TrySetCanceled(request.CancellationToken);
                return;
            }

            ownsMutex = true;

            lock (_sync)
            {
                _owner = request;
            }

            ownershipPublished = true;
            request.Acquisition.TrySetResult(
                new NamedLockAcquisition(waitResult.WasAbandoned));
            request.ReleaseRequested.Wait();
        }
        catch (Exception exception)
        {
            failure = exception;

            if (!ownershipPublished)
            {
                request.Acquisition.TrySetException(exception);
            }
        }
        finally
        {
            if (ownsMutex)
            {
                try
                {
                    mutex!.ReleaseMutex();
                }
                catch (Exception exception)
                {
                    failure ??= exception;
                }
            }

            try
            {
                mutex?.Dispose();
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }

            if (ownershipPublished)
            {
                ClearOwner(request);

                if (failure is null)
                {
                    request.ReleaseCompleted.TrySetResult();
                }
                else
                {
                    request.ReleaseCompleted.TrySetException(failure);
                }
            }

            request.Dispose();
        }
    }

    private static NamedLockWaitResult WaitForOwnership(
        Mutex mutex,
        CancellationToken cancellationToken)
    {
        try
        {
            int signaledHandle = WaitHandle.WaitAny(
                new WaitHandle[] { cancellationToken.WaitHandle, mutex });

            return signaledHandle == 0
                ? new NamedLockWaitResult(Acquired: false, WasAbandoned: false)
                : new NamedLockWaitResult(Acquired: true, WasAbandoned: false);
        }
        catch (AbandonedMutexException)
        {
            return new NamedLockWaitResult(Acquired: true, WasAbandoned: true);
        }
    }

    private void ClearOwner(AcquisitionRequest request)
    {
        lock (_sync)
        {
            if (ReferenceEquals(_owner, request))
            {
                _owner = null;
            }
        }
    }

    private readonly record struct NamedLockWaitResult(bool Acquired, bool WasAbandoned);

    private sealed class AcquisitionRequest : IDisposable
    {
        public AcquisitionRequest(CancellationToken cancellationToken) =>
            CancellationToken = cancellationToken;

        public CancellationToken CancellationToken { get; }

        public TaskCompletionSource<NamedLockAcquisition> Acquisition { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ManualResetEventSlim ReleaseRequested { get; } = new(initialState: false);

        public TaskCompletionSource ReleaseCompleted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Dispose() => ReleaseRequested.Dispose();
    }
}
