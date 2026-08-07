namespace IPMan.Application.Networking;

/// <summary>
/// Shared process-wide gate for network mutations that must never overlap.
/// </summary>
public interface INetworkMutationCoordinator
{
    Task<IDisposable> AcquireAsync(CancellationToken cancellationToken);
}
