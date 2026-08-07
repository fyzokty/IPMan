using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using Microsoft.Extensions.Logging;

namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Read-only adapter discovery. The synchronous Windows read is moved off the
/// calling thread here, inside infrastructure, so no ViewModel needs its own
/// <c>Task.Run</c>.
/// </summary>
public sealed partial class NetworkAdapterReader : INetworkAdapterReader
{
    private readonly IAdapterProbe _probe;
    private readonly ILogger<NetworkAdapterReader> _logger;

    public NetworkAdapterReader(IAdapterProbe probe, ILogger<NetworkAdapterReader> logger)
    {
        ArgumentNullException.ThrowIfNull(probe);
        ArgumentNullException.ThrowIfNull(logger);

        _probe = probe;
        _logger = logger;
    }

    public Task<IReadOnlyList<NetworkAdapterSnapshot>> GetAdaptersAsync(
        CancellationToken cancellationToken) =>
        Task.Run<IReadOnlyList<NetworkAdapterSnapshot>>(
            () => ReadAdapters(cancellationToken),
            cancellationToken);

    public async Task<NetworkAdapterSnapshot?> GetAdapterAsync(
        NetworkAdapterId adapterId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<NetworkAdapterSnapshot> adapters =
            await GetAdaptersAsync(cancellationToken).ConfigureAwait(false);

        return adapters.FirstOrDefault(adapter => adapter.Id == adapterId);
    }

    private List<NetworkAdapterSnapshot> ReadAdapters(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<AdapterReadModel> rawAdapters = _probe.ReadAdapters();
        List<NetworkAdapterSnapshot> snapshots = new(rawAdapters.Count);

        foreach (AdapterReadModel rawAdapter in rawAdapters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!AdapterDiscoveryFilter.IsDiscoverable(rawAdapter))
            {
                continue;
            }

            try
            {
                snapshots.Add(NetworkAdapterMapper.Map(rawAdapter));
            }
            catch (ArgumentException exception)
            {
                // Adapter without a usable Windows identity: skip it instead of
                // failing discovery of every other adapter.
                LogAdapterWithoutIdentity(exception);
            }
        }

        return snapshots;
    }

    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Warning,
        Message = "An adapter reported by Windows had no usable identity and was skipped.")]
    private partial void LogAdapterWithoutIdentity(Exception exception);
}
