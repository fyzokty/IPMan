using IPMan.Application.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeRecoveryRestoreService : IRecoveryRestoreService
{
    public RecoveryRestoreResult Result { get; set; } =
        new(RecoveryRestoreStatus.VerifiedSuccess);

    public Queue<RecoveryRestoreResult> QueuedResults { get; } = new();

    public List<RecoveryRestoreRequest> Requests { get; } = new();

    public Exception? Failure { get; set; }

    public Task<RecoveryRestoreResult>? PendingResult { get; set; }

    public TaskCompletionSource RestoreStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<RecoveryRestoreResult> RestoreLatestAsync(
        RecoveryRestoreRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request);
        RestoreStarted.TrySetResult();

        if (Failure is not null)
        {
            return Task.FromException<RecoveryRestoreResult>(Failure);
        }

        return PendingResult ?? Task.FromResult(QueuedResults.Count > 0 ? QueuedResults.Dequeue() : Result);
    }
}
