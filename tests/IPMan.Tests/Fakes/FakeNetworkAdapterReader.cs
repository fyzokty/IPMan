using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeNetworkAdapterReader : INetworkAdapterReader
{
    private readonly Queue<IReadOnlyList<NetworkAdapterSnapshot>> _results = new();
    private readonly object _sync = new();

    private IReadOnlyList<NetworkAdapterSnapshot> _lastResult = Array.Empty<NetworkAdapterSnapshot>();
    private Exception? _failure;

    public int ReadCount { get; private set; }

    public void EnqueueResult(params NetworkAdapterSnapshot[] adapters)
    {
        lock (_sync)
        {
            _results.Enqueue(adapters);
        }
    }

    public void FailWith(Exception failure)
    {
        lock (_sync)
        {
            _failure = failure;
        }
    }

    public void StopFailing()
    {
        lock (_sync)
        {
            _failure = null;
        }
    }

    public Task<IReadOnlyList<NetworkAdapterSnapshot>> GetAdaptersAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            ReadCount++;

            if (_failure is not null)
            {
                return Task.FromException<IReadOnlyList<NetworkAdapterSnapshot>>(_failure);
            }

            if (_results.Count > 0)
            {
                _lastResult = _results.Dequeue();
            }

            return Task.FromResult(_lastResult);
        }
    }

    public async Task<NetworkAdapterSnapshot?> GetAdapterAsync(
        NetworkAdapterId adapterId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<NetworkAdapterSnapshot> adapters =
            await GetAdaptersAsync(cancellationToken).ConfigureAwait(false);

        return adapters.FirstOrDefault(adapter => adapter.Id == adapterId);
    }
}
