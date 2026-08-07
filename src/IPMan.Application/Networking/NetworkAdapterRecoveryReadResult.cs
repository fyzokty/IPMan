using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public sealed record NetworkAdapterRecoveryReadResult(
    NetworkAdapterRecoveryReadStatus Status,
    NetworkAdapterRecoverySnapshot? Snapshot = null)
{
    public static NetworkAdapterRecoveryReadResult Success(NetworkAdapterRecoverySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new(NetworkAdapterRecoveryReadStatus.Success, snapshot);
    }
}
