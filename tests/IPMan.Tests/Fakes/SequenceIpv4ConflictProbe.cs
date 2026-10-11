using IPMan.Application.Networking;

namespace IPMan.Tests.Fakes;

public sealed class SequenceIpv4ConflictProbe : IIpv4ConflictProbe
{
    public Queue<Ipv4ConflictProbeStatus> Results { get; } = new();
    public List<string> Addresses { get; } = [];
    public Func<string, CancellationToken, Task<Ipv4ConflictProbeResult>>? Handler { get; set; }

    public Task<Ipv4ConflictProbeResult> ProbeAsync(string normalizedIpv4Address, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Addresses.Add(normalizedIpv4Address);
        return Handler?.Invoke(normalizedIpv4Address, cancellationToken) ??
            Task.FromResult(new Ipv4ConflictProbeResult(Results.Count == 0
                ? Ipv4ConflictProbeStatus.NoResponse : Results.Dequeue()));
    }
}
