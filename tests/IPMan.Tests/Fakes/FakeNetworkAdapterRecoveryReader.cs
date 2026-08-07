using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeNetworkAdapterRecoveryReader : INetworkAdapterRecoveryReader
{
    private readonly Queue<NetworkAdapterRecoveryReadResult> _results = new();
    private NetworkAdapterRecoveryReadResult? _lastResult;

    public int ReadCount { get; private set; }

    public NetworkAdapterId? LastAdapterId { get; private set; }

    public void Enqueue(NetworkAdapterRecoveryReadResult result) => _results.Enqueue(result);

    public void Enqueue(NetworkAdapterRecoverySnapshot snapshot) =>
        Enqueue(NetworkAdapterRecoveryReadResult.Success(snapshot));

    public Task<NetworkAdapterRecoveryReadResult> ReadAsync(
        NetworkAdapterId adapterId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadCount++;
        LastAdapterId = adapterId;

        if (_results.Count > 0)
        {
            _lastResult = _results.Dequeue();
        }

        return Task.FromResult(
            _lastResult ?? new NetworkAdapterRecoveryReadResult(
                NetworkAdapterRecoveryReadStatus.AdapterUnavailable));
    }
}
