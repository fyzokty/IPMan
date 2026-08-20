using IPMan.Application.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeStaticIpv4ApplyService : IStaticIpv4ApplyService
{
    public StaticIpv4ApplyResult Result { get; set; } =
        new(StaticIpv4ApplyStatus.VerifiedSuccess);

    public List<StaticIpv4ApplyRequest> Requests { get; } = new();

    public Task<StaticIpv4ApplyResult> ApplyAsync(
        StaticIpv4ApplyRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request);
        return Task.FromResult(Result);
    }
}
