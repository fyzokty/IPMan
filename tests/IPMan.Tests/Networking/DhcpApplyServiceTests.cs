using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class DhcpApplyServiceTests
{
    private static readonly NetworkAdapterId AdapterId = new("{A}");

    private static readonly string[] ManualDns = { "1.1.1.1" };

    [Fact]
    public async Task ApplyAsync_WhenProcessIsNotElevated_ReturnsSafetyBlockWithoutMutation()
    {
        using ApplyContext context = CreateContext(isElevated: false);

        DhcpApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(DhcpApplyStatus.SafetyBlocked, result.Status);
        Assert.Equal(StaticIpv4SafetyBlock.NotElevated, result.SafetyBlock);
        Assert.Equal(0, context.Configurator.DhcpApplyCount);
        Assert.Equal(0, context.Reader.ReadCount);
        Assert.Equal(0, context.RecoveryReader.ReadCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenAlreadyDhcpWithAutomaticDns_ReturnsNoChangeWithoutMutation()
    {
        NetworkAdapterSnapshot current = Snapshot(NetworkConfigurationMode.Dhcp);
        using ApplyContext context = CreateContext(
            current,
            currentDnsMode: DnsConfigurationMode.Automatic);

        DhcpApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(DhcpApplyStatus.NoChange, result.Status);
        Assert.Equal(0, context.Configurator.DhcpApplyCount);
        Assert.Empty(context.Recovery.SavedSnapshots);
    }

    [Fact]
    public async Task ApplyAsync_WhenDhcpHasManualDns_PerformsMutation()
    {
        NetworkAdapterSnapshot current = Snapshot(
            NetworkConfigurationMode.Dhcp,
            primaryDns: "1.1.1.1");
        using ApplyContext context = CreateContext(
            current,
            currentDnsMode: DnsConfigurationMode.Manual);

        DhcpApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(DhcpApplyStatus.VerifiedSuccess, result.Status);
        Assert.Equal(1, context.Configurator.DhcpApplyCount);
        DhcpMutationPlan plan = Assert.Single(context.Configurator.DhcpPlans);
        Assert.Equal(NetworkConfigurationMode.Dhcp, plan.PreviousMode);
        Assert.True(plan.ReturnDnsToAutomatic);
    }

    [Fact]
    public async Task ApplyAsync_WhenRecoveryPersistenceFails_ReturnsFailureWithoutMutation()
    {
        using ApplyContext context = CreateContext();
        context.Recovery.Result = RecoveryCaptureResult.Failed(RecoveryCaptureFailure.IoFailure);

        DhcpApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(DhcpApplyStatus.RecoveryCaptureFailed, result.Status);
        Assert.Equal(0, context.Configurator.DhcpApplyCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenMutationSucceeds_SavesRecoveryBeforeMutation()
    {
        int sequence = 0;
        int recoverySequence = 0;
        int mutationSequence = 0;
        using ApplyContext context = CreateContext();
        context.Recovery.OnSave = () => recoverySequence = Interlocked.Increment(ref sequence);
        context.Configurator.DhcpHandler = (_, _) =>
        {
            mutationSequence = Interlocked.Increment(ref sequence);
            return Task.FromResult(FakeNetworkAdapterConfigurator.SuccessfulDhcpResult());
        };

        DhcpApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(DhcpApplyStatus.VerifiedSuccess, result.Status);
        Assert.Equal(1, recoverySequence);
        Assert.Equal(2, mutationSequence);
        RecoverySnapshot saved = Assert.Single(context.Recovery.SavedSnapshots);
        Assert.Equal(2, saved.SchemaVersion);
        Assert.Equal(NetworkConfigurationMode.Static, saved.Mode);
        Assert.Equal(AdapterId, saved.AdapterId);
        Assert.NotNull(result.Recovery);
    }

    [Fact]
    public async Task ApplyAsync_WhenDhcpHasNoLease_VerifiesSuccessWithoutInventingAddress()
    {
        NetworkAdapterSnapshot noLease = Snapshot(
            NetworkConfigurationMode.Dhcp,
            ipv4Address: null,
            gateway: null,
            primaryDns: null);
        using ApplyContext context = CreateContext(verificationSnapshots: new[] { noLease });

        DhcpApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(DhcpApplyStatus.VerifiedSuccess, result.Status);
        Assert.NotNull(result.ActualSnapshot);
        Assert.Null(result.ActualSnapshot.Ipv4Address);
        Assert.Null(result.ActualSnapshot.SubnetMask);
        Assert.Empty(result.ActualSnapshot.Ipv4Addresses);
    }

    [Fact]
    public async Task ApplyAsync_WhenVerificationRemainsStatic_ReturnsFailureAfterBoundedAttempts()
    {
        NetworkAdapterSnapshot stillStatic = Snapshot(NetworkConfigurationMode.Static);
        using ApplyContext context = CreateContext(
            verificationSnapshots: new[] { stillStatic, stillStatic },
            verificationAttempts: 2);

        DhcpApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(DhcpApplyStatus.VerificationFailed, result.Status);
        Assert.Equal(3, context.Reader.ReadCount);
        Assert.Equal(1, context.Delay.DelayCount);
        Assert.Equal(NetworkConfigurationMode.Static, result.ActualSnapshot!.Mode);
    }

    [Fact]
    public async Task ApplyAsync_WhenCancelled_ReturnsCancelledWithoutMutation()
    {
        using ApplyContext context = CreateContext();
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        DhcpApplyResult result = await context.Service.ApplyAsync(Request(), cancellation.Token);

        Assert.Equal(DhcpApplyStatus.Cancelled, result.Status);
        Assert.Equal(0, context.Configurator.DhcpApplyCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenRecoveryIdentityChanges_ReturnsAdapterReadFailed()
    {
        NetworkAdapterSnapshot changed = TestData.Snapshot(
            id: "{B}",
            mode: NetworkConfigurationMode.Static,
            primaryDns: "1.1.1.1",
            ipv4DnsServers: new Ipv4AddressValueCollection(ManualDns));
        using ApplyContext context = CreateContext(
            initialRecovery: Recovery(changed, DnsConfigurationMode.Manual));

        DhcpApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(DhcpApplyStatus.AdapterReadFailed, result.Status);
        Assert.Equal(0, context.Configurator.DhcpApplyCount);
        Assert.Empty(context.Recovery.SavedSnapshots);
    }

    private static ApplyContext CreateContext(
        NetworkAdapterSnapshot? current = null,
        DnsConfigurationMode currentDnsMode = DnsConfigurationMode.Manual,
        NetworkAdapterRecoverySnapshot? initialRecovery = null,
        IReadOnlyList<NetworkAdapterSnapshot>? verificationSnapshots = null,
        int verificationAttempts = 4,
        bool isElevated = true)
    {
        current ??= Snapshot(NetworkConfigurationMode.Static, primaryDns: "1.1.1.1");
        NetworkAdapterSnapshot[] verified = verificationSnapshots?.ToArray() ??
            new[] { Snapshot(NetworkConfigurationMode.Dhcp) };
        FakeRecoverySnapshotRepository recovery = new();
        FakeNetworkAdapterConfigurator configurator = new();
        FakeNetworkAdapterReader reader = new();
        FakeNetworkAdapterRecoveryReader recoveryReader = new();
        ImmediateDelayProvider delay = new();
        NetworkMutationCoordinator coordinator = new();

        reader.EnqueueResult(current);
        recoveryReader.Enqueue(initialRecovery ?? Recovery(current, currentDnsMode));

        foreach (NetworkAdapterSnapshot snapshot in verified)
        {
            reader.EnqueueResult(snapshot);
            recoveryReader.Enqueue(Recovery(snapshot, DnsConfigurationMode.Automatic));
        }

        DhcpApplyService service = new(
            recovery,
            configurator,
            reader,
            recoveryReader,
            coordinator,
            delay,
            new FakeClock(new DateTimeOffset(2026, 8, 20, 10, 0, 0, TimeSpan.Zero)),
            new FakeElevationStateProvider(isElevated),
            new StaticIpv4ApplyOptions
            {
                VerificationAttempts = verificationAttempts,
                VerificationDelay = TimeSpan.FromMilliseconds(10)
            });

        return new ApplyContext(
            service,
            recovery,
            configurator,
            reader,
            recoveryReader,
            coordinator,
            delay);
    }

    private static DhcpApplyRequest Request() => new(AdapterId);

    private static NetworkAdapterSnapshot Snapshot(
        NetworkConfigurationMode mode,
        string? ipv4Address = "192.168.1.50",
        string? gateway = "192.168.1.1",
        string? primaryDns = null) =>
        TestData.Snapshot(
            id: AdapterId.Value,
            mode: mode,
            ipv4Address: ipv4Address,
            subnetMask: ipv4Address is null ? null : "255.255.255.0",
            gateway: gateway,
            primaryDns: primaryDns,
            ipv4DnsServers: primaryDns is null
                ? Ipv4AddressValueCollection.Empty
                : new Ipv4AddressValueCollection(new[] { primaryDns }));

    private static NetworkAdapterRecoverySnapshot Recovery(
        NetworkAdapterSnapshot snapshot,
        DnsConfigurationMode dnsMode) =>
        new(
            snapshot,
            dnsMode,
            dnsMode == DnsConfigurationMode.Manual
                ? snapshot.Ipv4DnsServers.ToArray()
                : Array.Empty<string>(),
            snapshot.Ipv4Gateways
                .Select(gateway => new Ipv4GatewayRecoveryState(gateway, 25))
                .ToArray());

    private sealed record ApplyContext(
        DhcpApplyService Service,
        FakeRecoverySnapshotRepository Recovery,
        FakeNetworkAdapterConfigurator Configurator,
        FakeNetworkAdapterReader Reader,
        FakeNetworkAdapterRecoveryReader RecoveryReader,
        NetworkMutationCoordinator Coordinator,
        ImmediateDelayProvider Delay) : IDisposable
    {
        public void Dispose() => Coordinator.Dispose();
    }
}
