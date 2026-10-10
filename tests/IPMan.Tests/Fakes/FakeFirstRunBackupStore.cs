using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeFirstRunBackupStore : IFirstRunBackupStore
{
    public bool HasBackup { get; set; }
    public Exception? ExistsFailure { get; set; }
    public Exception? SaveFailure { get; set; }
    public int SaveCount { get; private set; }
    public FirstRunBackup? SavedBackup { get; private set; }

    public bool Exists()
    {
        if (ExistsFailure is not null)
        {
            throw ExistsFailure;
        }

        return HasBackup;
    }

    public Task SaveAsync(FirstRunBackup backup, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SaveCount++;
        if (SaveFailure is not null)
        {
            throw SaveFailure;
        }

        SavedBackup = backup;
        HasBackup = true;
        return Task.CompletedTask;
    }
}
