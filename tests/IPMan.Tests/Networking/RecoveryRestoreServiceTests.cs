using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class RecoveryRestoreServiceTests
{
    private static readonly NetworkAdapterId AdapterId = new("{A}");

    [Fact]
    public async Task RestoreLatestAsync_WhenSnapshotDoesNotExist_ReturnsNoSnapshotWithoutApply()
    {
        RestoreContext context = CreateContext(loadResult: RecoverySnapshotLoadResult.Failed(
            RecoverySnapshotLoadStatus.NotFound));

        RecoveryRestoreResult result = await context.Service.RestoreLatestAsync(
            new RecoveryRestoreRequest(AdapterId),
            CancellationToken.None);

        Assert.Equal(RecoveryRestoreStatus.NoSnapshotFound, result.Status);
        Assert.Empty(context.StaticApply.Requests);
        Assert.Empty(context.DhcpApply.Requests);
        Assert.Equal(0, context.RecoveryReader.ReadCount);
    }

    [Fact]
    public async Task RestoreLatestAsync_WhenSnapshotUsesDhcp_DelegatesOnlyToDhcpApply()
    {
        RecoverySnapshot snapshot = Snapshot(NetworkConfigurationMode.Dhcp);
        RestoreContext context = CreateContext(snapshot);

        RecoveryRestoreResult result = await context.Service.RestoreLatestAsync(
            new RecoveryRestoreRequest(AdapterId),
            CancellationToken.None);

        Assert.Equal(RecoveryRestoreStatus.VerifiedSuccess, result.Status);
        DhcpApplyRequest request = Assert.Single(context.DhcpApply.Requests);
        Assert.Equal(AdapterId, request.AdapterId);
        Assert.Empty(context.StaticApply.Requests);
        Assert.Same(snapshot, result.RestoredFrom);
        Assert.Same(context.DhcpApply.Result, result.DhcpOutcome);
    }

    [Fact]
    public async Task RestoreLatestAsync_WhenSnapshotUsesStatic_PassesRecordedConfigurationExactly()
    {
        RecoverySnapshot snapshot = Snapshot(NetworkConfigurationMode.Static);
        RestoreContext context = CreateContext(snapshot);

        RecoveryRestoreResult result = await context.Service.RestoreLatestAsync(
            new RecoveryRestoreRequest(AdapterId),
            CancellationToken.None);

        Assert.Equal(RecoveryRestoreStatus.VerifiedSuccess, result.Status);
        StaticIpv4ApplyRequest request = Assert.Single(context.StaticApply.Requests);
        Assert.Equal(AdapterId, request.AdapterId);
        Assert.Equal("192.168.50.25", request.DesiredConfiguration.Ipv4Address);
        Assert.Equal("255.255.255.0", request.DesiredConfiguration.SubnetMask);
        Assert.Equal("192.168.50.1", request.DesiredConfiguration.Gateway);
        Assert.Equal("1.1.1.1", request.DesiredConfiguration.PrimaryDns);
        Assert.Equal("8.8.8.8", request.DesiredConfiguration.SecondaryDns);
        Assert.Empty(context.DhcpApply.Requests);
        Assert.Same(context.StaticApply.Result, result.StaticOutcome);
    }

    [Fact]
    public async Task RestoreLatestAsync_WhenStaticSnapshotUsesAutomaticDns_PassesNullDnsFields()
    {
        RecoverySnapshot snapshot = Snapshot(NetworkConfigurationMode.Static) with
        {
            DnsMode = DnsConfigurationMode.Automatic,
            ConfiguredIpv4DnsServers = ["stale-manual-value"]
        };
        RestoreContext context = CreateContext(snapshot);

        await context.Service.RestoreLatestAsync(
            new RecoveryRestoreRequest(AdapterId),
            CancellationToken.None);

        StaticIpv4ApplyRequest request = Assert.Single(context.StaticApply.Requests);
        Assert.Null(request.DesiredConfiguration.PrimaryDns);
        Assert.Null(request.DesiredConfiguration.SecondaryDns);
    }

    [Fact]
    public async Task RestoreLatestAsync_WhenSnapshotModeIsUnknown_ReturnsUnsupportedWithoutApply()
    {
        RestoreContext context = CreateContext(Snapshot(NetworkConfigurationMode.Unknown));

        RecoveryRestoreResult result = await context.Service.RestoreLatestAsync(
            new RecoveryRestoreRequest(AdapterId),
            CancellationToken.None);

        Assert.Equal(RecoveryRestoreStatus.UnsupportedSnapshot, result.Status);
        Assert.Empty(context.StaticApply.Requests);
        Assert.Empty(context.DhcpApply.Requests);
    }

    [Fact]
    public async Task RestoreLatestAsync_WhenFreshIdentityDiffers_ReturnsIdentityChangedWithoutApply()
    {
        RecoverySnapshot snapshot = Snapshot(NetworkConfigurationMode.Static);
        RestoreContext context = CreateContext(
            snapshot,
            currentIdentity: new NetworkAdapterId("{B}"));

        RecoveryRestoreResult result = await context.Service.RestoreLatestAsync(
            new RecoveryRestoreRequest(AdapterId),
            CancellationToken.None);

        Assert.Equal(RecoveryRestoreStatus.AdapterIdentityChanged, result.Status);
        Assert.Empty(context.StaticApply.Requests);
        Assert.Empty(context.DhcpApply.Requests);
    }

    [Fact]
    public async Task RestoreLatestAsync_WhenFreshReadIsAmbiguous_ReturnsReadFailedWithoutApply()
    {
        RestoreContext context = CreateContext(
            Snapshot(NetworkConfigurationMode.Static),
            readStatus: NetworkAdapterRecoveryReadStatus.AdapterMappingAmbiguous);

        RecoveryRestoreResult result = await context.Service.RestoreLatestAsync(
            new RecoveryRestoreRequest(AdapterId),
            CancellationToken.None);

        Assert.Equal(RecoveryRestoreStatus.AdapterReadFailed, result.Status);
        Assert.Empty(context.StaticApply.Requests);
        Assert.Empty(context.DhcpApply.Requests);
    }

    [Fact]
    public async Task RestoreLatestAsync_WhenConflictNeedsConfirmation_SurfacesStatusAndForwardsRetryFlag()
    {
        RestoreContext context = CreateContext(Snapshot(NetworkConfigurationMode.Static));
        context.StaticApply.Result = new StaticIpv4ApplyResult(
            StaticIpv4ApplyStatus.ConflictConfirmationRequired);

        RecoveryRestoreResult first = await context.Service.RestoreLatestAsync(
            new RecoveryRestoreRequest(AdapterId),
            CancellationToken.None);
        RecoveryRestoreResult second = await context.Service.RestoreLatestAsync(
            new RecoveryRestoreRequest(AdapterId, ConfirmPotentialConflict: true),
            CancellationToken.None);

        Assert.Equal(RecoveryRestoreStatus.ConflictConfirmationRequired, first.Status);
        Assert.Equal(RecoveryRestoreStatus.ConflictConfirmationRequired, second.Status);
        Assert.Equal(2, context.StaticApply.Requests.Count);
        Assert.False(context.StaticApply.Requests[0].ConfirmPotentialConflict);
        Assert.True(context.StaticApply.Requests[1].ConfirmPotentialConflict);
    }

    [Fact]
    public async Task RestoreLatestAsync_WhenSnapshotHasMultipleAddresses_ReturnsSafetyBlockWithoutApply()
    {
        RecoverySnapshot snapshot = Snapshot(NetworkConfigurationMode.Static) with
        {
            Ipv4Addresses =
            [
                new Ipv4AddressAssignment("192.168.50.25", "255.255.255.0"),
                new Ipv4AddressAssignment("10.0.0.25", "255.255.255.0")
            ]
        };
        RestoreContext context = CreateContext(snapshot);

        RecoveryRestoreResult result = await context.Service.RestoreLatestAsync(
            new RecoveryRestoreRequest(AdapterId),
            CancellationToken.None);

        Assert.Equal(RecoveryRestoreStatus.SafetyBlocked, result.Status);
        Assert.Equal(StaticIpv4SafetyBlock.MultipleIpv4Addresses, result.SafetyBlock);
        Assert.Empty(context.StaticApply.Requests);
        Assert.Empty(context.DhcpApply.Requests);
    }

    [Theory]
    [InlineData(StaticIpv4ApplyStatus.VerifiedSuccess, RecoveryRestoreStatus.VerifiedSuccess)]
    [InlineData(StaticIpv4ApplyStatus.NoChange, RecoveryRestoreStatus.NoChange)]
    [InlineData(StaticIpv4ApplyStatus.ValidationFailed, RecoveryRestoreStatus.UnsupportedSnapshot)]
    [InlineData(StaticIpv4ApplyStatus.AdapterUnavailable, RecoveryRestoreStatus.AdapterUnavailable)]
    [InlineData(StaticIpv4ApplyStatus.AdapterReadFailed, RecoveryRestoreStatus.AdapterReadFailed)]
    [InlineData(StaticIpv4ApplyStatus.SafetyBlocked, RecoveryRestoreStatus.SafetyBlocked)]
    [InlineData(StaticIpv4ApplyStatus.ConflictConfirmationRequired, RecoveryRestoreStatus.ConflictConfirmationRequired)]
    [InlineData(StaticIpv4ApplyStatus.ProbeIndeterminateConfirmationRequired, RecoveryRestoreStatus.ProbeIndeterminateConfirmationRequired)]
    [InlineData(StaticIpv4ApplyStatus.RecoveryCaptureFailed, RecoveryRestoreStatus.RecoveryCaptureFailed)]
    [InlineData(StaticIpv4ApplyStatus.MutationFailed, RecoveryRestoreStatus.MutationFailed)]
    [InlineData(StaticIpv4ApplyStatus.PartialFailure, RecoveryRestoreStatus.PartialFailure)]
    [InlineData(StaticIpv4ApplyStatus.VerificationFailed, RecoveryRestoreStatus.VerificationFailed)]
    [InlineData(StaticIpv4ApplyStatus.AdapterUnavailableDuringVerification, RecoveryRestoreStatus.AdapterUnavailableDuringVerification)]
    [InlineData(StaticIpv4ApplyStatus.Cancelled, RecoveryRestoreStatus.Cancelled)]
    [InlineData(StaticIpv4ApplyStatus.RecoveryStateUnavailable, RecoveryRestoreStatus.RecoveryStateUnavailable)]
    public async Task RestoreLatestAsync_WhenStaticApplyReturnsStatus_MapsStatusExactly(
        StaticIpv4ApplyStatus applyStatus,
        RecoveryRestoreStatus expectedStatus)
    {
        RestoreContext context = CreateContext(Snapshot(NetworkConfigurationMode.Static));
        context.StaticApply.Result = new StaticIpv4ApplyResult(
            applyStatus,
            SafetyBlock: StaticIpv4SafetyBlock.NotElevated);

        RecoveryRestoreResult result = await context.Service.RestoreLatestAsync(
            new RecoveryRestoreRequest(AdapterId),
            CancellationToken.None);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(StaticIpv4SafetyBlock.NotElevated, result.SafetyBlock);
        Assert.Same(context.StaticApply.Result, result.StaticOutcome);
    }

    [Theory]
    [InlineData(DhcpApplyStatus.VerifiedSuccess, RecoveryRestoreStatus.VerifiedSuccess)]
    [InlineData(DhcpApplyStatus.NoChange, RecoveryRestoreStatus.NoChange)]
    [InlineData(DhcpApplyStatus.AdapterUnavailable, RecoveryRestoreStatus.AdapterUnavailable)]
    [InlineData(DhcpApplyStatus.AdapterReadFailed, RecoveryRestoreStatus.AdapterReadFailed)]
    [InlineData(DhcpApplyStatus.SafetyBlocked, RecoveryRestoreStatus.SafetyBlocked)]
    [InlineData(DhcpApplyStatus.RecoveryCaptureFailed, RecoveryRestoreStatus.RecoveryCaptureFailed)]
    [InlineData(DhcpApplyStatus.MutationFailed, RecoveryRestoreStatus.MutationFailed)]
    [InlineData(DhcpApplyStatus.PartialFailure, RecoveryRestoreStatus.PartialFailure)]
    [InlineData(DhcpApplyStatus.VerificationFailed, RecoveryRestoreStatus.VerificationFailed)]
    [InlineData(DhcpApplyStatus.AdapterUnavailableDuringVerification, RecoveryRestoreStatus.AdapterUnavailableDuringVerification)]
    [InlineData(DhcpApplyStatus.Cancelled, RecoveryRestoreStatus.Cancelled)]
    [InlineData(DhcpApplyStatus.RecoveryStateUnavailable, RecoveryRestoreStatus.RecoveryStateUnavailable)]
    public async Task RestoreLatestAsync_WhenDhcpApplyReturnsStatus_MapsStatusExactly(
        DhcpApplyStatus applyStatus,
        RecoveryRestoreStatus expectedStatus)
    {
        RestoreContext context = CreateContext(Snapshot(NetworkConfigurationMode.Dhcp));
        context.DhcpApply.Result = new DhcpApplyResult(
            applyStatus,
            StaticIpv4SafetyBlock.NotElevated);

        RecoveryRestoreResult result = await context.Service.RestoreLatestAsync(
            new RecoveryRestoreRequest(AdapterId),
            CancellationToken.None);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(StaticIpv4SafetyBlock.NotElevated, result.SafetyBlock);
        Assert.Same(context.DhcpApply.Result, result.DhcpOutcome);
    }

    [Fact]
    public async Task RestoreLatestAsync_WhenRestoreFails_DoesNotWriteToRepository()
    {
        RecoverySnapshot snapshot = Snapshot(NetworkConfigurationMode.Static);
        RestoreContext context = CreateContext(snapshot);
        context.StaticApply.Result = new StaticIpv4ApplyResult(
            StaticIpv4ApplyStatus.MutationFailed);

        RecoveryRestoreResult result = await context.Service.RestoreLatestAsync(
            new RecoveryRestoreRequest(AdapterId),
            CancellationToken.None);

        Assert.Equal(RecoveryRestoreStatus.MutationFailed, result.Status);
        Assert.Empty(context.Repository.SavedSnapshots);
        Assert.Same(snapshot, context.Repository.LoadResult.Snapshot);
    }

    private static RestoreContext CreateContext(
        RecoverySnapshot? snapshot = null,
        RecoverySnapshotLoadResult? loadResult = null,
        NetworkAdapterId? currentIdentity = null,
        NetworkAdapterRecoveryReadStatus readStatus = NetworkAdapterRecoveryReadStatus.Success)
    {
        FakeRecoverySnapshotRepository repository = new()
        {
            LoadResult = loadResult ?? RecoverySnapshotLoadResult.Success(
                snapshot ?? Snapshot(NetworkConfigurationMode.Static))
        };
        FakeNetworkAdapterRecoveryReader recoveryReader = new();
        if (readStatus == NetworkAdapterRecoveryReadStatus.Success)
        {
            recoveryReader.Enqueue(CurrentRecovery(currentIdentity ?? AdapterId));
        }
        else
        {
            recoveryReader.Enqueue(new NetworkAdapterRecoveryReadResult(readStatus));
        }

        FakeStaticIpv4ApplyService staticApply = new();
        FakeDhcpApplyService dhcpApply = new();
        RecoveryRestoreService service = new(repository, recoveryReader, staticApply, dhcpApply);
        return new RestoreContext(service, repository, recoveryReader, staticApply, dhcpApply);
    }

    private static RecoverySnapshot Snapshot(NetworkConfigurationMode mode) =>
        new(
            RecoverySnapshotLoadResult.SupportedSchemaVersion,
            "snapshot-1",
            new DateTimeOffset(2026, 8, 20, 10, 0, 0, TimeSpan.Zero),
            RecoverySnapshotState.Captured,
            AdapterId,
            "Ethernet",
            "Contoso Ethernet",
            mode,
            [new Ipv4AddressAssignment("192.168.50.25", "255.255.255.0")],
            [new Ipv4GatewayRecoveryState("192.168.50.1", 25)],
            DnsConfigurationMode.Manual,
            ["1.1.1.1", "8.8.8.8"],
            ["1.1.1.1", "8.8.8.8"]);

    private static NetworkAdapterRecoverySnapshot CurrentRecovery(NetworkAdapterId adapterId)
    {
        NetworkAdapterSnapshot adapter = TestData.Snapshot(
            id: adapterId.Value,
            mode: NetworkConfigurationMode.Static,
            ipv4Address: "10.0.0.2",
            subnetMask: "255.255.255.0");
        return new NetworkAdapterRecoverySnapshot(
            adapter,
            DnsConfigurationMode.Automatic,
            Array.Empty<string>(),
            Array.Empty<Ipv4GatewayRecoveryState>());
    }

    private sealed record RestoreContext(
        RecoveryRestoreService Service,
        FakeRecoverySnapshotRepository Repository,
        FakeNetworkAdapterRecoveryReader RecoveryReader,
        FakeStaticIpv4ApplyService StaticApply,
        FakeDhcpApplyService DhcpApply);
}
