using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeNetworkConnectivityService : INetworkConnectivityService
{
    private EventHandler? _connectivityChanged;

    public IReadOnlyDictionary<NetworkAdapterId, InternetConnectivity> Connectivity { get; set; } =
        new Dictionary<NetworkAdapterId, InternetConnectivity>();

    public Exception? Failure { get; set; }

    public int ReadCount { get; private set; }

    public int SubscriberCount => _connectivityChanged?.GetInvocationList().Length ?? 0;

    public event EventHandler? ConnectivityChanged
    {
        add => _connectivityChanged += value;
        remove => _connectivityChanged -= value;
    }

    public Task<IReadOnlyDictionary<NetworkAdapterId, InternetConnectivity>> GetConnectivityAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadCount++;
        return Failure is null
            ? Task.FromResult(Connectivity)
            : Task.FromException<IReadOnlyDictionary<NetworkAdapterId, InternetConnectivity>>(Failure);
    }

    public void RaiseConnectivityChanged() => _connectivityChanged?.Invoke(this, EventArgs.Empty);
}
