using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public sealed record RecoveryCaptureResult(
    RecoverySnapshotReference? Reference,
    RecoveryCaptureFailure Failure)
{
    public bool IsSuccess => Reference is not null && Failure == RecoveryCaptureFailure.None;

    public static RecoveryCaptureResult Success(RecoverySnapshotReference reference) =>
        new(reference, RecoveryCaptureFailure.None);

    public static RecoveryCaptureResult Failed(RecoveryCaptureFailure failure) => new(null, failure);
}
