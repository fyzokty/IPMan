using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public sealed record RollbackCaptureResult(
    RollbackSnapshotReference? Reference,
    RollbackCaptureFailure Failure)
{
    public bool IsSuccess => Reference is not null && Failure == RollbackCaptureFailure.None;

    public static RollbackCaptureResult Success(RollbackSnapshotReference reference) =>
        new(reference, RollbackCaptureFailure.None);

    public static RollbackCaptureResult Failed(RollbackCaptureFailure failure) => new(null, failure);
}
