using System.Management;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class WmiNetworkAdapterConfiguratorTests
{
    private static readonly NetworkAdapterId AdapterId =
        new("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}");

    private static readonly string[] ExpectedIpAddress = { "192.168.1.60" };

    private static readonly string[] ExpectedSubnetMask = { "255.255.255.0" };

    private static readonly string[] ExpectedGateway = { "192.168.1.1" };

    private static readonly string[] ExpectedDnsServers = { "1.1.1.1", "8.8.8.8" };

    private static readonly string[] ExpectedPrimaryDnsServer = { "1.1.1.1" };

    [Fact]
    public async Task ApplyStaticAsync_WhenManualDnsHasTwoServers_UsesNativeWriterAfterIpv4AndGateway()
    {
        List<string> order = new();
        FakeWmiSession session = new(0, 0) { Order = order };
        FakeManualIpv4DnsWriter dnsWriter = new() { Order = order };
        WmiNetworkAdapterConfigurator configurator = CreateConfigurator(
            session,
            dnsWriter: dnsWriter);

        NetworkApplyResult result = await configurator.ApplyStaticAsync(
            AdapterId,
            Plan(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["EnableStatic", "SetGateways", "ManualDns"], order);
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
            });
        Assert.Equal(AdapterId, dnsWriter.AdapterId);
        Assert.Equal(ExpectedDnsServers, dnsWriter.Servers);
        Assert.DoesNotContain(session.Calls, call => call.Method == "SetDNSServerSearchOrder");
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenManualDnsHasOneServer_UsesNativeWriterNotWmiDns()
    {
        FakeWmiSession session = new(0, 0);
        FakeManualIpv4DnsWriter dnsWriter = new();

        NetworkApplyResult result = await CreateConfigurator(session, dnsWriter: dnsWriter)
            .ApplyStaticAsync(
                AdapterId,
                Plan(primaryDns: "1.1.1.1", secondaryDns: null),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ExpectedPrimaryDnsServer, dnsWriter.Servers);
        Assert.DoesNotContain(session.Calls, call => call.Method == "SetDNSServerSearchOrder");
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenStaticIpv4AddressChanges_InvokesOnlyEnableStatic()
    {
        FakeWmiSession session = new(0);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            Plan(
                gatewayMode: GatewayMutationMode.LeaveUnchanged,
                dnsMode: DnsMutationMode.LeaveUnchanged,
                previousMode: NetworkConfigurationMode.Static),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        WmiCall call = Assert.Single(session.Calls);
        Assert.Equal("EnableStatic", call.Method);
        Assert.Equal(NetworkMutationStepStatus.NotRequired, result.GatewayStep.Status);
        Assert.Equal(NetworkMutationStepStatus.NotRequired, result.DnsStep.Status);
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenDhcpTransitionsToStatic_InvokesEnableStatic()
    {
        FakeWmiSession session = new(0);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            Plan(
                gatewayMode: GatewayMutationMode.LeaveUnchanged,
                dnsMode: DnsMutationMode.LeaveUnchanged,
                previousMode: NetworkConfigurationMode.Dhcp),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("EnableStatic", Assert.Single(session.Calls).Method);
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenOnlyGatewayIsSet_DoesNotInvokeEnableStatic()
    {
        FakeWmiSession session = new(0);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            Plan(
                dnsMode: DnsMutationMode.LeaveUnchanged,
                previousMode: NetworkConfigurationMode.Static,
                applyIpv4Address: false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NetworkMutationStepStatus.NotRequired, result.Ipv4Step.Status);
        Assert.Equal("SetGateways", Assert.Single(session.Calls).Method);
    }

    [Theory]
    [InlineData("1.1.1.1", null)]
    [InlineData("1.1.1.1", "8.8.8.8")]
    public async Task ApplyStaticAsync_WhenOnlyManualDnsChanges_DoesNotInvokeEnableStaticOrGateway(
        string primaryDns,
        string? secondaryDns)
    {
        FakeWmiSession session = new();
        FakeManualIpv4DnsWriter dnsWriter = new();

        NetworkApplyResult result = await CreateConfigurator(session, dnsWriter: dnsWriter)
            .ApplyStaticAsync(
                AdapterId,
                Plan(
                    primaryDns: primaryDns,
                    secondaryDns: secondaryDns,
                    gatewayMode: GatewayMutationMode.LeaveUnchanged,
                    previousMode: NetworkConfigurationMode.Static,
                    applyIpv4Address: false),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NetworkMutationStepStatus.NotRequired, result.Ipv4Step.Status);
        Assert.Equal(NetworkMutationStepStatus.NotRequired, result.GatewayStep.Status);
        Assert.Empty(session.Calls);
        Assert.Equal(
            new[] { primaryDns, secondaryDns }.Where(value => value is not null).ToArray(),
            dnsWriter.Servers);
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenGatewayOnlyProviderFails_PreservesFailureWithoutLaterCall()
    {
        FakeWmiSession session = new(71);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            Plan(
                dnsMode: DnsMutationMode.LeaveUnchanged,
                previousMode: NetworkConfigurationMode.Static,
                applyIpv4Address: false),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsPartialFailure);
        Assert.Equal((uint)71, result.GatewayStep.TechnicalCode);
        Assert.Equal("SetGateways", Assert.Single(session.Calls).Method);
        Assert.Equal(NetworkMutationStepStatus.NotAttempted, result.DnsStep.Status);
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
        Assert.Equal(2, session.Calls.Count);
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
            : new FakeWmiSession(0, 0);
        FakeManualIpv4DnsWriter dnsWriter = new();

        if (failingCall == 2)
        {
            dnsWriter.Result = new ManualIpv4DnsWriteResult(
                ManualIpv4DnsWriteStatus.NativeCallFailed,
                81);
        }

        NetworkApplyResult result = await CreateConfigurator(session, dnsWriter: dnsWriter).ApplyStaticAsync(
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
        FakeManualIpv4DnsWriter dnsWriter = new()
        {
            Result = new ManualIpv4DnsWriteResult(
                ManualIpv4DnsWriteStatus.NativeCallFailed,
                96)
        };

        NetworkApplyResult result = await CreateConfigurator(
                new FakeWmiSession(),
                dnsWriter: dnsWriter)
            .ApplyStaticAsync(
                AdapterId,
                Plan(
                    applyIpv4Address: false,
                    gatewayMode: GatewayMutationMode.LeaveUnchanged,
                    previousMode: NetworkConfigurationMode.Static),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsPartialFailure);
        Assert.Equal((uint)96, result.DnsStep.TechnicalCode);
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenDnsIsCleared_UsesExplicitNullInputWithoutEmptyValue()
    {
        FakeWmiSession session = new(0);
        FakeManualIpv4DnsWriter dnsWriter = new();
        StaticIpv4MutationPlan plan = Plan(
            gateway: null,
            primaryDns: null,
            secondaryDns: null,
            gatewayMode: GatewayMutationMode.LeaveAbsent,
            gatewayMetric: null,
            dnsMode: DnsMutationMode.ClearToAutomatic,
            previousMode: NetworkConfigurationMode.Static,
            applyIpv4Address: false);

        NetworkApplyResult result = await CreateConfigurator(session, dnsWriter: dnsWriter).ApplyStaticAsync(
            AdapterId,
            plan,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        WmiCall call = Assert.Single(session.Calls);
        Assert.Equal("SetDNSServerSearchOrder", call.Method);
        KeyValuePair<string, object?> parameter = Assert.Single(call.Parameters!);
        Assert.Equal("DNSServerSearchOrder", parameter.Key);
        Assert.Null(parameter.Value);
        Assert.False(parameter.Value is string);
        Assert.False(parameter.Value is Array);
        Assert.Equal(NetworkMutationStepStatus.NotRequired, result.Ipv4Step.Status);
        Assert.Equal(NetworkMutationStepStatus.NotRequired, result.GatewayStep.Status);
        Assert.Equal(0, dnsWriter.CallCount);
    }

    [Theory]
    [InlineData(64, NetworkMutationFailureKind.OperationalFailure,
        "Automatic DNS reset is not supported by the WMI provider.")]
    [InlineData(68, NetworkMutationFailureKind.OperationalFailure,
        "Automatic DNS reset was rejected as an invalid input parameter.")]
    [InlineData(91, NetworkMutationFailureKind.AccessDenied,
        "Automatic DNS reset was denied by the WMI provider.")]
    public async Task ApplyStaticAsync_WhenAutomaticDnsResetFails_PreservesProviderFailure(
        uint code,
        NetworkMutationFailureKind expectedFailure,
        string expectedMessage)
    {
        FakeWmiSession session = new(code);
        StaticIpv4MutationPlan plan = Plan(
            gateway: null,
            primaryDns: null,
            secondaryDns: null,
            gatewayMode: GatewayMutationMode.LeaveAbsent,
            gatewayMetric: null,
            dnsMode: DnsMutationMode.ClearToAutomatic,
            previousMode: NetworkConfigurationMode.Static,
            applyIpv4Address: false);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyStaticAsync(
            AdapterId,
            plan,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsPartialFailure);
        Assert.Equal(NetworkMutationStepStatus.Failed, result.DnsStep.Status);
        Assert.Equal(code, result.DnsStep.TechnicalCode);
        Assert.Equal(expectedFailure, result.FailureKind);
        Assert.Equal(expectedMessage, result.TechnicalMessage);
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
            new WmiNetworkAdapterConfigurator(factory, new FakeDefaultRouteManager()).ApplyStaticAsync(
                AdapterId,
                plan,
                CancellationToken.None));

        Assert.Equal(0, factory.ResolveCount);
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenGatewayMustBeCleared_UsesExactRouteManagerNotWmiSentinel()
    {
        FakeWmiSession session = new();
        FakeDefaultRouteManager routeManager = new();
        StaticIpv4MutationPlan plan = Plan(
            gateway: null,
            gatewayMode: GatewayMutationMode.Clear,
            gatewayMetric: null,
            previousMode: NetworkConfigurationMode.Static,
            applyIpv4Address: false);

        NetworkApplyResult result = await CreateConfigurator(session, routeManager).ApplyStaticAsync(
            AdapterId,
            plan,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AdapterId, routeManager.LastAdapterId);
        Assert.Equal(NetworkMutationStepStatus.NotRequired, result.Ipv4Step.Status);
        Assert.Empty(session.Calls);
        Assert.DoesNotContain(session.Calls, call => call.Method == "SetGateways");
    }

    [Fact]
    public async Task ApplyStaticAsync_WhenRouteManagerReportsRouteRemaining_DoesNotReportSuccess()
    {
        FakeWmiSession session = new();
        FakeDefaultRouteManager routeManager = new()
        {
            Result = new Ipv4DefaultRouteClearResult(Ipv4DefaultRouteClearStatus.RouteStillPresent)
        };
        StaticIpv4MutationPlan plan = Plan(
            gateway: null,
            gatewayMode: GatewayMutationMode.Clear,
            gatewayMetric: null,
            previousMode: NetworkConfigurationMode.Static,
            applyIpv4Address: false);

        NetworkApplyResult result = await CreateConfigurator(session, routeManager).ApplyStaticAsync(
            AdapterId,
            plan,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsPartialFailure);
        Assert.Equal(NetworkMutationFailureKind.RouteVerificationFailure, result.FailureKind);
        Assert.Equal(NetworkMutationStepStatus.NotAttempted, result.DnsStep.Status);
    }

    [Theory]
    [InlineData(WmiAdapterResolutionStatus.Unavailable, NetworkMutationFailureKind.AdapterUnavailable)]
    [InlineData(WmiAdapterResolutionStatus.Ambiguous, NetworkMutationFailureKind.AdapterMappingAmbiguous)]
    internal async Task ApplyStaticAsync_WhenIdentityCannotResolveUniquely_DoesNotMutate(
        WmiAdapterResolutionStatus status,
        NetworkMutationFailureKind expected)
    {
        FakeWmiFactory factory = new(new WmiAdapterResolution(status));
        WmiNetworkAdapterConfigurator configurator = new(factory, new FakeDefaultRouteManager());

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
            () => new WmiNetworkAdapterConfigurator(factory, new FakeDefaultRouteManager()).ApplyStaticAsync(
                AdapterId,
                Plan(),
                cancellation.Token));

        Assert.Equal(0, factory.ResolveCount);
    }

    [Fact]
    public async Task ApplyDhcpAsync_WhenMutationRuns_InvokesEnableDhcpWithoutParametersAndResetsDns()
    {
        FakeWmiSession session = new(0, 0);
        FakeDefaultRouteManager routeManager = new();

        NetworkApplyResult result = await CreateConfigurator(session, routeManager).ApplyDhcpAsync(
            AdapterId,
            new DhcpMutationPlan(NetworkConfigurationMode.Static, ReturnDnsToAutomatic: true),
            CancellationToken.None);

        Assert.Equal(2, session.Calls.Count);
        Assert.Equal("EnableDHCP", session.Calls[0].Method);
        Assert.Null(session.Calls[0].Parameters);
        Assert.Equal("SetDNSServerSearchOrder", session.Calls[1].Method);
        KeyValuePair<string, object?> parameter = Assert.Single(session.Calls[1].Parameters!);
        Assert.Equal("DNSServerSearchOrder", parameter.Key);
        Assert.Null(parameter.Value);
        Assert.Equal(NetworkMutationStepStatus.Succeeded, result.Ipv4Step.Status);
        Assert.Equal(NetworkMutationStepStatus.NotAttempted, result.GatewayStep.Status);
        Assert.Equal(NetworkMutationStepStatus.Succeeded, result.DnsStep.Status);
        Assert.Null(routeManager.LastAdapterId);
    }

    [Theory]
    [InlineData(0, NetworkMutationStepStatus.Succeeded)]
    [InlineData(1, NetworkMutationStepStatus.SucceededRestartRequired)]
    [InlineData(70, NetworkMutationStepStatus.Failed)]
    public async Task ApplyDhcpAsync_WhenEnableDhcpReturnsCode_MapsOrdinaryResult(
        uint code,
        NetworkMutationStepStatus expectedStatus)
    {
        FakeWmiSession session = code is 0 or 1
            ? new FakeWmiSession(code, 0)
            : new FakeWmiSession(code);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyDhcpAsync(
            AdapterId,
            new DhcpMutationPlan(NetworkConfigurationMode.Static, ReturnDnsToAutomatic: true),
            CancellationToken.None);

        Assert.Equal(expectedStatus, result.Ipv4Step.Status);
        Assert.Equal(code, result.Ipv4Step.TechnicalCode);
        Assert.Equal(code == 1, result.RequiresRestart);
    }

    [Fact]
    public async Task ApplyDhcpAsync_WhenEnableDhcpReturns81_TreatsItAsFailure()
    {
        FakeWmiSession session = new(81);

        NetworkApplyResult result = await CreateConfigurator(session).ApplyDhcpAsync(
            AdapterId,
            new DhcpMutationPlan(NetworkConfigurationMode.Dhcp, ReturnDnsToAutomatic: true),
            CancellationToken.None);

        Assert.Equal(NetworkMutationStepStatus.Failed, result.Ipv4Step.Status);
        Assert.Equal((uint)81, result.Ipv4Step.TechnicalCode);
        Assert.Equal(NetworkMutationFailureKind.OperationalFailure, result.FailureKind);
        Assert.Equal(NetworkMutationStepStatus.NotAttempted, result.DnsStep.Status);
        Assert.Single(session.Calls);
    }

    [Theory]
    [InlineData(WmiAdapterResolutionStatus.Unavailable, NetworkMutationFailureKind.AdapterUnavailable)]
    [InlineData(WmiAdapterResolutionStatus.Ambiguous, NetworkMutationFailureKind.AdapterMappingAmbiguous)]
    internal async Task ApplyDhcpAsync_WhenIdentityCannotResolveUniquely_ReturnsTypedFailure(
        WmiAdapterResolutionStatus status,
        NetworkMutationFailureKind expected)
    {
        FakeWmiFactory factory = new(new WmiAdapterResolution(status));
        WmiNetworkAdapterConfigurator configurator = new(factory, new FakeDefaultRouteManager());

        NetworkApplyResult result = await configurator.ApplyDhcpAsync(
            AdapterId,
            new DhcpMutationPlan(NetworkConfigurationMode.Static, ReturnDnsToAutomatic: true),
            CancellationToken.None);

        Assert.Equal(expected, result.FailureKind);
        Assert.False(result.WasMutationAttempted);
    }

    [Fact]
    public void BuildExactIdentityQuery_EscapesIdentityAndNeverUsesDisplayMetadata()
    {
        string query = SystemWmiNetworkAdapterSessionFactory.BuildExactIdentityQuery("{A}' OR 1=1");

        Assert.Contains("SettingID = '{A}'' OR 1=1'", query, StringComparison.Ordinal);
        Assert.DoesNotContain("Description", query, StringComparison.Ordinal);
        Assert.DoesNotContain("Caption", query, StringComparison.Ordinal);
    }

    private static WmiNetworkAdapterConfigurator CreateConfigurator(
        FakeWmiSession session,
        IIpv4DefaultRouteManager? routeManager = null,
        IManualIpv4DnsWriter? dnsWriter = null) =>
        new(
            new FakeWmiFactory(
                new WmiAdapterResolution(WmiAdapterResolutionStatus.Found, session)),
            routeManager ?? new FakeDefaultRouteManager(),
            dnsWriter ?? new FakeManualIpv4DnsWriter());

    private static StaticIpv4MutationPlan Plan(
        string? gateway = "192.168.1.1",
        string? primaryDns = "1.1.1.1",
        string? secondaryDns = "8.8.8.8",
        GatewayMutationMode gatewayMode = GatewayMutationMode.Set,
        ushort? gatewayMetric = 1,
        DnsMutationMode dnsMode = DnsMutationMode.Set,
        NetworkConfigurationMode previousMode = NetworkConfigurationMode.Dhcp,
        bool applyIpv4Address = true) =>
        new(
            new StaticIpv4Configuration(
                "192.168.1.60",
                "255.255.255.0",
                gateway,
                primaryDns,
                secondaryDns),
            applyIpv4Address,
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

    private sealed class FakeDefaultRouteManager : IIpv4DefaultRouteManager
    {
        public Ipv4DefaultRouteClearResult Result { get; set; } =
            Ipv4DefaultRouteClearResult.Success();

        public NetworkAdapterId? LastAdapterId { get; private set; }

        public Ipv4DefaultRouteClearResult Clear(NetworkAdapterId adapterId)
        {
            LastAdapterId = adapterId;
            return Result;
        }
    }

    private sealed class FakeManualIpv4DnsWriter : IManualIpv4DnsWriter
    {
        public ManualIpv4DnsWriteResult Result { get; set; } =
            new(ManualIpv4DnsWriteStatus.Success, 0);

        public NetworkAdapterId? AdapterId { get; private set; }

        public string[]? Servers { get; private set; }

        public List<string>? Order { get; set; }

        public int CallCount { get; private set; }

        public ManualIpv4DnsWriteResult Write(
            NetworkAdapterId adapterId,
            IReadOnlyList<string> servers)
        {
            CallCount++;
            AdapterId = adapterId;
            Servers = servers.ToArray();
            Order?.Add("ManualDns");
            return Result;
        }
    }

    private sealed class FakeWmiSession : IWmiNetworkAdapterSession
    {
        private readonly Queue<uint> _codes;

        public FakeWmiSession(params uint[] codes) => _codes = new Queue<uint>(codes);

        public List<WmiCall> Calls { get; } = new();

        public Exception? Failure { get; set; }

        public List<string>? Order { get; set; }

        public uint Invoke(string methodName, IReadOnlyDictionary<string, object?>? parameters)
        {
            Calls.Add(new WmiCall(methodName, parameters));
            Order?.Add(methodName);

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
