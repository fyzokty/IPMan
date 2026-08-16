using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public interface IRecoverySnapshotRepository
{
    Task<RecoveryCaptureResult> SaveAsync(
        RecoverySnapshot snapshot,
        CancellationToken cancellationToken);
}
