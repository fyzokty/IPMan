using System.Net.NetworkInformation;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class StaticIpv4ApplyServiceTests
{
    private static readonly NetworkAdapterId AdapterId = new("{A}");

    private static readonly string[] MultipleGateways = { "192.168.1.1", "10.0.0.1" };

    private static readonly string[] ThreeDnsServers = { "1.1.1.1", "8.8.8.8", "9.9.9.9" };

    private static readonly string[] RollbackGateways = { "192.168.1.1" };

    private static readonly string[] RollbackDnsServers = { "1.1.1.1", "8.8.8.8" };

    private static readonly string[] DriftGateway = { "192.168.1.254" };

    private static readonly string[] DriftDns = { "9.9.9.9" };

    private static readonly string[] MatchingManualDns = { "1.1.1.1", "8.8.8.8" };

    private static readonly string[] SingleManualDns = { "1.1.1.1" };

    [Theory]
    [InlineData(NetworkConfigurationPreflightStatus.ValidationFailed, StaticIpv4ApplyStatus.ValidationFailed)]
    [InlineData(NetworkConfigurationPreflightStatus.AdapterUnavailable, StaticIpv4ApplyStatus.AdapterUnavailable)]
    [InlineData(NetworkConfigurationPreflightStatus.AdapterReadFailed, StaticIpv4ApplyStatus.AdapterReadFailed)]
    [InlineData(NetworkConfigurationPreflightStatus.Cancelled, StaticIpv4ApplyStatus.Cancelled)]
    public async Task ApplyAsync_WhenPreflightStops_ReturnsTypedResultWithoutSideEffects(
        NetworkConfigurationPreflightStatus preflightStatus,
        StaticIpv4ApplyStatus expected)
    {
        using ApplyContext context = CreateContext(preflightStatus: preflightStatus);

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(expected, result.Status);
        Assert.Empty(context.Rollback.SavedSnapshots);
        Assert.Equal(0, context.Configurator.ApplyCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenNoChange_DoesNotCaptureRollbackOrMutate()
    {
        NetworkAdapterSnapshot current = Verified();
        using ApplyContext context = CreateContext(
            current,
            preflightStatus: NetworkConfigurationPreflightStatus.NoChange,
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(Recovery(current)));

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.NoChange, result.Status);
        Assert.Empty(context.Rollback.SavedSnapshots);
        Assert.Equal(0, context.Configurator.ApplyCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenEffectiveDnsMatchesButSourceDoesNot_StillMutatesDnsSource()
    {
        StaticIpv4Configuration desired = new(
            "192.168.1.60",
            "255.255.255.0",
            "192.168.1.1",
            "1.1.1.1",
            null);
        NetworkAdapterSnapshot current = TestData.Snapshot(
            id: AdapterId.Value,
            mode: NetworkConfigurationMode.Static,
            ipv4Address: desired.Ipv4Address,
            subnetMask: desired.SubnetMask,
            gateway: desired.Gateway,
            primaryDns: desired.PrimaryDns,
            ipv4DnsServers: new Ipv4AddressValueCollection(SingleManualDns));
        using ApplyContext context = CreateContext(
            current,
            desired,
            preflightStatus: NetworkConfigurationPreflightStatus.NoChange,
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(
                Recovery(current, DnsConfigurationMode.Automatic)),
            verificationSnapshots: new[] { current });

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(
            Request(desired),
            CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        Assert.Equal(DnsMutationMode.Set, Assert.Single(context.Configurator.Plans).DnsMode);
    }

    [Fact]
    public async Task ApplyAsync_WhenEffectiveValuesMatchButModeIsDhcp_StillMutatesToStatic()
    {
        StaticIpv4Configuration desired = Desired();
        NetworkAdapterSnapshot current = TestData.Snapshot(
            id: AdapterId.Value,
            mode: NetworkConfigurationMode.Dhcp,
            ipv4Address: desired.Ipv4Address,
            subnetMask: desired.SubnetMask,
            gateway: desired.Gateway,
            primaryDns: null,
            secondaryDns: null,
            ipv4DnsServers: Ipv4AddressValueCollection.Empty);
        using ApplyContext context = CreateContext(
            current,
            desired,
            preflightStatus: NetworkConfigurationPreflightStatus.NoChange,
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(Recovery(current)));

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(
            Request(desired),
            CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        Assert.Equal(NetworkConfigurationMode.Dhcp, Assert.Single(context.Configurator.Plans).PreviousMode);
    }

    [Fact]
    public async Task ApplyAsync_WhenPreflightFindsMultipleIpv4_BlocksMutation()
    {
        Ipv4AddressCollection addresses = new(new[]
        {
            new Ipv4AddressAssignment("192.168.1.50", "255.255.255.0"),
            new Ipv4AddressAssignment("10.0.0.2", "255.255.255.0")
        });
        NetworkAdapterSnapshot current = Current(ipv4Addresses: addresses);
        using ApplyContext context = CreateContext(
            current,
            preflightStatus: NetworkConfigurationPreflightStatus.MultipleIpv4RequiresSafetyDecision);

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.SafetyBlocked, result.Status);
        Assert.Equal(StaticIpv4SafetyBlock.MultipleIpv4Addresses, result.SafetyBlock);
        Assert.Equal(0, context.Configurator.ApplyCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenMultipleGatewaysExist_BlocksBeforeRollback()
    {
        NetworkAdapterSnapshot current = Current(
            gateways: new Ipv4AddressValueCollection(MultipleGateways));
        using ApplyContext context = CreateContext(current);

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.SafetyBlocked, result.Status);
        Assert.Equal(StaticIpv4SafetyBlock.MultipleIpv4Gateways, result.SafetyBlock);
        Assert.Empty(context.Rollback.SavedSnapshots);
    }

    [Fact]
    public async Task ApplyAsync_WhenMoreThanTwoDnsServersExist_BlocksBeforeRollback()
    {
        NetworkAdapterSnapshot current = Current(
            dnsServers: new Ipv4AddressValueCollection(ThreeDnsServers));
        using ApplyContext context = CreateContext(current);

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.SafetyBlocked, result.Status);
        Assert.Equal(StaticIpv4SafetyBlock.TooManyIpv4DnsServers, result.SafetyBlock);
        Assert.Equal(0, context.Configurator.ApplyCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenAdapterDisappearsOnPreMutationRefresh_StopsWithoutRollback()
    {
        using ApplyContext context = CreateContext(
            recoveryResult: new NetworkAdapterRecoveryReadResult(
                NetworkAdapterRecoveryReadStatus.AdapterUnavailable));

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.AdapterUnavailable, result.Status);
        Assert.Empty(context.Rollback.SavedSnapshots);
        Assert.Equal(0, context.Configurator.ApplyCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenPreMutationRefreshIsNowEquivalent_ReturnsNoChange()
    {
        NetworkAdapterSnapshot nowEquivalent = Verified();
        using ApplyContext context = CreateContext(
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(Recovery(nowEquivalent)));

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.NoChange, result.Status);
        Assert.Empty(context.Rollback.SavedSnapshots);
        Assert.Equal(0, context.Configurator.ApplyCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenSecondIpv4AppearsOnPreMutationRefresh_Blocks()
    {
        NetworkAdapterSnapshot drifted = Current(
            ipv4Addresses: new Ipv4AddressCollection(new[]
            {
                new Ipv4AddressAssignment("192.168.1.50", "255.255.255.0"),
                new Ipv4AddressAssignment("10.0.0.2", "255.255.255.0")
            }));
        using ApplyContext context = CreateContext(
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(Recovery(drifted)));

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.SafetyBlocked, result.Status);
        Assert.Equal(StaticIpv4SafetyBlock.MultipleIpv4Addresses, result.SafetyBlock);
        Assert.Empty(context.Rollback.SavedSnapshots);
    }

    [Fact]
    public async Task ApplyAsync_WhenMultipleGatewaysAppearOnPreMutationRefresh_Blocks()
    {
        NetworkAdapterSnapshot drifted = Current(
            gateways: new Ipv4AddressValueCollection(MultipleGateways));
        using ApplyContext context = CreateContext(
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(Recovery(drifted)));

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.SafetyBlocked, result.Status);
        Assert.Equal(StaticIpv4SafetyBlock.MultipleIpv4Gateways, result.SafetyBlock);
        Assert.Empty(context.Rollback.SavedSnapshots);
    }

    [Fact]
    public async Task ApplyAsync_WhenThirdDnsAppearsOnPreMutationRefresh_Blocks()
    {
        NetworkAdapterSnapshot drifted = Current(
            dnsServers: new Ipv4AddressValueCollection(ThreeDnsServers));
        using ApplyContext context = CreateContext(
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(Recovery(drifted)));

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.SafetyBlocked, result.Status);
        Assert.Equal(StaticIpv4SafetyBlock.TooManyIpv4DnsServers, result.SafetyBlock);
        Assert.Empty(context.Rollback.SavedSnapshots);
    }

    [Fact]
    public async Task ApplyAsync_WhenConflictIsNotConfirmed_RequiresConfirmation()
    {
        using ApplyContext context = CreateContext(
            preflightStatus: NetworkConfigurationPreflightStatus.PotentialAddressConflict);

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.ConflictConfirmationRequired, result.Status);
        Assert.Equal(0, context.Configurator.ApplyCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenConflictIsConfirmed_ContinuesThroughVerifiedMutation()
    {
        using ApplyContext context = CreateContext(
            preflightStatus: NetworkConfigurationPreflightStatus.PotentialAddressConflict);

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(
            Request(confirmConflict: true),
            CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        Assert.Equal(1, context.Configurator.ApplyCount);
    }

    [Theory]
    [InlineData(false, StaticIpv4ApplyStatus.ProbeIndeterminateConfirmationRequired, 0)]
    [InlineData(true, StaticIpv4ApplyStatus.VerifiedSuccess, 1)]
    public async Task ApplyAsync_WhenProbeIsIndeterminate_RequiresExplicitContinuation(
        bool continueAfterProbe,
        StaticIpv4ApplyStatus expected,
        int expectedMutationCount)
    {
        using ApplyContext context = CreateContext(
            preflightStatus: NetworkConfigurationPreflightStatus.ProbeIndeterminate);

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(
            Request(continueAfterProbe: continueAfterProbe),
            CancellationToken.None);

        Assert.Equal(expected, result.Status);
        Assert.Equal(expectedMutationCount, context.Configurator.ApplyCount);
    }

    [Fact]
    public async Task ApplyAsync_CapturesCompleteRollbackBeforeFirstMutation()
    {
        int sequence = 0;
        int rollbackSequence = 0;
        int mutationSequence = 0;
        NetworkAdapterSnapshot current = Current(
            dnsServers: new Ipv4AddressValueCollection(RollbackDnsServers));
        using ApplyContext context = CreateContext(current);
        context.Rollback.OnSave = () => rollbackSequence = Interlocked.Increment(ref sequence);
        context.Configurator.Handler = (_, _) =>
        {
            mutationSequence = Interlocked.Increment(ref sequence);
            return Task.FromResult(FakeNetworkAdapterConfigurator.SuccessfulResult());
        };

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        Assert.Equal(1, rollbackSequence);
        Assert.Equal(2, mutationSequence);
        NetworkRollbackSnapshot saved = Assert.Single(context.Rollback.SavedSnapshots);
        Assert.Equal(2, saved.SchemaVersion);
        Assert.Equal(AdapterId, saved.AdapterId);
        Assert.Equal(new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero), saved.CapturedAtUtc);
        Ipv4GatewayRecoveryState gateway = Assert.Single(saved.Ipv4Gateways);
        Assert.Equal(RollbackGateways[0], gateway.Address);
        Assert.Equal((ushort)25, gateway.Metric);
        Assert.Equal(DnsConfigurationMode.Automatic, saved.DnsMode);
        Assert.Equal(RollbackDnsServers, saved.Ipv4DnsServers);
        Assert.Single(saved.Ipv4Addresses);
        Assert.NotNull(result.Rollback);
    }

    [Fact]
    public async Task ApplyAsync_UsesSecondFreshStateForRollbackAndMutationPlan()
    {
        NetworkAdapterSnapshot drifted = TestData.Snapshot(
            id: AdapterId.Value,
            mode: NetworkConfigurationMode.Static,
            ipv4Address: "192.168.1.55",
            subnetMask: "255.255.255.0",
            gateway: "192.168.1.254",
            primaryDns: "9.9.9.9",
            secondaryDns: null,
            ipv4Gateways: new Ipv4AddressValueCollection(DriftGateway),
            ipv4DnsServers: new Ipv4AddressValueCollection(DriftDns));
        NetworkAdapterRecoverySnapshot recovery = Recovery(
            drifted,
            DnsConfigurationMode.Manual,
            gatewayMetric: 42);
        using ApplyContext context = CreateContext(
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(recovery));

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        NetworkRollbackSnapshot saved = Assert.Single(context.Rollback.SavedSnapshots);
        Assert.Equal("192.168.1.55", Assert.Single(saved.Ipv4Addresses).Address);
        Ipv4GatewayRecoveryState gateway = Assert.Single(saved.Ipv4Gateways);
        Assert.Equal("192.168.1.254", gateway.Address);
        Assert.Equal((ushort)42, gateway.Metric);
        Assert.Equal(DnsConfigurationMode.Manual, saved.DnsMode);
        Assert.Equal(DriftDns, saved.ConfiguredIpv4DnsServers);
        Assert.Equal(DriftDns, saved.Ipv4DnsServers);
        Assert.Equal(NetworkConfigurationMode.Static, Assert.Single(context.Configurator.Plans).PreviousMode);
    }

    [Theory]
    [InlineData(DnsConfigurationMode.Unknown)]
    public async Task ApplyAsync_WhenRecoverySemanticsAreIncomplete_DoesNotMutate(
        DnsConfigurationMode dnsMode)
    {
        NetworkAdapterRecoverySnapshot incomplete = Recovery(Current(), dnsMode);
        using ApplyContext context = CreateContext(
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(incomplete));

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.RecoveryStateUnavailable, result.Status);
        Assert.Empty(context.Rollback.SavedSnapshots);
        Assert.Equal(0, context.Configurator.ApplyCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenRollbackPersistenceFails_DoesNotMutate()
    {
        using ApplyContext context = CreateContext();
        context.Rollback.Result = RollbackCaptureResult.Failed(RollbackCaptureFailure.IoFailure);

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.RollbackCaptureFailed, result.Status);
        Assert.Equal(0, context.Configurator.ApplyCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenExistingGatewayMustBeCleared_UsesDocumentedClearPlan()
    {
        StaticIpv4Configuration desired = Desired(gateway: null);
        NetworkAdapterSnapshot verified = Verified(
            gateway: null,
            gateways: Ipv4AddressValueCollection.Empty);
        using ApplyContext context = CreateContext(
            desired: desired,
            verificationSnapshots: new[] { verified });

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(
            Request(desired: desired),
            CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        StaticIpv4MutationPlan plan = Assert.Single(context.Configurator.Plans);
        Assert.Equal(GatewayMutationMode.Clear, plan.GatewayMode);
        Assert.Null(plan.GatewayMetric);
    }

    [Fact]
    public async Task ApplyAsync_WhenOnlyStaticIpv4Changes_PreservesExistingGatewayMetric()
    {
        NetworkAdapterSnapshot current = TestData.Snapshot(
            id: AdapterId.Value,
            mode: NetworkConfigurationMode.Static,
            ipv4Address: "192.168.1.50",
            subnetMask: "255.255.255.0",
            gateway: "192.168.1.1");
        using ApplyContext context = CreateContext(
            current,
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(
                Recovery(current, gatewayMetric: 25)));

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(
            Request(),
            CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        Assert.Equal((ushort)25, Assert.Single(context.Configurator.Plans).GatewayMetric);
    }

    [Fact]
    public async Task ApplyAsync_WhenOnlyDnsChanges_PreservesExistingGatewayMetric()
    {
        StaticIpv4Configuration desired = new(
            "192.168.1.60",
            "255.255.255.0",
            "192.168.1.1",
            "1.1.1.1",
            null);
        NetworkAdapterSnapshot current = TestData.Snapshot(
            id: AdapterId.Value,
            mode: NetworkConfigurationMode.Static,
            ipv4Address: desired.Ipv4Address,
            subnetMask: desired.SubnetMask,
            gateway: desired.Gateway,
            ipv4DnsServers: Ipv4AddressValueCollection.Empty);
        using ApplyContext context = CreateContext(
            current,
            desired,
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(
                Recovery(current, gatewayMetric: 25)));

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(
            Request(desired),
            CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        StaticIpv4MutationPlan plan = Assert.Single(context.Configurator.Plans);
        Assert.Equal((ushort)25, plan.GatewayMetric);
        Assert.Equal(DnsMutationMode.Set, plan.DnsMode);
    }

    [Fact]
    public async Task ApplyAsync_WhenGatewayChanges_UsesApprovedDefaultMetric()
    {
        StaticIpv4Configuration desired = Desired(gateway: "192.168.1.254");
        NetworkAdapterSnapshot current = TestData.Snapshot(
            id: AdapterId.Value,
            mode: NetworkConfigurationMode.Static,
            ipv4Address: "192.168.1.50",
            subnetMask: "255.255.255.0",
            gateway: "192.168.1.1");
        NetworkAdapterSnapshot verified = Verified(
            gateway: desired.Gateway,
            gateways: new Ipv4AddressValueCollection(new[] { desired.Gateway! }));
        using ApplyContext context = CreateContext(
            current,
            desired,
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(Recovery(current)),
            verificationSnapshots: new[] { verified });

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(
            Request(desired),
            CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        Assert.Equal((ushort)1, Assert.Single(context.Configurator.Plans).GatewayMetric);
    }

    [Fact]
    public async Task ApplyAsync_WhenUnchangedGatewayMetricIsUnavailable_FailsClosed()
    {
        NetworkAdapterSnapshot current = TestData.Snapshot(
            id: AdapterId.Value,
            mode: NetworkConfigurationMode.Static,
            ipv4Address: "192.168.1.50",
            subnetMask: "255.255.255.0",
            gateway: "192.168.1.1");
        NetworkAdapterRecoverySnapshot recovery = new(
            current,
            DnsConfigurationMode.Automatic,
            Array.Empty<string>(),
            new[] { new Ipv4GatewayRecoveryState("192.168.1.1", null) });
        using ApplyContext context = CreateContext(
            current,
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(recovery));

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(
            Request(),
            CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.RecoveryStateUnavailable, result.Status);
        Assert.Empty(context.Configurator.Plans);
    }

    [Fact]
    public async Task ApplyAsync_WhenPostMutationGatewayMetricDoesNotMatchPlan_FailsVerification()
    {
        NetworkAdapterSnapshot current = TestData.Snapshot(
            id: AdapterId.Value,
            mode: NetworkConfigurationMode.Static,
            ipv4Address: "192.168.1.50",
            subnetMask: "255.255.255.0",
            gateway: "192.168.1.1");
        using ApplyContext context = CreateContext(
            current,
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(
                Recovery(current, gatewayMetric: 25)),
            verificationAttempts: 1,
            verificationGatewayMetric: 1);

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(
            Request(),
            CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerificationFailed, result.Status);
    }

    [Fact]
    public async Task ApplyAsync_WhenGatewayAlreadyAbsent_UsesLeaveAbsentPlan()
    {
        StaticIpv4Configuration desired = Desired(gateway: null);
        NetworkAdapterSnapshot current = Current(
            gateway: null,
            gateways: Ipv4AddressValueCollection.Empty);
        NetworkAdapterSnapshot verified = Verified(
            gateway: null,
            gateways: Ipv4AddressValueCollection.Empty);
        using ApplyContext context = CreateContext(current, desired, verificationSnapshots: new[] { verified });
        context.Configurator.Result = FakeNetworkAdapterConfigurator.SuccessfulResult(
            NetworkMutationStepStatus.NotRequired);

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(
            Request(desired: desired),
            CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        StaticIpv4MutationPlan plan = Assert.Single(context.Configurator.Plans);
        Assert.Equal(GatewayMutationMode.LeaveAbsent, plan.GatewayMode);
        Assert.Null(plan.GatewayMetric);
    }

    [Fact]
    public async Task ApplyAsync_WhenAutomaticDnsAlreadyMatches_LeavesDnsUnchanged()
    {
        using ApplyContext context = CreateContext();

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        Assert.Equal(DnsMutationMode.LeaveUnchanged, Assert.Single(context.Configurator.Plans).DnsMode);
    }

    [Fact]
    public async Task ApplyAsync_WhenManualDnsAlreadyMatches_LeavesDnsUnchanged()
    {
        StaticIpv4Configuration desired = new(
            "192.168.1.60",
            "255.255.255.0",
            "192.168.1.1",
            "1.1.1.1",
            "8.8.8.8");
        NetworkAdapterSnapshot current = Current(
            dnsServers: new Ipv4AddressValueCollection(MatchingManualDns));
        using ApplyContext context = CreateContext(
            current,
            desired,
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(
                Recovery(current, DnsConfigurationMode.Manual)));

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(
            Request(desired),
            CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        Assert.Equal(DnsMutationMode.LeaveUnchanged, Assert.Single(context.Configurator.Plans).DnsMode);
    }

    [Fact]
    public async Task ApplyAsync_WhenReturningDnsToAutomatic_UsesClearPlan()
    {
        NetworkAdapterSnapshot current = Current(
            dnsServers: new Ipv4AddressValueCollection(SingleManualDns));
        using ApplyContext context = CreateContext(
            current,
            recoveryResult: NetworkAdapterRecoveryReadResult.Success(
                Recovery(current, DnsConfigurationMode.Manual)));

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        Assert.Equal(DnsMutationMode.ClearToAutomatic, Assert.Single(context.Configurator.Plans).DnsMode);
    }

    [Fact]
    public async Task ApplyAsync_WhenSettingManualDns_UsesSetPlan()
    {
        StaticIpv4Configuration desired = new(
            "192.168.1.60",
            "255.255.255.0",
            "192.168.1.1",
            "1.1.1.1",
            null);
        using ApplyContext context = CreateContext(desired: desired);

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(
            Request(desired),
            CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        Assert.Equal(DnsMutationMode.Set, Assert.Single(context.Configurator.Plans).DnsMode);
    }

    [Fact]
    public async Task ApplyAsync_WhenFirstFreshReadMatches_ReturnsVerifiedSuccess()
    {
        using ApplyContext context = CreateContext();

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        Assert.NotNull(result.ActualSnapshot);
        Assert.True(result.VerificationComparison!.IsEquivalent);
        Assert.Equal(0, context.Delay.DelayCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenWindowsSettlesAfterRetry_DelaysAndThenVerifies()
    {
        NetworkAdapterSnapshot stale = Current();
        using ApplyContext context = CreateContext(
            verificationSnapshots: new[] { stale, Verified() });

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        Assert.Equal(1, context.Delay.DelayCount);
        Assert.Equal(2, context.Reader.ReadCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenVerificationNeverMatches_ReturnsFailureAfterBoundedAttempts()
    {
        NetworkAdapterSnapshot stale = Current();
        using ApplyContext context = CreateContext(
            verificationSnapshots: new[] { stale, stale, stale },
            verificationAttempts: 3);

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerificationFailed, result.Status);
        Assert.Equal(3, context.Reader.ReadCount);
        Assert.Equal(2, context.Delay.DelayCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenMutationPartiallyFails_FreshReadsAndNeverReportsSuccess()
    {
        using ApplyContext context = CreateContext();
        context.Configurator.Result = new NetworkApplyResult(
            new NetworkMutationStepResult(NetworkMutationStepStatus.Succeeded, 0),
            new NetworkMutationStepResult(NetworkMutationStepStatus.Failed, 71),
            NetworkMutationStepResult.NotAttempted(),
            NetworkMutationFailureKind.OperationalFailure);

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.PartialFailure, result.Status);
        Assert.NotNull(result.Rollback);
        Assert.NotNull(result.ActualSnapshot);
        Assert.Equal(1, context.Reader.ReadCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenAdapterDisappearsDuringVerification_ReturnsUnavailable()
    {
        using ApplyContext context = CreateContext(verificationSnapshots: Array.Empty<NetworkAdapterSnapshot>());

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.AdapterUnavailableDuringVerification, result.Status);
        Assert.NotNull(result.Rollback);
    }

    [Fact]
    public async Task ApplyAsync_WhenVerificationReadHasOperationalFailure_ReturnsVerificationFailure()
    {
        using ApplyContext context = CreateContext();
        context.Reader.FailWith(new NetworkInformationException());

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(StaticIpv4ApplyStatus.VerificationFailed, result.Status);
        Assert.NotNull(result.Rollback);
    }

    [Fact]
    public async Task ApplyAsync_WhenCancelledAfterRollbackButBeforeMutation_StopsSafely()
    {
        using CancellationTokenSource cancellation = new();
        using ApplyContext context = CreateContext();
        context.Rollback.OnSave = cancellation.Cancel;

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), cancellation.Token);

        Assert.Equal(StaticIpv4ApplyStatus.Cancelled, result.Status);
        Assert.Single(context.Rollback.SavedSnapshots);
        Assert.Equal(0, context.Configurator.ApplyCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenCancelledAfterMutationBegins_StillFreshReadsActualState()
    {
        using CancellationTokenSource cancellation = new();
        using ApplyContext context = CreateContext();
        context.Configurator.Handler = (_, _) =>
        {
            cancellation.Cancel();
            return Task.FromResult(FakeNetworkAdapterConfigurator.SuccessfulResult());
        };

        StaticIpv4ApplyResult result = await context.Service.ApplyAsync(Request(), cancellation.Token);

        Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status);
        Assert.Equal(1, context.Reader.ReadCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenTwoServiceInstancesShareCoordinator_SerializesAllMutation()
    {
        using ApplyContext context = CreateContext(
            verificationSnapshots: new[] { Verified(), Verified() },
            serializedApplyCount: 2);
        StaticIpv4ApplyService secondService = new(
            context.Preflight,
            context.Rollback,
            context.Configurator,
            context.Reader,
            context.RecoveryReader,
            context.Coordinator,
            new StaticIpv4ConfigurationComparer(),
            context.Delay,
            new FakeClock(new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero)),
            new StaticIpv4ApplyOptions());
        TaskCompletionSource firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int invocation = 0;
        context.Configurator.Handler = async (_, _) =>
        {
            if (Interlocked.Increment(ref invocation) == 1)
            {
                firstEntered.SetResult();
                await releaseFirst.Task.ConfigureAwait(false);
            }

            return FakeNetworkAdapterConfigurator.SuccessfulResult();
        };

        Task<StaticIpv4ApplyResult> first = context.Service.ApplyAsync(Request(), CancellationToken.None);
        await firstEntered.Task;
        Task<StaticIpv4ApplyResult> second = secondService.ApplyAsync(Request(), CancellationToken.None);
        releaseFirst.SetResult();
        StaticIpv4ApplyResult[] results = await Task.WhenAll(first, second);

        Assert.All(results, result => Assert.Equal(StaticIpv4ApplyStatus.VerifiedSuccess, result.Status));
        Assert.Equal(1, context.Configurator.MaximumConcurrentCalls);
        Assert.Equal(2, context.Configurator.ApplyCount);
    }

    [Fact]
    public async Task ApplyAsync_AlwaysPassesExactRequestAdapterIdentityToPreflight()
    {
        using ApplyContext context = CreateContext();

        await context.Service.ApplyAsync(Request(), CancellationToken.None);

        Assert.Equal(AdapterId, context.Preflight.LastAdapterId);
        Assert.Equal(AdapterId, context.RecoveryReader.LastAdapterId);
    }

    private static ApplyContext CreateContext(
        NetworkAdapterSnapshot? current = null,
        StaticIpv4Configuration? desired = null,
        NetworkConfigurationPreflightStatus preflightStatus = NetworkConfigurationPreflightStatus.Ready,
        NetworkAdapterRecoveryReadResult? recoveryResult = null,
        IReadOnlyList<NetworkAdapterSnapshot>? verificationSnapshots = null,
        int verificationAttempts = 4,
        int serializedApplyCount = 1,
        ushort? verificationGatewayMetric = null)
    {
        desired ??= Desired();
        current ??= Current();
        NetworkConfigurationPreflightResult preflightResult = Preflight(preflightStatus, current, desired);
        FakeNetworkConfigurationPreflightService preflight = new() { Result = preflightResult };
        FakeRollbackSnapshotRepository rollback = new();
        FakeNetworkAdapterConfigurator configurator = new();
        FakeNetworkAdapterReader reader = new();
        FakeNetworkAdapterRecoveryReader recoveryReader = new();
        NetworkAdapterRecoveryReadResult initialRecovery = recoveryResult ??
            NetworkAdapterRecoveryReadResult.Success(Recovery(current));
        IReadOnlyList<NetworkAdapterSnapshot> snapshots = verificationSnapshots ?? new[] { Verified() };

        if (serializedApplyCount == 1)
        {
            recoveryReader.Enqueue(initialRecovery);

            foreach (NetworkAdapterSnapshot snapshot in snapshots)
            {
                recoveryReader.Enqueue(RecoveryAfterMutation(
                    snapshot,
                    desired,
                    initialRecovery,
                    verificationGatewayMetric));
            }
        }
        else
        {
            Assert.Equal(serializedApplyCount, snapshots.Count);

            foreach (NetworkAdapterSnapshot snapshot in snapshots)
            {
                recoveryReader.Enqueue(initialRecovery);
                recoveryReader.Enqueue(RecoveryAfterMutation(
                    snapshot,
                    desired,
                    initialRecovery,
                    verificationGatewayMetric));
            }
        }

        foreach (NetworkAdapterSnapshot snapshot in snapshots)
        {
            reader.EnqueueResult(snapshot);
        }

        if (snapshots.Count == 0)
        {
            reader.EnqueueResult();
        }

        ImmediateDelayProvider delay = new();
        NetworkMutationCoordinator coordinator = new();
        StaticIpv4ApplyService service = new(
            preflight,
            rollback,
            configurator,
            reader,
            recoveryReader,
            coordinator,
            new StaticIpv4ConfigurationComparer(),
            delay,
            new FakeClock(new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero)),
            new StaticIpv4ApplyOptions
            {
                VerificationAttempts = verificationAttempts,
                VerificationDelay = TimeSpan.FromMilliseconds(10)
            });

        return new ApplyContext(
            service,
            preflight,
            rollback,
            configurator,
            reader,
            recoveryReader,
            coordinator,
            delay);
    }

    private static NetworkConfigurationPreflightResult Preflight(
        NetworkConfigurationPreflightStatus status,
        NetworkAdapterSnapshot current,
        StaticIpv4Configuration desired) =>
        new(
            status,
            current,
            StaticIpv4ValidationResult.Valid(desired),
            new NetworkConfigurationComparisonResult(NetworkConfigurationDifference.Ipv4Address));

    private static StaticIpv4ApplyRequest Request(
        StaticIpv4Configuration? desired = null,
        bool confirmConflict = false,
        bool continueAfterProbe = false) =>
        new(AdapterId, desired ?? Desired(), confirmConflict, continueAfterProbe);

    private static StaticIpv4Configuration Desired(string? gateway = "192.168.1.1") =>
        new("192.168.1.60", "255.255.255.0", gateway, null, null);

    private static NetworkAdapterSnapshot Current(
        string? gateway = "192.168.1.1",
        Ipv4AddressCollection? ipv4Addresses = null,
        Ipv4AddressValueCollection? gateways = null,
        Ipv4AddressValueCollection? dnsServers = null) =>
        TestData.Snapshot(
            id: AdapterId.Value,
            mode: NetworkConfigurationMode.Dhcp,
            gateway: gateway,
            primaryDns: dnsServers is { Count: > 0 } ? dnsServers[0] : null,
            secondaryDns: dnsServers is { Count: > 1 } ? dnsServers[1] : null,
            ipv4Addresses: ipv4Addresses,
            ipv4Gateways: gateways,
            ipv4DnsServers: dnsServers);

    private static NetworkAdapterSnapshot Verified(
        string? gateway = "192.168.1.1",
        Ipv4AddressValueCollection? gateways = null) =>
        TestData.Snapshot(
            id: AdapterId.Value,
            mode: NetworkConfigurationMode.Static,
            ipv4Address: "192.168.1.60",
            subnetMask: "255.255.255.0",
            gateway: gateway,
            primaryDns: null,
            secondaryDns: null,
            ipv4Gateways: gateways,
            ipv4DnsServers: Ipv4AddressValueCollection.Empty);

    private static NetworkAdapterRecoverySnapshot Recovery(
        NetworkAdapterSnapshot snapshot,
        DnsConfigurationMode dnsMode = DnsConfigurationMode.Automatic,
        ushort gatewayMetric = 25) =>
        new(
            snapshot,
            dnsMode,
            dnsMode == DnsConfigurationMode.Manual
                ? snapshot.Ipv4DnsServers.ToArray()
                : Array.Empty<string>(),
            snapshot.Ipv4Gateways
                .Select(gateway => new Ipv4GatewayRecoveryState(gateway, gatewayMetric))
                .ToArray());

    private static NetworkAdapterRecoverySnapshot RecoveryAfterMutation(
        NetworkAdapterSnapshot snapshot,
        StaticIpv4Configuration desired,
        NetworkAdapterRecoveryReadResult initialRecovery,
        ushort? verificationGatewayMetric)
    {
        string[] configuredDns = new[] { desired.PrimaryDns, desired.SecondaryDns }
            .Where(value => value is not null)
            .Select(value => value!)
            .ToArray();

        ushort? expectedGatewayMetric = desired.Gateway is null
            ? null
            : initialRecovery.Snapshot?.Ipv4Gateways.SingleOrDefault(gateway =>
                string.Equals(gateway.Address, desired.Gateway, StringComparison.Ordinal))?.Metric ?? (ushort)1;
        ushort? observedGatewayMetric = verificationGatewayMetric ?? expectedGatewayMetric;

        return new NetworkAdapterRecoverySnapshot(
            snapshot,
            configuredDns.Length == 0
                ? DnsConfigurationMode.Automatic
                : DnsConfigurationMode.Manual,
            configuredDns,
            snapshot.Ipv4Gateways
                .Select(gateway => new Ipv4GatewayRecoveryState(gateway, observedGatewayMetric))
                .ToArray());
    }

    private sealed record ApplyContext(
        StaticIpv4ApplyService Service,
        FakeNetworkConfigurationPreflightService Preflight,
        FakeRollbackSnapshotRepository Rollback,
        FakeNetworkAdapterConfigurator Configurator,
        FakeNetworkAdapterReader Reader,
        FakeNetworkAdapterRecoveryReader RecoveryReader,
        NetworkMutationCoordinator Coordinator,
        ImmediateDelayProvider Delay) : IDisposable
    {
        public void Dispose() => Coordinator.Dispose();
    }
}
