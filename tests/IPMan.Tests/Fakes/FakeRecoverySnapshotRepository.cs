using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeRecoverySnapshotRepository : IRecoverySnapshotRepository
{
    public RecoveryCaptureResult Result { get; set; } = RecoveryCaptureResult.Success(
        new RecoverySnapshotReference("recovery-1", "C:\\Backup\\recovery-1.json"));

    public List<RecoverySnapshot> SavedSnapshots { get; } = new();

    public RecoverySnapshotLoadResult LoadResult { get; set; } =
        RecoverySnapshotLoadResult.Failed(RecoverySnapshotLoadStatus.NotFound);

    public int LoadCount { get; private set; }

    public NetworkAdapterId? LastLoadedAdapterId { get; private set; }

    public Action? OnSave { get; set; }

    public Task<RecoveryCaptureResult> SaveAsync(
        RecoverySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SavedSnapshots.Add(snapshot);
        OnSave?.Invoke();
        return Task.FromResult(Result);
    }

    public Task<RecoverySnapshotLoadResult> LoadLatestAsync(
        NetworkAdapterId adapterId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LoadCount++;
        LastLoadedAdapterId = adapterId;
        return Task.FromResult(LoadResult);
    }
}
