using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>Reads Windows' connectivity assessment without sending application probes.</summary>
public interface INetworkConnectivityService
{
    /// <summary>Raised when Windows reports a connectivity change.</summary>
    event EventHandler? ConnectivityChanged;

    /// <summary>Returns reported adapters; an absent adapter has unknown connectivity.</summary>
    Task<IReadOnlyDictionary<NetworkAdapterId, InternetConnectivity>> GetConnectivityAsync(
        CancellationToken cancellationToken);
}
