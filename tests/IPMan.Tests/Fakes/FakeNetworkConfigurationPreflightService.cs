using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeNetworkConfigurationPreflightService : INetworkConfigurationPreflightService
{
    public required NetworkConfigurationPreflightResult Result { get; set; }

    public int CallCount { get; private set; }

    public NetworkAdapterId? LastAdapterId { get; private set; }

    public Task<NetworkConfigurationPreflightResult> PreflightAsync(
        NetworkAdapterId adapterId,
        StaticIpv4Configuration desiredConfiguration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        LastAdapterId = adapterId;
        return Task.FromResult(Result);
    }
}
