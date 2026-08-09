using IPMan.Application.Networking;
using IPMan.Infrastructure.Networking;

namespace IPMan.IntegrationTests.Harness;

public sealed class ProductionHarnessContext : IDisposable
{
    private readonly NetworkMutationCoordinator _coordinator;

    internal ProductionHarnessContext(
        INetworkAdapterReader adapterReader,
        WmiNetworkAdapterRecoveryReader recoveryReader,
        RecordingNetworkAdapterRecoveryReader applyRecoveryReader,
        IStaticIpv4ApplyService applyService,
        NetworkMutationCoordinator coordinator)
    {
        AdapterReader = adapterReader;
        RecoveryReader = recoveryReader;
        ApplyRecoveryReader = applyRecoveryReader;
        ApplyService = applyService;
        _coordinator = coordinator;
    }

    public INetworkAdapterReader AdapterReader { get; }

    public WmiNetworkAdapterRecoveryReader RecoveryReader { get; }

    public RecordingNetworkAdapterRecoveryReader ApplyRecoveryReader { get; }

    public IStaticIpv4ApplyService ApplyService { get; }

    public void Dispose() => _coordinator.Dispose();
}
