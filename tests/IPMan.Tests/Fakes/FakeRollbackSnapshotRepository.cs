using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeRollbackSnapshotRepository : IRollbackSnapshotRepository
{
    public RollbackCaptureResult Result { get; set; } = RollbackCaptureResult.Success(
        new RollbackSnapshotReference("rollback-1", "C:\\Backup\\rollback-1.json"));

    public List<NetworkRollbackSnapshot> SavedSnapshots { get; } = new();

    public Action? OnSave { get; set; }

    public Task<RollbackCaptureResult> SaveAsync(
        NetworkRollbackSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SavedSnapshots.Add(snapshot);
        OnSave?.Invoke();
        return Task.FromResult(Result);
    }
}
