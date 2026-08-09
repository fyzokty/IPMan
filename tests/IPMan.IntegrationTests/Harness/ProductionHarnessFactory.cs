using IPMan.Application.Common;
using IPMan.Application.Networking;
using IPMan.Infrastructure.Common;
using IPMan.Infrastructure.Networking;
using Microsoft.Extensions.Logging.Abstractions;

namespace IPMan.IntegrationTests.Harness;

public static class ProductionHarnessFactory
{
    public static ProductionHarnessContext Create()
    {
        IAdapterProbe probe = new SystemNetworkInterfaceProbe(
            NullLogger<SystemNetworkInterfaceProbe>.Instance);
        INetworkAdapterReader reader = new NetworkAdapterReader(
            probe,
            NullLogger<NetworkAdapterReader>.Instance);
        IStaticIpv4ConfigurationValidator validator = new StaticIpv4ConfigurationValidator();
        IStaticIpv4ConfigurationComparer comparer = new StaticIpv4ConfigurationComparer();
        IIpv4ConflictProbe conflictProbe = new PingIpv4ConflictProbe(
            new Ipv4ConflictProbeOptions { Timeout = TimeSpan.FromMilliseconds(500) });
        INetworkConfigurationPreflightService preflight = new NetworkConfigurationPreflightService(
            reader,
            validator,
            comparer,
            conflictProbe);
        IRollbackSnapshotRepository rollback = new JsonRollbackSnapshotRepository(
            new RollbackSnapshotRepositoryOptions
            {
                BackupDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "IPMan",
                    "Backup")
            });
        WmiNetworkAdapterRecoveryReader recoveryReader = new();
        RecordingNetworkAdapterRecoveryReader applyRecoveryReader = new(recoveryReader);
        INetworkAdapterConfigurator configurator = new WmiNetworkAdapterConfigurator();
        NetworkMutationCoordinator coordinator = new();
        IDelayProvider delay = new SystemDelayProvider();
        IClock clock = new SystemClock();
        IStaticIpv4ApplyService apply = new StaticIpv4ApplyService(
            preflight,
            rollback,
            configurator,
            reader,
            applyRecoveryReader,
            coordinator,
            comparer,
            delay,
            clock,
            new StaticIpv4ApplyOptions
            {
                VerificationAttempts = 4,
                VerificationDelay = TimeSpan.FromMilliseconds(500)
            });

        return new ProductionHarnessContext(
            reader,
            recoveryReader,
            applyRecoveryReader,
            apply,
            coordinator);
    }
}
