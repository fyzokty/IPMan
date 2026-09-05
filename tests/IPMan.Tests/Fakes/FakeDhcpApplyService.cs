using IPMan.Application.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeDhcpApplyService : IDhcpApplyService
{
    public DhcpApplyResult Result { get; set; } = new(DhcpApplyStatus.VerifiedSuccess);

    public Queue<DhcpApplyResult> QueuedResults { get; } = new();

    public List<DhcpApplyRequest> Requests { get; } = new();

    public Task<DhcpApplyResult> ApplyAsync(
        DhcpApplyRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request);
        return Task.FromResult(QueuedResults.Count > 0 ? QueuedResults.Dequeue() : Result);
    }
}
