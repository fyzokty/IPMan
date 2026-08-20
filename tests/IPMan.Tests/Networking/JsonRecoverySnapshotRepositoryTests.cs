using System.IO;
using System.Text.Json;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class JsonRecoverySnapshotRepositoryTests
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
            JsonRecoverySnapshotRepository repository = CreateRepository(directory);
            RecoverySnapshot snapshot = Snapshot();

            RecoveryCaptureResult result = await repository.SaveAsync(snapshot, CancellationToken.None);

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
            JsonRecoverySnapshotRepository repository = CreateRepository(directory);
            RecoverySnapshot snapshot = Snapshot();
            RecoveryCaptureResult first = await repository.SaveAsync(snapshot, CancellationToken.None);
            string original = await File.ReadAllTextAsync(first.Reference!.StoragePath);

            RecoveryCaptureResult second = await repository.SaveAsync(snapshot, CancellationToken.None);

            Assert.False(second.IsSuccess);
            Assert.Equal(RecoveryCaptureFailure.IoFailure, second.Failure);
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
            JsonRecoverySnapshotRepository repository = CreateRepository(directory);
            RecoverySnapshot snapshot = Snapshot() with
            {
                AdapterId = new NetworkAdapterId("827a2938-bb14-4d18-b67f-94e9c4f818ba")
            };
            RecoveryCaptureResult saved = await repository.SaveAsync(snapshot, CancellationToken.None);

            await using FileStream stream = File.OpenRead(saved.Reference!.StoragePath);
            RecoverySnapshot restored = await new RecoverySnapshotJsonCodec()
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
            RecoveryCaptureResult result = await CreateRepository(invalidDirectory)
                .SaveAsync(Snapshot(), CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(RecoveryCaptureFailure.IoFailure, result.Failure);
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public async Task LoadLatestAsync_WhenMultipleAdaptersExist_SelectsRequestedAdapter()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonRecoverySnapshotRepository repository = CreateRepository(directory);
            RecoverySnapshot requested = Snapshot(
                "requested",
                new NetworkAdapterId("{A}"),
                new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero));
            RecoverySnapshot other = Snapshot(
                "other",
                new NetworkAdapterId("{B}"),
                new DateTimeOffset(2026, 8, 8, 11, 0, 0, TimeSpan.Zero));
            await repository.SaveAsync(requested, CancellationToken.None);
            await repository.SaveAsync(other, CancellationToken.None);

            RecoverySnapshotLoadResult result = await repository.LoadLatestAsync(
                requested.AdapterId,
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(requested.SnapshotId, result.Snapshot!.SnapshotId);
            Assert.Equal(requested.AdapterId, result.Snapshot.AdapterId);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadLatestAsync_WhenAdapterHasThreeSnapshots_SelectsGreatestCapturedAtUtc()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonRecoverySnapshotRepository repository = CreateRepository(directory);
            NetworkAdapterId adapterId = new("{A}");
            RecoverySnapshot newest = Snapshot(
                "00000000-0000-0000-0000-000000000001",
                adapterId,
                new DateTimeOffset(2026, 8, 8, 12, 0, 0, TimeSpan.Zero));
            await repository.SaveAsync(
                Snapshot(
                    "ffffffff-ffff-ffff-ffff-ffffffffffff",
                    adapterId,
                    new DateTimeOffset(2026, 8, 8, 9, 0, 0, TimeSpan.Zero)),
                CancellationToken.None);
            await repository.SaveAsync(newest, CancellationToken.None);
            await repository.SaveAsync(
                Snapshot(
                    "77777777-7777-7777-7777-777777777777",
                    adapterId,
                    new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero)),
                CancellationToken.None);

            RecoverySnapshotLoadResult result = await repository.LoadLatestAsync(
                adapterId,
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(newest.SnapshotId, result.Snapshot!.SnapshotId);
            Assert.Equal(newest.CapturedAtUtc, result.Snapshot.CapturedAtUtc);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadLatestAsync_WhenMalformedFileExists_LoadsHealthyAndPreservesMalformedFile()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonRecoverySnapshotRepository repository = CreateRepository(directory);
            RecoverySnapshot healthy = Snapshot();
            await repository.SaveAsync(healthy, CancellationToken.None);
            string malformedPath = Path.Combine(directory, "recovery-malformed.json");
            await File.WriteAllTextAsync(malformedPath, "{ definitely-not-json");

            RecoverySnapshotLoadResult result = await repository.LoadLatestAsync(
                healthy.AdapterId,
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(healthy.SnapshotId, result.Snapshot!.SnapshotId);
            Assert.True(File.Exists(malformedPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadLatestAsync_WhenSchemaVersionIsUnsupported_ReturnsNotFound()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonRecoverySnapshotRepository repository = CreateRepository(directory);
            RecoverySnapshot unsupported = Snapshot() with { SchemaVersion = 99 };
            await repository.SaveAsync(unsupported, CancellationToken.None);

            RecoverySnapshotLoadResult result = await repository.LoadLatestAsync(
                unsupported.AdapterId,
                CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(RecoverySnapshotLoadStatus.NotFound, result.Status);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadLatestAsync_WhenDirectoryDoesNotExist_ReturnsNotFound()
    {
        string parent = CreateTestDirectory();
        string missing = Path.Combine(parent, "missing");

        try
        {
            RecoverySnapshotLoadResult result = await CreateRepository(missing)
                .LoadLatestAsync(new NetworkAdapterId("{A}"), CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(RecoverySnapshotLoadStatus.NotFound, result.Status);
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public async Task LoadLatestAsync_WhenNoAdapterMatches_ReturnsNotFound()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonRecoverySnapshotRepository repository = CreateRepository(directory);
            await repository.SaveAsync(Snapshot(), CancellationToken.None);

            RecoverySnapshotLoadResult result = await repository.LoadLatestAsync(
                new NetworkAdapterId("{B}"),
                CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(RecoverySnapshotLoadStatus.NotFound, result.Status);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenRetentionExceeded_DeletesOldestAndPreservesNewest()
    {
        string directory = CreateTestDirectory();

        try
        {
            const int retentionLimit = 3;
            JsonRecoverySnapshotRepository repository = CreateRepository(directory, retentionLimit);
            NetworkAdapterId adapterId = new("{A}");
            for (int index = 0; index < retentionLimit + 3; index++)
            {
                RecoveryCaptureResult save = await repository.SaveAsync(
                    Snapshot(
                        $"snapshot-{index}",
                        adapterId,
                        new DateTimeOffset(2026, 8, 8, 9 + index, 0, 0, TimeSpan.Zero)),
                    CancellationToken.None);
                Assert.True(save.IsSuccess);
            }

            string[] fileNames = Directory.GetFiles(directory, "recovery-*.json")
                .Select(Path.GetFileName)
                .ToArray()!;

            Assert.Equal(retentionLimit, fileNames.Length);
            Assert.Contains("recovery-snapshot-5.json", fileNames);
            Assert.Contains("recovery-snapshot-4.json", fileNames);
            Assert.Contains("recovery-snapshot-3.json", fileNames);
            Assert.DoesNotContain("recovery-snapshot-0.json", fileNames);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenMalformedFileExists_RetentionPreservesMalformedFile()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonRecoverySnapshotRepository repository = CreateRepository(directory, retentionLimit: 1);
            string malformedPath = Path.Combine(directory, "recovery-malformed.json");
            await File.WriteAllTextAsync(malformedPath, "not-json");
            await repository.SaveAsync(
                Snapshot("old", capturedAtUtc: new DateTimeOffset(2026, 8, 8, 9, 0, 0, TimeSpan.Zero)),
                CancellationToken.None);
            await repository.SaveAsync(
                Snapshot("new", capturedAtUtc: new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero)),
                CancellationToken.None);

            Assert.True(File.Exists(malformedPath));
            Assert.True(File.Exists(Path.Combine(directory, "recovery-new.json")));
            Assert.False(File.Exists(Path.Combine(directory, "recovery-old.json")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenRetentionDeleteFails_ReturnsSuccess()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonRecoverySnapshotRepository repository = CreateRepository(directory, retentionLimit: 1);
            RecoveryCaptureResult oldSave = await repository.SaveAsync(
                Snapshot("old", capturedAtUtc: new DateTimeOffset(2026, 8, 8, 9, 0, 0, TimeSpan.Zero)),
                CancellationToken.None);
            await using FileStream deletionBlocker = new(
                oldSave.Reference!.StoragePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            RecoveryCaptureResult newSave = await repository.SaveAsync(
                Snapshot("new", capturedAtUtc: new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero)),
                CancellationToken.None);

            Assert.True(newSave.IsSuccess);
            Assert.True(File.Exists(newSave.Reference!.StoragePath));
            Assert.True(File.Exists(oldSave.Reference.StoragePath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static JsonRecoverySnapshotRepository CreateRepository(
        string directory,
        int retentionLimit = 10) =>
        new(new RecoverySnapshotRepositoryOptions
        {
            BackupDirectory = directory,
            RetentionLimitPerAdapter = retentionLimit
        });

    private static RecoverySnapshot Snapshot(
        string snapshotId = "snapshot-1",
        NetworkAdapterId? adapterId = null,
        DateTimeOffset? capturedAtUtc = null) =>
        new(
            2,
            snapshotId,
            capturedAtUtc ?? new DateTimeOffset(2026, 8, 8, 9, 30, 0, TimeSpan.Zero),
            RecoverySnapshotState.Captured,
            adapterId ?? new NetworkAdapterId("{A}"),
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
