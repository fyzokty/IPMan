using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public interface INetworkAdapterConfigurator
{
    Task<NetworkApplyResult> ApplyStaticAsync(
        NetworkAdapterId adapterId,
        StaticIpv4Configuration configuration,
        CancellationToken cancellationToken);

    Task<NetworkApplyResult> EnableDhcpAsync(
        NetworkAdapterId adapterId,
        CancellationToken cancellationToken);
}
