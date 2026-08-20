using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>Applies validated low-level network mutation plans.</summary>
public interface INetworkAdapterConfigurator
{
    /// <summary>Applies a static IPv4 mutation plan to the exact adapter identity.</summary>
    Task<NetworkApplyResult> ApplyStaticAsync(
        NetworkAdapterId adapterId,
        StaticIpv4MutationPlan mutationPlan,
        CancellationToken cancellationToken);

    /// <summary>Enables DHCP and applies the accompanying automatic-DNS intent.</summary>
    Task<NetworkApplyResult> ApplyDhcpAsync(
        NetworkAdapterId adapterId,
        DhcpMutationPlan mutationPlan,
        CancellationToken cancellationToken);
}
