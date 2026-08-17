using IPMan.Infrastructure.Common;

namespace IPMan.Tests.Fakes;

internal sealed class FakeActivationChannelClient : IActivationChannelClient
{
    private readonly FakeActivationChannelServer? _server;

    public FakeActivationChannelClient(FakeActivationChannelServer? server = null) =>
        _server = server;

    public bool SendResult { get; set; } = true;

    public int SendCount { get; private set; }

    public Action? OnSend { get; set; }

    public Task<bool> TrySendActivationAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SendCount++;
        OnSend?.Invoke();

        if (SendResult)
        {
            _server?.ReceiveActivationSignal();
        }

        return Task.FromResult(SendResult);
    }
}
