namespace IPMan.Application.Networking;

public interface IStaticIpv4ApplyService
{
    Task<StaticIpv4ApplyResult> ApplyAsync(
        StaticIpv4ApplyRequest request,
        CancellationToken cancellationToken);
}
