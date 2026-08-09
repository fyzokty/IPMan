using System.IO;
using System.Text.Json;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class JsonRollbackSnapshotRepositoryTests
{
    private static readonly Ipv4GatewayRecoveryState[] SnapshotGateways =
    {
        new("192.168.1.1", 25)
    };

    private static readonly string[] SnapshotDnsServers = { "1.1.1.1", "8.8.8.8" };

    [Fact]
    public async Task SaveAsync_WhenStorageIsAvailable_WritesCompleteAtomicJsonSnapshot()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonRollbackSnapshotRepository repository = CreateRepository(directory);
            NetworkRollbackSnapshot snapshot = Snapshot();

            RollbackCaptureResult result = await repository.SaveAsync(snapshot, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.True(File.Exists(result.Reference!.StoragePath));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));

            using JsonDocument document = JsonDocument.Parse(
                await File.ReadAllTextAsync(result.Reference.StoragePath));
            JsonElement root = document.RootElement;
            Assert.Equal(2, root.GetProperty("schemaVersion").GetInt32());
            Assert.Equal("snapshot-1", root.GetProperty("snapshotId").GetString());
            Assert.Equal("{A}", root.GetProperty("adapterId").GetProperty("value").GetString());
            Assert.Equal(2, root.GetProperty("ipv4Addresses").GetArrayLength());
            Assert.Equal("192.168.1.1", root.GetProperty("ipv4Gateways")[0].GetProperty("address").GetString());
            Assert.Equal(25, root.GetProperty("ipv4Gateways")[0].GetProperty("metric").GetInt32());
            Assert.Equal((int)DnsConfigurationMode.Manual, root.GetProperty("dnsMode").GetInt32());
            Assert.Equal("1.1.1.1", root.GetProperty("configuredIpv4DnsServers")[0].GetString());
            Assert.Equal("8.8.8.8", root.GetProperty("ipv4DnsServers")[1].GetString());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenFinalFileAlreadyExists_ReturnsIoFailureWithoutOverwriting()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonRollbackSnapshotRepository repository = CreateRepository(directory);
            NetworkRollbackSnapshot snapshot = Snapshot();
            RollbackCaptureResult first = await repository.SaveAsync(snapshot, CancellationToken.None);
            string original = await File.ReadAllTextAsync(first.Reference!.StoragePath);

            RollbackCaptureResult second = await repository.SaveAsync(snapshot, CancellationToken.None);

            Assert.False(second.IsSuccess);
            Assert.Equal(RollbackCaptureFailure.IoFailure, second.Failure);
            Assert.Equal(original, await File.ReadAllTextAsync(first.Reference.StoragePath));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenSnapshotIsReadWithAuthoritativeCodec_PreservesCanonicalAdapterIdentity()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonRollbackSnapshotRepository repository = CreateRepository(directory);
            NetworkRollbackSnapshot snapshot = Snapshot() with
            {
                AdapterId = new NetworkAdapterId("827a2938-bb14-4d18-b67f-94e9c4f818ba")
            };
            RollbackCaptureResult saved = await repository.SaveAsync(snapshot, CancellationToken.None);

            await using FileStream stream = File.OpenRead(saved.Reference!.StoragePath);
            NetworkRollbackSnapshot restored = await new RollbackSnapshotJsonCodec()
                .DeserializeAsync(stream, CancellationToken.None);

            Assert.Equal(snapshot.AdapterId, restored.AdapterId);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenBackupPathIsAFile_ReturnsIoFailure()
    {
        string parent = CreateTestDirectory();
        string invalidDirectory = Path.Combine(parent, "not-a-directory");
        await File.WriteAllTextAsync(invalidDirectory, "occupied");

        try
        {
            RollbackCaptureResult result = await CreateRepository(invalidDirectory)
                .SaveAsync(Snapshot(), CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(RollbackCaptureFailure.IoFailure, result.Failure);
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    private static JsonRollbackSnapshotRepository CreateRepository(string directory) =>
        new(new RollbackSnapshotRepositoryOptions { BackupDirectory = directory });

    private static NetworkRollbackSnapshot Snapshot() =>
        new(
            2,
            "snapshot-1",
            new DateTimeOffset(2026, 8, 8, 9, 30, 0, TimeSpan.Zero),
            RollbackSnapshotState.Captured,
            new NetworkAdapterId("{A}"),
            "Ethernet",
            "Contoso Adapter",
            NetworkConfigurationMode.Static,
            new[]
            {
                new Ipv4AddressAssignment("192.168.1.50", "255.255.255.0"),
                new Ipv4AddressAssignment("10.0.0.2", "255.255.255.0")
            },
            SnapshotGateways,
            DnsConfigurationMode.Manual,
            SnapshotDnsServers,
            SnapshotDnsServers);

    private static string CreateTestDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"IPMan-Sprint07-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
