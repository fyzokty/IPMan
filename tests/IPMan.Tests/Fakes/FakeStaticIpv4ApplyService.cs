using IPMan.Application.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeStaticIpv4ApplyService : IStaticIpv4ApplyService
{
    public StaticIpv4ApplyResult Result { get; set; } =
        new(StaticIpv4ApplyStatus.VerifiedSuccess);

    public Queue<StaticIpv4ApplyResult> QueuedResults { get; } = new();

    public List<StaticIpv4ApplyRequest> Requests { get; } = new();

    public Exception? Failure { get; set; }

    public Task<StaticIpv4ApplyResult> ApplyAsync(
        StaticIpv4ApplyRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request);

        if (Failure is not null)
        {
            return Task.FromException<StaticIpv4ApplyResult>(Failure);
        }

        return Task.FromResult(QueuedResults.Count > 0 ? QueuedResults.Dequeue() : Result);
    }
}
