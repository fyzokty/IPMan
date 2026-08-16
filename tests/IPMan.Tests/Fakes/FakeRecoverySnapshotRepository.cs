using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeRecoverySnapshotRepository : IRecoverySnapshotRepository
{
    public RecoveryCaptureResult Result { get; set; } = RecoveryCaptureResult.Success(
        new RecoverySnapshotReference("recovery-1", "C:\\Backup\\recovery-1.json"));

    public List<RecoverySnapshot> SavedSnapshots { get; } = new();

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
}
