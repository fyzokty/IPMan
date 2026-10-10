using System.IO;
using System.Text.Json;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class JsonFirstRunBackupStoreTests
{
    [Fact]
    public async Task SaveAsync_WhenDirectoryMissing_CreatesCompleteFileAndNoTemporaryFiles()
    {
        string parent = CreateTemporaryDirectory();
        string path = Path.Combine(parent, "FirstRun", "first-run.json");
        try
        {
            JsonFirstRunBackupStore store = new(path);
            Assert.False(store.Exists());

            await store.SaveAsync(Backup(), CancellationToken.None);

            Assert.True(store.Exists());
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp"));
            await using FileStream stream = File.OpenRead(path);
            FirstRunBackup backup = await new RecoverySnapshotJsonCodec()
                .DeserializeFirstRunBackupAsync(stream, CancellationToken.None);
            Assert.Equal(Backup().CapturedAtUtc, backup.CapturedAtUtc);
            Assert.Equal(Backup().Adapters[0].AdapterId, Assert.Single(backup.Adapters).AdapterId);
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenDestinationExists_NeverOverwritesEvenMalformedFile()
    {
        string parent = CreateTemporaryDirectory();
        string path = Path.Combine(parent, "first-run.json");
        try
        {
            await File.WriteAllTextAsync(path, "original copy");
            JsonFirstRunBackupStore store = new(path);
            Assert.True(store.Exists());

            await Assert.ThrowsAsync<IOException>(() => store.SaveAsync(Backup(), CancellationToken.None));

            Assert.Equal("original copy", await File.ReadAllTextAsync(path));
            Assert.Empty(Directory.GetFiles(parent, "*.tmp"));
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenSerializationFails_LeavesNoCompletionMarkerAndAllowsRetry()
    {
        string parent = CreateTemporaryDirectory();
        string path = Path.Combine(parent, "FirstRun", "first-run.json");
        try
        {
            JsonFirstRunBackupStore store = new(path);
            FirstRunBackup invalid = Backup() with
            {
                Adapters = new[] { Backup().Adapters[0] with { AdapterId = default } }
            };

            await Assert.ThrowsAsync<JsonException>(() => store.SaveAsync(invalid, CancellationToken.None));

            Assert.False(store.Exists());
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp"));
            await store.SaveAsync(Backup(), CancellationToken.None);
            Assert.True(store.Exists());
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenDirectoryIsAFile_LeavesNoCompletionMarker()
    {
        string parent = CreateTemporaryDirectory();
        string blockingPath = Path.Combine(parent, "FirstRun");
        try
        {
            await File.WriteAllTextAsync(blockingPath, "occupied");
            JsonFirstRunBackupStore store = new(Path.Combine(blockingPath, "first-run.json"));

            await Assert.ThrowsAsync<IOException>(() => store.SaveAsync(Backup(), CancellationToken.None));

            Assert.False(store.Exists());
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public async Task RecoveryRepository_WhenFirstRunCopyExists_NeverLoadsOrPrunesIt()
    {
        string parent = CreateTemporaryDirectory();
        string path = Path.Combine(parent, "FirstRun", "first-run.json");
        string backupDirectory = Path.Combine(parent, "Backup");
        try
        {
            await new JsonFirstRunBackupStore(path).SaveAsync(Backup(), CancellationToken.None);
            byte[] original = await File.ReadAllBytesAsync(path);
            JsonRecoverySnapshotRepository repository = new(new RecoverySnapshotRepositoryOptions
            {
                BackupDirectory = backupDirectory,
                RetentionLimitPerAdapter = 1
            });
            RecoverySnapshot snapshot = Backup().Adapters[0];

            RecoverySnapshotLoadResult absent = await repository.LoadLatestAsync(snapshot.AdapterId, CancellationToken.None);
            Assert.Equal(RecoverySnapshotLoadStatus.NotFound, absent.Status);

            RecoverySnapshot older = snapshot with { SnapshotId = "old", CapturedAtUtc = snapshot.CapturedAtUtc.AddDays(-2) };
            RecoverySnapshot newer = snapshot with { SnapshotId = "new", CapturedAtUtc = snapshot.CapturedAtUtc.AddDays(-1) };
            Assert.True((await repository.SaveAsync(older, CancellationToken.None)).IsSuccess);
            Assert.True((await repository.SaveAsync(newer, CancellationToken.None)).IsSuccess);
            RecoverySnapshotLoadResult latest = await repository.LoadLatestAsync(snapshot.AdapterId, CancellationToken.None);

            Assert.Equal("new", latest.Snapshot!.SnapshotId);
            Assert.Single(Directory.GetFiles(backupDirectory, "recovery-*.json"));
            Assert.Equal(original, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    private static FirstRunBackup Backup()
    {
        DateTimeOffset capturedAt = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        return new FirstRunBackup(FirstRunBackup.CurrentSchemaVersion, capturedAt, new[]
        {
            new RecoverySnapshot(2, "initial", capturedAt, RecoverySnapshotState.Captured,
                new NetworkAdapterId("{A}"), "Ethernet", "Contoso Adapter", NetworkConfigurationMode.Dhcp,
                Array.Empty<Ipv4AddressAssignment>(), Array.Empty<Ipv4GatewayRecoveryState>(),
                DnsConfigurationMode.Automatic, Array.Empty<string>(), Array.Empty<string>())
        });
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "IPMan.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
