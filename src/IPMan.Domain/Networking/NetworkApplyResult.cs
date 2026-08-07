namespace IPMan.Domain.Networking;

public sealed record NetworkApplyResult(
    bool IsSuccess,
    bool RequiresRestart,
    string? TechnicalCode,
    string? TechnicalMessage)
{
    public static NetworkApplyResult Success(bool requiresRestart = false) =>
        new(true, requiresRestart, null, null);

    public static NetworkApplyResult Failure(
        string? technicalCode,
        string? technicalMessage) =>
        new(false, false, technicalCode, technicalMessage);
}
