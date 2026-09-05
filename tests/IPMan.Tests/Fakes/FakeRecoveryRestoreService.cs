using IPMan.Application.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeRecoveryRestoreService : IRecoveryRestoreService
{
    public RecoveryRestoreResult Result { get; set; } =
        new(RecoveryRestoreStatus.VerifiedSuccess);

    public Queue<RecoveryRestoreResult> QueuedResults { get; } = new();

    public List<RecoveryRestoreRequest> Requests { get; } = new();

    public Task<RecoveryRestoreResult> RestoreLatestAsync(
        RecoveryRestoreRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request);
        return Task.FromResult(QueuedResults.Count > 0 ? QueuedResults.Dequeue() : Result);
    }
}
