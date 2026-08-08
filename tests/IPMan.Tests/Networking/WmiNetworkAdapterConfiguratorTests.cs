using System.Management;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class WmiNetworkAdapterConfiguratorTests
{
    private static readonly NetworkAdapterId AdapterId = new("{A}");

    private static readonly string[] ExpectedIpAddress = { "192.168.1.60" };

    private static readonly string[] ExpectedSubnetMask = { "255.255.255.0" };

    private static readonly string[] ExpectedGateway = { "192.168.1.1" };

    private static readonly string[] ExpectedDnsServers = { "1.1.1.1", "8.8.8.8" };

    [Fact]
    public async Task ApplyStaticAsync_WhenAllStepsReturnZero_InvokesDocumentedMethodsInOrder()
    {
        FakeWmiSession session = new(0, 0, 0);
        WmiNetworkAdapterConfigurator configurator = CreateConfigurator(session);

        NetworkApplyResult result = await configurator.ApplyStaticAsync(
            AdapterId,
            Plan(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Collection(
            session.Calls,
            call =>
            {
                Assert.Equal("EnableStatic", call.Method);
                Assert.Equal(ExpectedIpAddress, call.Parameters!["IPAddress"]);
                Assert.Equal(ExpectedSubnetMask, call.Parameters["SubnetMask"]);
            },
            call =>
            {
                Assert.Equal("SetGateways", call.Method);
                Assert.Equal(ExpectedGateway, call.Parameters!["DefaultIPGateway"]);
                Assert.Equal(new ushort[] { 1 }, call.Parameters["GatewayCostMetric"]);
            },
            call =>
            {
                Assert.Equal("SetDNSServerSearchOrder", call.Method);
                Assert.Equal(ExpectedDnsServers, call.Parameters!["DNSServerSearchOrder"]);
            });
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenSuccessRequiresRestart_RetainsRestartState()
    {
        WmiNetworkAdapterConfigurator configurator = CreateConfigurator(new FakeWmiSession(1, 0, 0));

        NetworkApplyResult result = await configurator.ApplyStaticAsync(
            AdapterId,
            Plan(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.RequiresRestart);
        Assert.Equal((uint)1, result.Ipv4Step.TechnicalCode);
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenIpv4StepFails_DoesNotAttemptLaterSteps()
    {
        FakeWmiSession session = new(70);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            Plan(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsPartialFailure);
        Assert.Equal(NetworkMutationFailureKind.OperationalFailure, result.FailureKind);
        Assert.Equal((uint)70, result.Ipv4Step.TechnicalCode);
        Assert.Single(session.Calls);
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenAlreadyStaticAndEnableStaticReturns81_ContinuesProvisionally()
    {
        FakeWmiSession session = new(81, 0, 0);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            Plan(previousMode: NetworkConfigurationMode.Static),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NetworkMutationStepStatus.SucceededProvisionally, result.Ipv4Step.Status);
        Assert.Equal((uint)81, result.Ipv4Step.TechnicalCode);
        Assert.Equal(3, session.Calls.Count);
    }

    [Theory]
    [InlineData(NetworkConfigurationMode.Dhcp)]
    [InlineData(NetworkConfigurationMode.Unknown)]
    public async Task ApplyStaticAsync_WhenPriorModeIsNotStatic_DoesNotAcceptEnableStatic81(
        NetworkConfigurationMode previousMode)
    {
        FakeWmiSession session = new(81);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            Plan(previousMode: previousMode),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NetworkMutationStepStatus.Failed, result.Ipv4Step.Status);
        Assert.Equal((uint)81, result.Ipv4Step.TechnicalCode);
        Assert.Single(session.Calls);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ApplyStaticAsync_WhenLaterMethodReturns81_TreatsItAsFailure(int failingCall)
    {
        FakeWmiSession session = failingCall == 1
            ? new FakeWmiSession(0, 81)
            : new FakeWmiSession(0, 0, 81);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            Plan(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        NetworkMutationStepResult failed = failingCall == 1 ? result.GatewayStep : result.DnsStep;
        Assert.Equal(NetworkMutationStepStatus.Failed, failed.Status);
        Assert.Equal((uint)81, failed.TechnicalCode);
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenGatewayStepFails_ReturnsPartialFailureWithoutDnsCall()
    {
        FakeWmiSession session = new(0, 71);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            Plan(),
            CancellationToken.None);

        Assert.True(result.IsPartialFailure);
        Assert.Equal(NetworkMutationStepStatus.Failed, result.GatewayStep.Status);
        Assert.Equal(NetworkMutationStepStatus.NotAttempted, result.DnsStep.Status);
        Assert.Equal(2, session.Calls.Count);
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenDnsStepFails_ReturnsPartialFailure()
    {
        NetworkApplyResult result = await CreateConfigurator(new FakeWmiSession(0, 0, 96))
            .ApplyStaticAsync(AdapterId, Plan(), CancellationToken.None);

        Assert.True(result.IsPartialFailure);
        Assert.Equal((uint)96, result.DnsStep.TechnicalCode);
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenDnsIsEmpty_OmitsAllDnsInputParameters()
    {
        FakeWmiSession session = new(0, 0, 0);
        StaticIpv4MutationPlan plan = Plan(
            primaryDns: null,
            secondaryDns: null,
            dnsMode: DnsMutationMode.ClearToAutomatic);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            plan,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(session.Calls[2].Parameters);
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenDnsAlreadyMatches_SkipsUnnecessaryDnsCall()
    {
        FakeWmiSession session = new(0, 0);
        StaticIpv4MutationPlan plan = Plan(dnsMode: DnsMutationMode.LeaveUnchanged);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            plan,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NetworkMutationStepStatus.NotRequired, result.DnsStep.Status);
        Assert.DoesNotContain(session.Calls, call => call.Method == "SetDNSServerSearchOrder");
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenGatewayWasAlreadyAbsent_SkipsUnnecessaryGatewayCall()
    {
        FakeWmiSession session = new(0, 0);
        StaticIpv4MutationPlan plan = Plan(
            gateway: null,
            gatewayMode: GatewayMutationMode.LeaveAbsent,
            gatewayMetric: null);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            plan,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NetworkMutationStepStatus.NotRequired, result.GatewayStep.Status);
        Assert.DoesNotContain(session.Calls, call => call.Method == "SetGateways");
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenExistingGatewayMetricIsPreserved_UsesPlannedMetric()
    {
        FakeWmiSession session = new(0, 0, 0);
        StaticIpv4MutationPlan plan = Plan(gatewayMetric: 25);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            plan,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new ushort[] { 25 }, session.Calls[1].Parameters!["GatewayCostMetric"]);
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenSetGatewayMetricIsMissing_FailsBeforeResolvingAdapter()
    {
        FakeWmiFactory factory = new(new WmiAdapterResolution(WmiAdapterResolutionStatus.Unavailable));
        StaticIpv4MutationPlan plan = Plan(gatewayMetric: null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            new WmiNetworkAdapterConfigurator(factory).ApplyStaticAsync(
                AdapterId,
                plan,
                CancellationToken.None));

        Assert.Equal(0, factory.ResolveCount);
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenGatewayMustBeCleared_UsesDocumentedHostAddressSentinel()
    {
        FakeWmiSession session = new(0, 0, 0);
        StaticIpv4MutationPlan plan = Plan(
            gateway: null,
            gatewayMode: GatewayMutationMode.Clear,
            gatewayMetric: null);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            plan,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ExpectedIpAddress, session.Calls[1].Parameters!["DefaultIPGateway"]);
    }

    [Theory]
    [InlineData(WmiAdapterResolutionStatus.Unavailable, NetworkMutationFailureKind.AdapterUnavailable)]
    [InlineData(WmiAdapterResolutionStatus.Ambiguous, NetworkMutationFailureKind.AdapterMappingAmbiguous)]
    internal async Task ApplyStaticAsync_WhenIdentityCannotResolveUniquely_DoesNotMutate(
        WmiAdapterResolutionStatus status,
        NetworkMutationFailureKind expected)
    {
        FakeWmiFactory factory = new(new WmiAdapterResolution(status));
        WmiNetworkAdapterConfigurator configurator = new(factory);

        NetworkApplyResult result = await configurator.ApplyStaticAsync(
            AdapterId,
            Plan(),
            CancellationToken.None);

        Assert.Equal(expected, result.FailureKind);
        Assert.False(result.WasMutationAttempted);
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenSessionThrowsManagementFailure_ReturnsTypedFailure()
    {
        FakeWmiSession session = new();
        session.Failure = new ManagementException("WMI unavailable");

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            Plan(),
            CancellationToken.None);

        Assert.Equal(NetworkMutationFailureKind.ManagementFailure, result.FailureKind);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenSessionHasProgrammingDefect_PropagatesException()
    {
        FakeWmiSession session = new();
        session.Failure = new InvalidOperationException("defect");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateConfigurator(session).ApplyStaticAsync(
                AdapterId,
                Plan(),
                CancellationToken.None));
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenCancelledBeforeWorker_DoesNotResolveAdapter()
    {
        FakeWmiFactory factory = new(new WmiAdapterResolution(WmiAdapterResolutionStatus.Unavailable));
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new WmiNetworkAdapterConfigurator(factory).ApplyStaticAsync(
                AdapterId,
                Plan(),
                cancellation.Token));

        Assert.Equal(0, factory.ResolveCount);
    }

    [Fact]
    public void BuildExactIdentityQuery_EscapesIdentityAndNeverUsesDisplayMetadata()
    {
        string query = SystemWmiNetworkAdapterSessionFactory.BuildExactIdentityQuery("{A}' OR 1=1");

        Assert.Contains("SettingID = '{A}'' OR 1=1'", query, StringComparison.Ordinal);
        Assert.DoesNotContain("Description", query, StringComparison.Ordinal);
        Assert.DoesNotContain("Caption", query, StringComparison.Ordinal);
    }

    private static WmiNetworkAdapterConfigurator CreateConfigurator(FakeWmiSession session) =>
        new(new FakeWmiFactory(
            new WmiAdapterResolution(WmiAdapterResolutionStatus.Found, session)));

    private static StaticIpv4MutationPlan Plan(
        string? gateway = "192.168.1.1",
        string? primaryDns = "1.1.1.1",
        string? secondaryDns = "8.8.8.8",
        GatewayMutationMode gatewayMode = GatewayMutationMode.Set,
        ushort? gatewayMetric = 1,
        DnsMutationMode dnsMode = DnsMutationMode.Set,
        NetworkConfigurationMode previousMode = NetworkConfigurationMode.Dhcp) =>
        new(
            new StaticIpv4Configuration(
                "192.168.1.60",
                "255.255.255.0",
                gateway,
                primaryDns,
                secondaryDns),
            gatewayMode,
            gatewayMetric,
            dnsMode,
            previousMode);

    private sealed class FakeWmiFactory : IWmiNetworkAdapterSessionFactory
    {
        private readonly WmiAdapterResolution _resolution;

        public FakeWmiFactory(WmiAdapterResolution resolution) => _resolution = resolution;

        public int ResolveCount { get; private set; }

        public WmiAdapterResolution ResolveBySettingId(string adapterId)
        {
            ResolveCount++;
            Assert.Equal(AdapterId.Value, adapterId);
            return _resolution;
        }
    }

    private sealed class FakeWmiSession : IWmiNetworkAdapterSession
    {
        private readonly Queue<uint> _codes;

        public FakeWmiSession(params uint[] codes) => _codes = new Queue<uint>(codes);

        public List<WmiCall> Calls { get; } = new();

        public Exception? Failure { get; set; }

        public uint Invoke(string methodName, IReadOnlyDictionary<string, object?>? parameters)
        {
            Calls.Add(new WmiCall(methodName, parameters));

            if (Failure is not null)
            {
                throw Failure;
            }

            return _codes.Dequeue();
        }

        public object? ReadProperty(string propertyName) => null;

        public void Dispose()
        {
        }
    }

    private sealed record WmiCall(
        string Method,
        IReadOnlyDictionary<string, object?>? Parameters);
}
