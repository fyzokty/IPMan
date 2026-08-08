using System.Net.NetworkInformation;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using IPMan.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class NetworkAdapterReaderTests
{
    [Fact]
    public async Task GetAdaptersAsync_WhenNoAdaptersExist_ReturnsEmptyResult()
    {
        NetworkAdapterReader reader = CreateReader(new FakeAdapterProbe());

        IReadOnlyList<NetworkAdapterSnapshot> adapters =
            await reader.GetAdaptersAsync(CancellationToken.None);

        Assert.Empty(adapters);
    }

    [Fact]
    public async Task GetAdaptersAsync_ReturnsOneSnapshotPerDiscoverableAdapter()
    {
        NetworkAdapterReader reader = CreateReader(new FakeAdapterProbe().WithAdapters(
            TestData.Adapter(id: "{A}", name: "Ethernet"),
            TestData.Adapter(id: "{B}", name: "Wi-Fi")));

        IReadOnlyList<NetworkAdapterSnapshot> adapters =
            await reader.GetAdaptersAsync(CancellationToken.None);

        Assert.Collection(
            adapters,
            adapter => Assert.Equal("{A}", adapter.Id.Value),
            adapter => Assert.Equal("{B}", adapter.Id.Value));
    }

    [Fact]
    public async Task GetAdaptersAsync_ExcludesSoftwareLoopbackOnly()
    {
        NetworkAdapterReader reader = CreateReader(new FakeAdapterProbe().WithAdapters(
            TestData.Adapter(id: "{A}", interfaceType: NetworkInterfaceType.Ethernet),
            TestData.Adapter(id: "{LOOPBACK}", interfaceType: NetworkInterfaceType.Loopback),
            TestData.Adapter(id: "{TUNNEL}", interfaceType: NetworkInterfaceType.Tunnel)));

        IReadOnlyList<NetworkAdapterSnapshot> adapters =
            await reader.GetAdaptersAsync(CancellationToken.None);

        Assert.Collection(
            adapters,
            adapter => Assert.Equal("{A}", adapter.Id.Value),
            adapter => Assert.Equal("{TUNNEL}", adapter.Id.Value));
    }

    [Fact]
    public async Task GetAdaptersAsync_WhenOneAdapterCannotBeMapped_ReturnsRemainingAdapters()
    {
        NetworkAdapterReader reader = CreateReader(new FakeAdapterProbe().WithAdapters(
            TestData.Adapter(id: " ", name: "Adapter without identity"),
            TestData.Adapter(id: "{B}", name: "Ethernet")));

        IReadOnlyList<NetworkAdapterSnapshot> adapters =
            await reader.GetAdaptersAsync(CancellationToken.None);

        Assert.Equal("{B}", Assert.Single(adapters).Id.Value);
    }

    [Fact]
    public async Task GetAdaptersAsync_WhenAdapterIsRemoved_NoLongerReturnsIt()
    {
        FakeAdapterProbe probe = new FakeAdapterProbe().WithAdapters(
            TestData.Adapter(id: "{A}"),
            TestData.Adapter(id: "{USB}"));

        NetworkAdapterReader reader = CreateReader(probe);

        Assert.Equal(2, (await reader.GetAdaptersAsync(CancellationToken.None)).Count);

        probe.WithAdapters(TestData.Adapter(id: "{A}"));

        IReadOnlyList<NetworkAdapterSnapshot> afterRemoval =
            await reader.GetAdaptersAsync(CancellationToken.None);

        Assert.Equal("{A}", Assert.Single(afterRemoval).Id.Value);
    }

    [Fact]
    public async Task GetAdapterAsync_WhenAdapterExists_ReturnsMatchingSnapshot()
    {
        NetworkAdapterReader reader = CreateReader(new FakeAdapterProbe().WithAdapters(
            TestData.Adapter(id: "{A}", name: "Ethernet"),
            TestData.Adapter(id: "{B}", name: "Wi-Fi")));

        NetworkAdapterSnapshot? adapter =
            await reader.GetAdapterAsync(new NetworkAdapterId("{B}"), CancellationToken.None);

        Assert.NotNull(adapter);
        Assert.Equal("Wi-Fi", adapter.Name);
    }

    [Fact]
    public async Task GetAdapterAsync_WhenWindowsGuidTextDiffers_FindsExactEquivalentIdentity()
    {
        const string targetWindowsId = "{827A2938-BB14-4D18-B67F-94E9C4F818BA}";
        NetworkAdapterReader reader = CreateReader(new FakeAdapterProbe().WithAdapters(
            TestData.Adapter(
                id: "{927A2938-BB14-4D18-B67F-94E9C4F818BA}",
                name: "Ethernet"),
            TestData.Adapter(
                id: targetWindowsId,
                name: "Ethernet")));

        NetworkAdapterSnapshot? adapter = await reader.GetAdapterAsync(
            new NetworkAdapterId("827a2938-bb14-4d18-b67f-94e9c4f818ba"),
            CancellationToken.None);

        Assert.NotNull(adapter);
        Assert.Equal(targetWindowsId, adapter.Id.Value);
    }

    [Fact]
    public async Task GetAdapterAsync_WhenAdapterIsUnknown_ReturnsNull()
    {
        NetworkAdapterReader reader = CreateReader(new FakeAdapterProbe().WithAdapters(
            TestData.Adapter(id: "{A}")));

        NetworkAdapterSnapshot? adapter =
            await reader.GetAdapterAsync(new NetworkAdapterId("{MISSING}"), CancellationToken.None);

        Assert.Null(adapter);
    }

    [Fact]
    public async Task GetAdaptersAsync_WhenAlreadyCancelled_DoesNotReadFromWindows()
    {
        FakeAdapterProbe probe = new FakeAdapterProbe().WithAdapters(TestData.Adapter());
        NetworkAdapterReader reader = CreateReader(probe);

        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => reader.GetAdaptersAsync(cancellation.Token));

        Assert.Equal(0, probe.ReadCount);
    }

    [Fact]
    public async Task GetAdaptersAsync_WhenWindowsReadFails_PropagatesExpectedOperationalFailure()
    {
        NetworkAdapterReader reader = CreateReader(
            new FakeAdapterProbe().FailWith(new NetworkInformationException()));

        await Assert.ThrowsAsync<NetworkInformationException>(
            () => reader.GetAdaptersAsync(CancellationToken.None));
    }

    private static NetworkAdapterReader CreateReader(FakeAdapterProbe probe) =>
        new(probe, NullLogger<NetworkAdapterReader>.Instance);
}
