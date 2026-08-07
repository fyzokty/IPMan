using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>Reads exact-identity state required for a restore-capable rollback.</summary>
public interface INetworkAdapterRecoveryReader
{
    Task<NetworkAdapterRecoveryReadResult> ReadAsync(
        NetworkAdapterId adapterId,
        CancellationToken cancellationToken);
}
