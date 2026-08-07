using System.Net.NetworkInformation;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class NetworkConfigurationPreflightServiceTests
{
    private static readonly NetworkAdapterId AdapterId = new("{A}");

    [Fact]
    public async Task PreflightAsync_WhenAdapterExists_UsesFreshReadAndReturnsReady()
    {
        FakeNetworkAdapterReader reader = ReaderWith(TestData.Snapshot(id: AdapterId.Value));
        FakeIpv4ConflictProbe probe = new();
        NetworkConfigurationPreflightService service = CreateService(reader, probe);

        NetworkConfigurationPreflightResult result = await service.PreflightAsync(
            AdapterId,
            Desired(),
            CancellationToken.None);

        Assert.Equal(NetworkConfigurationPreflightStatus.Ready, result.Status);
        Assert.Equal(1, reader.ReadCount);
        Assert.Equal("192.168.1.60", probe.LastAddress);
        Assert.Equal(Ipv4ConflictProbeStatus.NoResponse, result.ConflictProbe!.Status);
    }

    [Fact]
    public async Task PreflightAsync_WhenExactAdapterDisappears_ReturnsAdapterUnavailable()
    {
        FakeNetworkAdapterReader reader = ReaderWith(TestData.Snapshot(id: "{OTHER}", name: "Same display name"));
        FakeIpv4ConflictProbe probe = new();

        NetworkConfigurationPreflightResult result = await CreateService(reader, probe).PreflightAsync(
            AdapterId,
            Desired(),
            CancellationToken.None);

        Assert.Equal(NetworkConfigurationPreflightStatus.AdapterUnavailable, result.Status);
        Assert.Equal(0, probe.ProbeCount);
    }

    [Fact]
    public async Task PreflightAsync_WhenDesiredConfigurationIsInvalid_ReturnsFieldErrorsWithoutProbe()
    {
        FakeIpv4ConflictProbe probe = new();

        NetworkConfigurationPreflightResult result = await CreateService(
                ReaderWith(TestData.Snapshot(id: AdapterId.Value)),
                probe)
            .PreflightAsync(AdapterId, Desired(ipv4Address: "not-ip"), CancellationToken.None);

        Assert.Equal(NetworkConfigurationPreflightStatus.ValidationFailed, result.Status);
        Assert.False(result.Validation!.IsValid);
        Assert.Equal(0, probe.ProbeCount);
    }

    [Fact]
    public async Task PreflightAsync_WhenCurrentStateIsEquivalent_ReturnsNoChangeWithoutProbe()
    {
        NetworkAdapterSnapshot current = TestData.Snapshot(
            id: AdapterId.Value,
            mode: NetworkConfigurationMode.Static,
            gateway: null,
            primaryDns: null,
            secondaryDns: null);
        FakeIpv4ConflictProbe probe = new()
        {
            Result = new Ipv4ConflictProbeResult(Ipv4ConflictProbeStatus.ResponseObserved)
        };

        NetworkConfigurationPreflightResult result = await CreateService(ReaderWith(current), probe)
            .PreflightAsync(
                AdapterId,
                Desired(ipv4Address: " 192.168.001.050 "),
                CancellationToken.None);

        Assert.Equal(NetworkConfigurationPreflightStatus.NoChange, result.Status);
        Assert.True(result.Comparison!.IsEquivalent);
        Assert.Equal(0, probe.ProbeCount);
    }

    [Fact]
    public async Task PreflightAsync_WhenCurrentAddressMatchesButAnotherFieldDiffers_DoesNotProbeOwnAddress()
    {
        NetworkAdapterSnapshot current = TestData.Snapshot(
            id: AdapterId.Value,
            mode: NetworkConfigurationMode.Dhcp,
            gateway: null,
            primaryDns: null,
            secondaryDns: null);
        FakeIpv4ConflictProbe probe = new()
        {
            Result = new Ipv4ConflictProbeResult(Ipv4ConflictProbeStatus.ResponseObserved)
        };

        NetworkConfigurationPreflightResult result = await CreateService(ReaderWith(current), probe)
            .PreflightAsync(
                AdapterId,
                Desired(ipv4Address: "192.168.1.50"),
                CancellationToken.None);

        Assert.Equal(NetworkConfigurationPreflightStatus.Ready, result.Status);
        Assert.True(result.Comparison!.HasDifference(NetworkConfigurationDifference.Mode));
        Assert.Null(result.ConflictProbe);
        Assert.Equal(0, probe.ProbeCount);
    }

    [Fact]
    public async Task PreflightAsync_WhenAdapterHasAdditionalIpv4_ReturnsSafetyConditionWithoutProbe()
    {
        Ipv4AddressCollection addresses = new(new[]
        {
            new Ipv4AddressAssignment("192.168.1.50", "255.255.255.0"),
            new Ipv4AddressAssignment("10.0.0.2", "255.255.255.0")
        });
        NetworkAdapterSnapshot current = TestData.Snapshot(id: AdapterId.Value, ipv4Addresses: addresses);
        FakeIpv4ConflictProbe probe = new();

        NetworkConfigurationPreflightResult result = await CreateService(ReaderWith(current), probe)
            .PreflightAsync(AdapterId, Desired(), CancellationToken.None);

        Assert.Equal(NetworkConfigurationPreflightStatus.MultipleIpv4RequiresSafetyDecision, result.Status);
        Assert.True(result.CurrentSnapshot!.HasAdditionalIpv4Addresses);
        Assert.Equal(0, probe.ProbeCount);
    }

    [Fact]
    public async Task PreflightAsync_WhenProbeObservesResponse_ReturnsPotentialConflict()
    {
        FakeIpv4ConflictProbe probe = new()
        {
            Result = new Ipv4ConflictProbeResult(Ipv4ConflictProbeStatus.ResponseObserved)
        };

        NetworkConfigurationPreflightResult result = await CreateService(
                ReaderWith(TestData.Snapshot(id: AdapterId.Value)),
                probe)
            .PreflightAsync(AdapterId, Desired(), CancellationToken.None);

        Assert.Equal(NetworkConfigurationPreflightStatus.PotentialAddressConflict, result.Status);
    }

    [Fact]
    public async Task PreflightAsync_WhenProbeHasNoResponse_ReturnsReadyButKeepsInconclusiveEvidence()
    {
        FakeIpv4ConflictProbe probe = new();

        NetworkConfigurationPreflightResult result = await CreateService(
                ReaderWith(TestData.Snapshot(id: AdapterId.Value)),
                probe)
            .PreflightAsync(AdapterId, Desired(), CancellationToken.None);

        Assert.Equal(NetworkConfigurationPreflightStatus.Ready, result.Status);
        Assert.Equal(Ipv4ConflictProbeStatus.NoResponse, result.ConflictProbe!.Status);
    }

    [Fact]
    public async Task PreflightAsync_WhenProbeReportsUnavailable_ReturnsIndeterminate()
    {
        FakeIpv4ConflictProbe probe = new()
        {
            Result = new Ipv4ConflictProbeResult(Ipv4ConflictProbeStatus.Unavailable)
        };

        NetworkConfigurationPreflightResult result = await CreateService(
                ReaderWith(TestData.Snapshot(id: AdapterId.Value)),
                probe)
            .PreflightAsync(AdapterId, Desired(), CancellationToken.None);

        Assert.Equal(NetworkConfigurationPreflightStatus.ProbeIndeterminate, result.Status);
    }

    [Fact]
    public async Task PreflightAsync_WhenProbeHasUnexpectedDefect_PropagatesException()
    {
        FakeIpv4ConflictProbe probe = new();
        probe.FailWith(new InvalidOperationException("probe failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(
                    ReaderWith(TestData.Snapshot(id: AdapterId.Value)),
                    probe)
                .PreflightAsync(AdapterId, Desired(), CancellationToken.None));
    }

    [Fact]
    public async Task PreflightAsync_WhenProbeReportsCancellation_ReturnsCancelled()
    {
        FakeIpv4ConflictProbe probe = new()
        {
            Result = new Ipv4ConflictProbeResult(Ipv4ConflictProbeStatus.Cancelled)
        };

        NetworkConfigurationPreflightResult result = await CreateService(
                ReaderWith(TestData.Snapshot(id: AdapterId.Value)),
                probe)
            .PreflightAsync(AdapterId, Desired(), CancellationToken.None);

        Assert.Equal(NetworkConfigurationPreflightStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task PreflightAsync_WhenTokenIsAlreadyCancelled_ReturnsCancelledWithoutProbe()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        FakeIpv4ConflictProbe probe = new();

        NetworkConfigurationPreflightResult result = await CreateService(
                ReaderWith(TestData.Snapshot(id: AdapterId.Value)),
                probe)
            .PreflightAsync(AdapterId, Desired(), cancellation.Token);

        Assert.Equal(NetworkConfigurationPreflightStatus.Cancelled, result.Status);
        Assert.Equal(0, probe.ProbeCount);
    }

    [Fact]
    public async Task PreflightAsync_WhenFreshReadFails_ReturnsReadFailure()
    {
        FakeNetworkAdapterReader reader = new();
        reader.FailWith(new NetworkInformationException());

        NetworkConfigurationPreflightResult result = await CreateService(reader, new FakeIpv4ConflictProbe())
            .PreflightAsync(AdapterId, Desired(), CancellationToken.None);

        Assert.Equal(NetworkConfigurationPreflightStatus.AdapterReadFailed, result.Status);
    }

    [Fact]
    public async Task PreflightAsync_WhenFreshReadHasUnexpectedDefect_PropagatesException()
    {
        FakeNetworkAdapterReader reader = new();
        reader.FailWith(new InvalidOperationException("reader defect"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(reader, new FakeIpv4ConflictProbe())
                .PreflightAsync(AdapterId, Desired(), CancellationToken.None));
    }

    private static NetworkConfigurationPreflightService CreateService(
        INetworkAdapterReader reader,
        IIpv4ConflictProbe probe) =>
        new(
            reader,
            new StaticIpv4ConfigurationValidator(),
            new StaticIpv4ConfigurationComparer(),
            probe);

    private static FakeNetworkAdapterReader ReaderWith(params NetworkAdapterSnapshot[] adapters)
    {
        FakeNetworkAdapterReader reader = new();
        reader.EnqueueResult(adapters);
        return reader;
    }

    private static StaticIpv4Configuration Desired(
        string ipv4Address = "192.168.1.60",
        string subnetMask = "255.255.255.0",
        string? gateway = null,
        string? primaryDns = null,
        string? secondaryDns = null) =>
        new(ipv4Address, subnetMask, gateway, primaryDns, secondaryDns);
}
