using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public interface IRollbackSnapshotRepository
{
    Task<RollbackCaptureResult> SaveAsync(
        NetworkRollbackSnapshot snapshot,
        CancellationToken cancellationToken);
}
