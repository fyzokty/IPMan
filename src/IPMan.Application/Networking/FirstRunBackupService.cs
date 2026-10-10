using IPMan.Application.Common;
using IPMan.Application.Logging;
using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>Captures initial configuration without mutations or user notifications.</summary>
public sealed class FirstRunBackupService
{
    private readonly INetworkAdapterReader _reader;
    private readonly INetworkAdapterRecoveryReader _recoveryReader;
    private readonly IFirstRunBackupStore _store;
    private readonly IClock _clock;
    private readonly ICriticalLogger _logger;

    public FirstRunBackupService(
        INetworkAdapterReader reader,
        INetworkAdapterRecoveryReader recoveryReader,
        IFirstRunBackupStore store,
        IClock clock,
        ICriticalLogger logger)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(recoveryReader);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);
        _reader = reader;
        _recoveryReader = recoveryReader;
        _store = store;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Attempts once per startup; absence of a completed file permits retry.</summary>
    public async Task CaptureAsync(bool showVirtualAdapters, CancellationToken cancellationToken)
    {
        try
        {
            if (_store.Exists())
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<NetworkAdapterSnapshot> discovered = await _reader
                .GetAdaptersAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyList<NetworkAdapterSnapshot> visible = AdapterVisibilityFilter
                .SelectVisible(discovered, showVirtualAdapters);
            DateTimeOffset capturedAtUtc = _clock.UtcNow.ToUniversalTime();
            List<RecoverySnapshot> snapshots = new();

            foreach (NetworkAdapterSnapshot adapter in visible)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    NetworkAdapterRecoveryReadResult result = await _recoveryReader
                        .ReadAsync(adapter.Id, cancellationToken).ConfigureAwait(false);
                    if (result.Status != NetworkAdapterRecoveryReadStatus.Success ||
                        result.Snapshot is null || result.Snapshot.Adapter.Id != adapter.Id)
                    {
                        LogFailure($"First-run backup could not read adapter {adapter.Id}: {result.Status}.");
                        continue;
                    }

                    snapshots.Add(CreateSnapshot(result.Snapshot, capturedAtUtc));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    LogFailure($"First-run backup could not read adapter {adapter.Id}.", exception);
                }
            }

            if (snapshots.Count > 0)
            {
                await _store.SaveAsync(
                    new FirstRunBackup(FirstRunBackup.CurrentSchemaVersion, capturedAtUtc, snapshots.ToArray()),
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A canceled or incomplete capture leaves no marker and can be retried.
        }
        catch (Exception exception)
        {
            LogFailure("First-run configuration backup failed.", exception);
        }
    }

    private static RecoverySnapshot CreateSnapshot(
        NetworkAdapterRecoverySnapshot recovery,
        DateTimeOffset capturedAtUtc) =>
        new(
            SchemaVersion: RecoverySnapshotLoadResult.SupportedSchemaVersion,
            SnapshotId: Guid.NewGuid().ToString("N"),
            CapturedAtUtc: capturedAtUtc,
            State: RecoverySnapshotState.Captured,
            AdapterId: recovery.Adapter.Id,
            AdapterName: recovery.Adapter.Name,
            AdapterDescription: recovery.Adapter.Description,
            Mode: recovery.Adapter.Mode,
            Ipv4Addresses: recovery.Adapter.Ipv4Addresses.ToArray(),
            Ipv4Gateways: recovery.Ipv4Gateways.ToArray(),
            DnsMode: recovery.DnsMode,
            ConfiguredIpv4DnsServers: recovery.ConfiguredIpv4DnsServers.ToArray(),
            Ipv4DnsServers: recovery.Adapter.Ipv4DnsServers.ToArray());

    private void LogFailure(string message, Exception? exception = null)
    {
        try
        {
            _logger.Log(new(CriticalLogCategory.FirstRunBackup, message, exception?.HResult, exception));
        }
        catch (Exception)
        {
            // Even a failing diagnostic sink must not escape into startup.
        }
    }
}
