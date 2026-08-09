using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using IPMan.IntegrationTests.Harness;
using Xunit;

namespace IPMan.IntegrationTests.Evidence;

public sealed class RollbackSnapshotVerifierTests
{
    private static readonly NetworkAdapterId AdapterId =
        new("{827A2938-BB14-4D18-B67F-94E9C4F818BA}");

    [Fact]
    public async Task MatchesAsync_WhenPersistedSnapshotMatchesExactSource_ReturnsTrue()
    {
        NetworkAdapterRecoverySnapshot source = Recovery("10.250.0.10");
        RollbackSnapshotReference reference = await WriteRollbackAsync(source);

        try
        {
            Assert.True(await RollbackSnapshotVerifier.MatchesAsync(
                reference,
                source,
                CancellationToken.None));
        }
        finally
        {
            File.Delete(reference.StoragePath);
        }
    }

    [Fact]
    public async Task MatchesAsync_WhenRestoreStateDiffers_ReturnsFalse()
    {
        NetworkAdapterRecoverySnapshot persistedSource = Recovery("10.250.0.10");
        NetworkAdapterRecoverySnapshot differentState = Recovery("10.250.0.11");
        RollbackSnapshotReference reference = await WriteRollbackAsync(persistedSource);

        try
        {
            Assert.False(await RollbackSnapshotVerifier.MatchesAsync(
                reference,
                differentState,
                CancellationToken.None));
        }
        finally
        {
            File.Delete(reference.StoragePath);
        }
    }

    [Fact]
    public async Task ApplyCapture_WhenHarnessPreReadDiffers_VerifiesAgainstFirstApplyRead()
    {
        NetworkAdapterRecoverySnapshot harnessPreRead = Recovery("10.250.0.9");
        NetworkAdapterRecoverySnapshot applyRollbackSource = Recovery("10.250.0.10");
        NetworkAdapterRecoverySnapshot applyVerificationRead = Recovery("10.250.0.20");
        SequencedRecoveryReader inner = new(
            harnessPreRead,
            applyRollbackSource,
            applyVerificationRead);
        RecordingNetworkAdapterRecoveryReader recorder = new(inner);

        NetworkAdapterRecoveryReadResult first = await inner.ReadAsync(
            AdapterId,
            CancellationToken.None);
        recorder.BeginApplyCapture();
        _ = await recorder.ReadAsync(AdapterId, CancellationToken.None);
        _ = await recorder.ReadAsync(AdapterId, CancellationToken.None);
        NetworkAdapterRecoveryReadResult captured = Assert.IsType<NetworkAdapterRecoveryReadResult>(
            recorder.EndApplyCapture());
        RollbackSnapshotReference reference = await WriteRollbackAsync(applyRollbackSource);

        try
        {
            Assert.NotEqual(first.Snapshot, captured.Snapshot);
            Assert.Equal(applyRollbackSource, captured.Snapshot);
            Assert.False(await RollbackSnapshotVerifier.MatchesAsync(
                reference,
                first.Snapshot!,
                CancellationToken.None));
            Assert.True(await RollbackSnapshotVerifier.MatchesAsync(
                reference,
                captured.Snapshot!,
                CancellationToken.None));
        }
        finally
        {
            File.Delete(reference.StoragePath);
        }
    }

    private static async Task<RollbackSnapshotReference> WriteRollbackAsync(
        NetworkAdapterRecoverySnapshot source)
    {
        NetworkRollbackSnapshot snapshot = new(
            SchemaVersion: 2,
            SnapshotId: Guid.NewGuid().ToString("N"),
            CapturedAtUtc: new DateTimeOffset(2026, 8, 9, 9, 0, 0, TimeSpan.Zero),
            State: RollbackSnapshotState.Captured,
            AdapterId: source.Adapter.Id,
            AdapterName: source.Adapter.Name,
            AdapterDescription: source.Adapter.Description,
            Mode: source.Adapter.Mode,
            Ipv4Addresses: source.Adapter.Ipv4Addresses.ToArray(),
            Ipv4Gateways: source.Ipv4Gateways.ToArray(),
            DnsMode: source.DnsMode,
            ConfiguredIpv4DnsServers: source.ConfiguredIpv4DnsServers.ToArray(),
            Ipv4DnsServers: source.Adapter.Ipv4DnsServers.ToArray());
        string path = Path.Combine(Path.GetTempPath(), $"IPMan-rollback-{Guid.NewGuid():N}.json");

        await using (FileStream stream = new(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.Asynchronous))
        {
            await new RollbackSnapshotJsonCodec()
                .SerializeAsync(stream, snapshot, CancellationToken.None);
        }

        return new RollbackSnapshotReference(snapshot.SnapshotId, path);
    }

    private static NetworkAdapterRecoverySnapshot Recovery(string ipv4Address)
    {
        Ipv4AddressAssignment assignment = new(ipv4Address, "255.255.255.0");
        NetworkAdapterSnapshot adapter = new(
            AdapterId,
            "Ethernet",
            "Isolated test adapter",
            "00-11-22-33-44-55",
            IsConnected: true,
            LinkSpeedBitsPerSecond: 1_000_000_000,
            NetworkConfigurationMode.Static,
            ipv4Address,
            assignment.SubnetMask,
            Gateway: null,
            PrimaryDns: null,
            SecondaryDns: null,
            new Ipv4AddressCollection([assignment]),
            Ipv4AddressValueCollection.Empty,
            Ipv4AddressValueCollection.Empty);

        return new NetworkAdapterRecoverySnapshot(
            adapter,
            DnsConfigurationMode.Automatic,
            [],
            []);
    }

    private sealed class SequencedRecoveryReader : INetworkAdapterRecoveryReader
    {
        private readonly Queue<NetworkAdapterRecoveryReadResult> _results;

        public SequencedRecoveryReader(params NetworkAdapterRecoverySnapshot[] snapshots) =>
            _results = new Queue<NetworkAdapterRecoveryReadResult>(
                snapshots.Select(NetworkAdapterRecoveryReadResult.Success));

        public Task<NetworkAdapterRecoveryReadResult> ReadAsync(
            NetworkAdapterId adapterId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal(AdapterId, adapterId);
            return Task.FromResult(_results.Dequeue());
        }
    }
}
