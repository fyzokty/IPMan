namespace IPMan.Application.Networking;

public interface IIpv4ConflictProbe
{
    Task<Ipv4ConflictProbeResult> ProbeAsync(
        string normalizedIpv4Address,
        CancellationToken cancellationToken);
}
