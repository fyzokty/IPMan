using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>
/// Restores a captured configuration by validating current adapter identity and
/// delegating mutation to the existing static or DHCP apply transaction.
/// </summary>
public sealed class RecoveryRestoreService : IRecoveryRestoreService
{
    private readonly IRecoverySnapshotRepository _repository;
    private readonly INetworkAdapterRecoveryReader _recoveryReader;
    private readonly IStaticIpv4ApplyService _staticApplyService;
    private readonly IDhcpApplyService _dhcpApplyService;

    /// <summary>Initializes the recovery restore orchestrator.</summary>
    public RecoveryRestoreService(
        IRecoverySnapshotRepository repository,
        INetworkAdapterRecoveryReader recoveryReader,
        IStaticIpv4ApplyService staticApplyService,
        IDhcpApplyService dhcpApplyService)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(recoveryReader);
        ArgumentNullException.ThrowIfNull(staticApplyService);
        ArgumentNullException.ThrowIfNull(dhcpApplyService);

        _repository = repository;
        _recoveryReader = recoveryReader;
        _staticApplyService = staticApplyService;
        _dhcpApplyService = dhcpApplyService;
    }

    /// <inheritdoc />
    public async Task<RecoveryRestoreResult> RestoreLatestAsync(
        RecoveryRestoreRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        RecoverySnapshotLoadResult load;
        try
        {
            load = await _repository
                .LoadLatestAsync(request.AdapterId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new RecoveryRestoreResult(RecoveryRestoreStatus.Cancelled);
        }

        if (!load.IsSuccess || load.Snapshot is null)
        {
            return new RecoveryRestoreResult(MapLoadFailure(load.Status));
        }

        RecoverySnapshot snapshot = load.Snapshot;
        if (snapshot.SchemaVersion != RecoverySnapshotLoadResult.SupportedSchemaVersion)
        {
            return new RecoveryRestoreResult(
                RecoveryRestoreStatus.UnsupportedSnapshot,
                snapshot);
        }

        NetworkAdapterRecoveryReadResult currentRead;
        try
        {
            currentRead = await _recoveryReader
                .ReadAsync(request.AdapterId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new RecoveryRestoreResult(RecoveryRestoreStatus.Cancelled, snapshot);
        }

        if (currentRead.Status != NetworkAdapterRecoveryReadStatus.Success ||
            currentRead.Snapshot is null)
        {
            RecoveryRestoreStatus readStatus = currentRead.Status ==
                NetworkAdapterRecoveryReadStatus.AdapterUnavailable
                    ? RecoveryRestoreStatus.AdapterUnavailable
                    : RecoveryRestoreStatus.AdapterReadFailed;
            return new RecoveryRestoreResult(readStatus, snapshot);
        }

        if (currentRead.Snapshot.Adapter.Id != request.AdapterId ||
            currentRead.Snapshot.Adapter.Id != snapshot.AdapterId)
        {
            return new RecoveryRestoreResult(
                RecoveryRestoreStatus.AdapterIdentityChanged,
                snapshot);
        }

        return snapshot.Mode switch
        {
            NetworkConfigurationMode.Dhcp => await RestoreDhcpAsync(
                    request,
                    snapshot,
                    cancellationToken)
                .ConfigureAwait(false),
            NetworkConfigurationMode.Static => await RestoreStaticAsync(
                    request,
                    snapshot,
                    cancellationToken)
                .ConfigureAwait(false),
            _ => new RecoveryRestoreResult(
                RecoveryRestoreStatus.UnsupportedSnapshot,
                snapshot)
        };
    }

    private async Task<RecoveryRestoreResult> RestoreDhcpAsync(
        RecoveryRestoreRequest request,
        RecoverySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        DhcpApplyResult outcome = await _dhcpApplyService
            .ApplyAsync(new DhcpApplyRequest(request.AdapterId), cancellationToken)
            .ConfigureAwait(false);
        return new RecoveryRestoreResult(
            MapDhcpStatus(outcome.Status),
            snapshot,
            outcome.SafetyBlock,
            DhcpOutcome: outcome);
    }

    private async Task<RecoveryRestoreResult> RestoreStaticAsync(
        RecoveryRestoreRequest request,
        RecoverySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        RecoveryRestoreResult? unsupported = ValidateStaticSnapshot(snapshot);
        if (unsupported is not null)
        {
            return unsupported;
        }

        Ipv4AddressAssignment address = snapshot.Ipv4Addresses[0];
        string? gateway = snapshot.Ipv4Gateways.Length == 0
            ? null
            : snapshot.Ipv4Gateways[0].Address;
        string? primaryDns = null;
        string? secondaryDns = null;
        if (snapshot.DnsMode == DnsConfigurationMode.Manual)
        {
            primaryDns = snapshot.ConfiguredIpv4DnsServers.ElementAtOrDefault(0);
            secondaryDns = snapshot.ConfiguredIpv4DnsServers.ElementAtOrDefault(1);
        }

        StaticIpv4Configuration configuration = new(
            address.Address,
            address.SubnetMask!,
            gateway,
            primaryDns,
            secondaryDns);
        StaticIpv4ApplyRequest applyRequest = new(
            request.AdapterId,
            configuration,
            request.ConfirmPotentialConflict,
            request.ContinueAfterIndeterminateProbe);
        StaticIpv4ApplyResult outcome = await _staticApplyService
            .ApplyAsync(applyRequest, cancellationToken)
            .ConfigureAwait(false);
        return new RecoveryRestoreResult(
            MapStaticStatus(outcome.Status),
            snapshot,
            outcome.SafetyBlock,
            StaticOutcome: outcome);
    }

    private static RecoveryRestoreResult? ValidateStaticSnapshot(RecoverySnapshot snapshot)
    {
        if (snapshot.Ipv4Addresses is null || snapshot.Ipv4Addresses.Length == 0)
        {
            return new RecoveryRestoreResult(
                RecoveryRestoreStatus.UnsupportedSnapshot,
                snapshot);
        }

        if (snapshot.Ipv4Addresses.Length > 1)
        {
            return new RecoveryRestoreResult(
                RecoveryRestoreStatus.SafetyBlocked,
                snapshot,
                StaticIpv4SafetyBlock.MultipleIpv4Addresses);
        }

        if (string.IsNullOrWhiteSpace(snapshot.Ipv4Addresses[0].Address) ||
            string.IsNullOrWhiteSpace(snapshot.Ipv4Addresses[0].SubnetMask))
        {
            return new RecoveryRestoreResult(
                RecoveryRestoreStatus.UnsupportedSnapshot,
                snapshot);
        }

        if (snapshot.Ipv4Gateways is null)
        {
            return new RecoveryRestoreResult(
                RecoveryRestoreStatus.UnsupportedSnapshot,
                snapshot);
        }

        if (snapshot.Ipv4Gateways.Length > 1)
        {
            return new RecoveryRestoreResult(
                RecoveryRestoreStatus.SafetyBlocked,
                snapshot,
                StaticIpv4SafetyBlock.MultipleIpv4Gateways);
        }

        if (snapshot.ConfiguredIpv4DnsServers is null)
        {
            return new RecoveryRestoreResult(
                RecoveryRestoreStatus.UnsupportedSnapshot,
                snapshot);
        }

        if (snapshot.DnsMode == DnsConfigurationMode.Manual &&
            snapshot.ConfiguredIpv4DnsServers.Length > 2)
        {
            return new RecoveryRestoreResult(
                RecoveryRestoreStatus.SafetyBlocked,
                snapshot,
                StaticIpv4SafetyBlock.TooManyIpv4DnsServers);
        }

        return snapshot.DnsMode is DnsConfigurationMode.Automatic or DnsConfigurationMode.Manual
            ? null
            : new RecoveryRestoreResult(
                RecoveryRestoreStatus.UnsupportedSnapshot,
                snapshot);
    }

    private static RecoveryRestoreStatus MapLoadFailure(RecoverySnapshotLoadStatus status) =>
        status switch
        {
            RecoverySnapshotLoadStatus.NotFound => RecoveryRestoreStatus.NoSnapshotFound,
            RecoverySnapshotLoadStatus.IoFailure or RecoverySnapshotLoadStatus.AccessDenied =>
                RecoveryRestoreStatus.SnapshotUnreadable,
            RecoverySnapshotLoadStatus.Success => RecoveryRestoreStatus.SnapshotUnreadable,
            _ => throw new InvalidOperationException($"Unsupported recovery load status: {status}.")
        };

    private static RecoveryRestoreStatus MapStaticStatus(StaticIpv4ApplyStatus status) =>
        status switch
        {
            StaticIpv4ApplyStatus.VerifiedSuccess => RecoveryRestoreStatus.VerifiedSuccess,
            StaticIpv4ApplyStatus.NoChange => RecoveryRestoreStatus.NoChange,
            StaticIpv4ApplyStatus.ValidationFailed => RecoveryRestoreStatus.UnsupportedSnapshot,
            StaticIpv4ApplyStatus.AdapterUnavailable => RecoveryRestoreStatus.AdapterUnavailable,
            StaticIpv4ApplyStatus.AdapterReadFailed => RecoveryRestoreStatus.AdapterReadFailed,
            StaticIpv4ApplyStatus.SafetyBlocked => RecoveryRestoreStatus.SafetyBlocked,
            StaticIpv4ApplyStatus.ConflictConfirmationRequired =>
                RecoveryRestoreStatus.ConflictConfirmationRequired,
            StaticIpv4ApplyStatus.ProbeIndeterminateConfirmationRequired =>
                RecoveryRestoreStatus.ProbeIndeterminateConfirmationRequired,
            StaticIpv4ApplyStatus.RecoveryCaptureFailed => RecoveryRestoreStatus.RecoveryCaptureFailed,
            StaticIpv4ApplyStatus.RecoveryStateUnavailable => RecoveryRestoreStatus.RecoveryStateUnavailable,
            StaticIpv4ApplyStatus.MutationFailed => RecoveryRestoreStatus.MutationFailed,
            StaticIpv4ApplyStatus.PartialFailure => RecoveryRestoreStatus.PartialFailure,
            StaticIpv4ApplyStatus.VerificationFailed => RecoveryRestoreStatus.VerificationFailed,
            StaticIpv4ApplyStatus.AdapterUnavailableDuringVerification =>
                RecoveryRestoreStatus.AdapterUnavailableDuringVerification,
            StaticIpv4ApplyStatus.Cancelled => RecoveryRestoreStatus.Cancelled,
            _ => throw new InvalidOperationException($"Unsupported static apply status: {status}.")
        };

    private static RecoveryRestoreStatus MapDhcpStatus(DhcpApplyStatus status) =>
        status switch
        {
            DhcpApplyStatus.VerifiedSuccess => RecoveryRestoreStatus.VerifiedSuccess,
            DhcpApplyStatus.NoChange => RecoveryRestoreStatus.NoChange,
            DhcpApplyStatus.AdapterUnavailable => RecoveryRestoreStatus.AdapterUnavailable,
            DhcpApplyStatus.AdapterReadFailed => RecoveryRestoreStatus.AdapterReadFailed,
            DhcpApplyStatus.SafetyBlocked => RecoveryRestoreStatus.SafetyBlocked,
            DhcpApplyStatus.RecoveryCaptureFailed => RecoveryRestoreStatus.RecoveryCaptureFailed,
            DhcpApplyStatus.RecoveryStateUnavailable => RecoveryRestoreStatus.RecoveryStateUnavailable,
            DhcpApplyStatus.MutationFailed => RecoveryRestoreStatus.MutationFailed,
            DhcpApplyStatus.PartialFailure => RecoveryRestoreStatus.PartialFailure,
            DhcpApplyStatus.VerificationFailed => RecoveryRestoreStatus.VerificationFailed,
            DhcpApplyStatus.AdapterUnavailableDuringVerification =>
                RecoveryRestoreStatus.AdapterUnavailableDuringVerification,
            DhcpApplyStatus.Cancelled => RecoveryRestoreStatus.Cancelled,
            _ => throw new InvalidOperationException($"Unsupported DHCP apply status: {status}.")
        };
}
