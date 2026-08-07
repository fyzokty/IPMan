using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public interface INetworkAdapterConfigurator
{
    Task<NetworkApplyResult> ApplyStaticAsync(
        NetworkAdapterId adapterId,
        StaticIpv4MutationPlan mutationPlan,
        CancellationToken cancellationToken);
}
