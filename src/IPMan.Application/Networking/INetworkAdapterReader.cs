using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public interface INetworkAdapterReader
{
    Task<IReadOnlyList<NetworkAdapterSnapshot>> GetAdaptersAsync(
        CancellationToken cancellationToken);

    Task<NetworkAdapterSnapshot?> GetAdapterAsync(
        NetworkAdapterId adapterId,
        CancellationToken cancellationToken);
}
