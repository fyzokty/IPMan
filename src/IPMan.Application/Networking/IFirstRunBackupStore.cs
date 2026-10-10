using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>Stores the first-run copy independently of pre-mutation recovery data.</summary>
public interface IFirstRunBackupStore
{
    bool Exists();

    Task SaveAsync(FirstRunBackup backup, CancellationToken cancellationToken);
}
