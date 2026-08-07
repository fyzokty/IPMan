using IPMan.Application.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeIpv4ConflictProbe : IIpv4ConflictProbe
{
    private Exception? _failure;

    public Ipv4ConflictProbeResult Result { get; set; } =
        new(Ipv4ConflictProbeStatus.NoResponse);

    public int ProbeCount { get; private set; }

    public string? LastAddress { get; private set; }

    public void FailWith(Exception failure) => _failure = failure;

    public Task<Ipv4ConflictProbeResult> ProbeAsync(
        string normalizedIpv4Address,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ProbeCount++;
        LastAddress = normalizedIpv4Address;

        return _failure is null
            ? Task.FromResult(Result)
            : Task.FromException<Ipv4ConflictProbeResult>(_failure);
    }
}
