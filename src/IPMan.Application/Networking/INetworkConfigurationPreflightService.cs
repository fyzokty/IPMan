using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public interface INetworkConfigurationPreflightService
{
    Task<NetworkConfigurationPreflightResult> PreflightAsync(
        NetworkAdapterId adapterId,
        StaticIpv4Configuration desiredConfiguration,
        CancellationToken cancellationToken);
}
