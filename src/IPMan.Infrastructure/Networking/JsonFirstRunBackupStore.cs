using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Common;

namespace IPMan.Infrastructure.Networking;

/// <summary>Atomically creates the first-run document without ever replacing it.</summary>
public sealed class JsonFirstRunBackupStore : IFirstRunBackupStore
{
    private readonly string _filePath;
    private readonly RecoverySnapshotJsonCodec _codec = new();

    public JsonFirstRunBackupStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = Path.GetFullPath(filePath);
    }

    public bool Exists() => File.Exists(_filePath);

    public Task SaveAsync(FirstRunBackup backup, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(backup);
        return AtomicJsonFileWriter.WriteAsync(
            _filePath,
            overwrite: false,
            (stream, token) => _codec.SerializeFirstRunBackupAsync(stream, backup, token),
            cancellationToken);
    }
}
