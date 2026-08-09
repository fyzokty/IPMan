namespace IPMan.Application.Networking;

public sealed record Ipv4DefaultRouteClearResult(
    Ipv4DefaultRouteClearStatus Status,
    uint? TechnicalCode = null)
{
    public bool IsSuccess => Status == Ipv4DefaultRouteClearStatus.Success;

    public static Ipv4DefaultRouteClearResult Success() =>
        new(Ipv4DefaultRouteClearStatus.Success);
}
