using IPMan.Application.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeDhcpApplyService : IDhcpApplyService
{
    public DhcpApplyResult Result { get; set; } = new(DhcpApplyStatus.VerifiedSuccess);

    public List<DhcpApplyRequest> Requests { get; } = new();

    public Task<DhcpApplyResult> ApplyAsync(
        DhcpApplyRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request);
        return Task.FromResult(Result);
    }
}
