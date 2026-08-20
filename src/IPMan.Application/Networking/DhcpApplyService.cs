using System.Net.NetworkInformation;
using IPMan.Application.Common;
using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>
/// Owns the complete DHCP apply transaction: fresh reads, safety gates,
/// recovery capture, one serialized mutation and bounded verification.
/// </summary>
public sealed class DhcpApplyService : IDhcpApplyService
{
    private readonly IRecoverySnapshotRepository _recoveryRepository;
    private readonly INetworkAdapterConfigurator _configurator;
    private readonly INetworkAdapterReader _adapterReader;
    private readonly INetworkAdapterRecoveryReader _recoveryReader;
    private readonly INetworkMutationCoordinator _mutationCoordinator;
    private readonly IDelayProvider _delayProvider;
    private readonly IClock _clock;
    private readonly IElevationStateProvider _elevationStateProvider;
    private readonly StaticIpv4ApplyOptions _options;

    /// <summary>Creates the DHCP transaction coordinator.</summary>
    /// <param name="recoveryRepository">Persists the pre-mutation recovery state.</param>
    /// <param name="configurator">Executes the low-level DHCP mutation.</param>
    /// <param name="adapterReader">Reads fresh effective adapter state.</param>
    /// <param name="recoveryReader">Reads exact state needed for recovery and DNS verification.</param>
    /// <param name="mutationCoordinator">Serializes machine-wide network mutations.</param>
    /// <param name="delayProvider">Provides bounded reconciliation delays.</param>
    /// <param name="clock">Supplies the recovery capture time.</param>
    /// <param name="elevationStateProvider">Reports whether mutation is permitted.</param>
    /// <param name="options">Controls bounded verification attempts and delay.</param>
    public DhcpApplyService(
        IRecoverySnapshotRepository recoveryRepository,
        INetworkAdapterConfigurator configurator,
        INetworkAdapterReader adapterReader,
        INetworkAdapterRecoveryReader recoveryReader,
        INetworkMutationCoordinator mutationCoordinator,
        IDelayProvider delayProvider,
        IClock clock,
        IElevationStateProvider elevationStateProvider,
        StaticIpv4ApplyOptions options)
    {
        ArgumentNullException.ThrowIfNull(recoveryRepository);
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(adapterReader);
        ArgumentNullException.ThrowIfNull(recoveryReader);
        ArgumentNullException.ThrowIfNull(mutationCoordinator);
        ArgumentNullException.ThrowIfNull(delayProvider);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(elevationStateProvider);
        ArgumentNullException.ThrowIfNull(options);

        if (options.VerificationAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "At least one verification attempt is required.");
        }

        if (options.VerificationDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Verification delay cannot be negative.");
        }

        _recoveryRepository = recoveryRepository;
        _configurator = configurator;
        _adapterReader = adapterReader;
        _recoveryReader = recoveryReader;
        _mutationCoordinator = mutationCoordinator;
        _delayProvider = delayProvider;
        _clock = clock;
        _elevationStateProvider = elevationStateProvider;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<DhcpApplyResult> ApplyAsync(
        DhcpApplyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_elevationStateProvider.IsElevated)
        {
            return new DhcpApplyResult(
                DhcpApplyStatus.SafetyBlocked,
                SafetyBlock: StaticIpv4SafetyBlock.NotElevated);
        }

        IDisposable lease;

        try
        {
            lease = await _mutationCoordinator.AcquireAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new DhcpApplyResult(DhcpApplyStatus.Cancelled);
        }

        using (lease)
        {
            return await ApplySerializedAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<DhcpApplyResult> ApplySerializedAsync(
        DhcpApplyRequest request,
        CancellationToken cancellationToken)
    {
        InitialAdapterRead initialRead = await ReadInitialStateAsync(
                request.AdapterId,
                cancellationToken)
            .ConfigureAwait(false);

        if (initialRead.Cancelled)
        {
            return new DhcpApplyResult(DhcpApplyStatus.Cancelled);
        }

        if (initialRead.Failed)
        {
            return new DhcpApplyResult(DhcpApplyStatus.AdapterReadFailed);
        }

        if (initialRead.Snapshot is null)
        {
            return new DhcpApplyResult(DhcpApplyStatus.AdapterUnavailable);
        }

        NetworkAdapterRecoveryReadResult recoveryRead;

        try
        {
            recoveryRead = await _recoveryReader
                .ReadAsync(request.AdapterId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new DhcpApplyResult(DhcpApplyStatus.Cancelled);
        }

        if (recoveryRead.Status != NetworkAdapterRecoveryReadStatus.Success ||
            recoveryRead.Snapshot is null)
        {
            return new DhcpApplyResult(
                recoveryRead.Status == NetworkAdapterRecoveryReadStatus.AdapterUnavailable
                    ? DhcpApplyStatus.AdapterUnavailable
                    : DhcpApplyStatus.AdapterReadFailed);
        }

        NetworkAdapterRecoverySnapshot recovery = recoveryRead.Snapshot;

        if (recovery.Adapter.Id != request.AdapterId)
        {
            return new DhcpApplyResult(DhcpApplyStatus.AdapterReadFailed);
        }

        StaticIpv4SafetyBlock safetyBlock = GetSafetyBlock(recovery.Adapter);

        if (safetyBlock != StaticIpv4SafetyBlock.None)
        {
            return new DhcpApplyResult(
                DhcpApplyStatus.SafetyBlocked,
                SafetyBlock: safetyBlock);
        }

        if (recovery.Adapter.Mode == NetworkConfigurationMode.Dhcp &&
            recovery.DnsMode == DnsConfigurationMode.Automatic)
        {
            return new DhcpApplyResult(DhcpApplyStatus.NoChange);
        }

        if (!recovery.IsRestoreCapable)
        {
            return new DhcpApplyResult(DhcpApplyStatus.RecoveryStateUnavailable);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new DhcpApplyResult(DhcpApplyStatus.Cancelled);
        }

        RecoveryCaptureResult capture;

        try
        {
            capture = await _recoveryRepository
                .SaveAsync(CreateRecoverySnapshot(recovery), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new DhcpApplyResult(DhcpApplyStatus.Cancelled);
        }

        if (!capture.IsSuccess)
        {
            return new DhcpApplyResult(DhcpApplyStatus.RecoveryCaptureFailed);
        }

        RecoverySnapshotReference recoveryReference = capture.Reference!;

        if (cancellationToken.IsCancellationRequested)
        {
            return new DhcpApplyResult(
                DhcpApplyStatus.Cancelled,
                Recovery: recoveryReference);
        }

        DhcpMutationPlan plan = new(
            recovery.Adapter.Mode,
            ReturnDnsToAutomatic: true);

        NetworkApplyResult mutation = await _configurator
            .ApplyDhcpAsync(request.AdapterId, plan, CancellationToken.None)
            .ConfigureAwait(false);

        if (!DhcpMutationSucceeded(mutation))
        {
            PostMutationRead read = mutation.WasMutationAttempted
                ? await ReadAfterMutationAsync(request.AdapterId).ConfigureAwait(false)
                : new PostMutationRead();

            return new DhcpApplyResult(
                DhcpMutationIsPartialFailure(mutation)
                    ? DhcpApplyStatus.PartialFailure
                    : DhcpApplyStatus.MutationFailed,
                Recovery: recoveryReference,
                Mutation: mutation,
                ActualSnapshot: read.Snapshot);
        }

        return await VerifyAsync(
                request.AdapterId,
                recoveryReference,
                mutation)
            .ConfigureAwait(false);
    }

    private async Task<DhcpApplyResult> VerifyAsync(
        NetworkAdapterId adapterId,
        RecoverySnapshotReference recoveryReference,
        NetworkApplyResult mutation)
    {
        NetworkAdapterSnapshot? lastSnapshot = null;

        for (int attempt = 0; attempt < _options.VerificationAttempts; attempt++)
        {
            PostMutationRead read = await ReadAfterMutationAsync(adapterId).ConfigureAwait(false);

            if (read.Failed)
            {
                return new DhcpApplyResult(
                    DhcpApplyStatus.VerificationFailed,
                    Recovery: recoveryReference,
                    Mutation: mutation);
            }

            NetworkAdapterSnapshot? actual = read.Snapshot;

            if (actual is null)
            {
                return new DhcpApplyResult(
                    DhcpApplyStatus.AdapterUnavailableDuringVerification,
                    Recovery: recoveryReference,
                    Mutation: mutation);
            }

            lastSnapshot = actual;
            NetworkAdapterRecoveryReadResult recoveryRead = await _recoveryReader
                .ReadAsync(adapterId, CancellationToken.None)
                .ConfigureAwait(false);

            if (recoveryRead.Status != NetworkAdapterRecoveryReadStatus.Success ||
                recoveryRead.Snapshot is null ||
                recoveryRead.Snapshot.Adapter.Id != adapterId)
            {
                return new DhcpApplyResult(
                    DhcpApplyStatus.VerificationFailed,
                    Recovery: recoveryReference,
                    Mutation: mutation,
                    ActualSnapshot: actual);
            }

            if (actual.Mode == NetworkConfigurationMode.Dhcp &&
                recoveryRead.Snapshot.DnsMode == DnsConfigurationMode.Automatic)
            {
                return new DhcpApplyResult(
                    DhcpApplyStatus.VerifiedSuccess,
                    Recovery: recoveryReference,
                    Mutation: mutation,
                    ActualSnapshot: actual);
            }

            if (attempt + 1 < _options.VerificationAttempts)
            {
                await _delayProvider
                    .DelayAsync(_options.VerificationDelay, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        return new DhcpApplyResult(
            DhcpApplyStatus.VerificationFailed,
            Recovery: recoveryReference,
            Mutation: mutation,
            ActualSnapshot: lastSnapshot);
    }

    private async Task<InitialAdapterRead> ReadInitialStateAsync(
        NetworkAdapterId adapterId,
        CancellationToken cancellationToken)
    {
        try
        {
            NetworkAdapterSnapshot? snapshot = await _adapterReader
                .GetAdapterAsync(adapterId, cancellationToken)
                .ConfigureAwait(false);
            return new InitialAdapterRead(snapshot);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new InitialAdapterRead(Cancelled: true);
        }
        catch (NetworkInformationException)
        {
            return new InitialAdapterRead(Failed: true);
        }
        catch (PlatformNotSupportedException)
        {
            return new InitialAdapterRead(Failed: true);
        }
    }

    private async Task<PostMutationRead> ReadAfterMutationAsync(NetworkAdapterId adapterId)
    {
        try
        {
            NetworkAdapterSnapshot? snapshot = await _adapterReader
                .GetAdapterAsync(adapterId, CancellationToken.None)
                .ConfigureAwait(false);
            return new PostMutationRead(snapshot);
        }
        catch (NetworkInformationException)
        {
            return new PostMutationRead(Failed: true);
        }
        catch (PlatformNotSupportedException)
        {
            return new PostMutationRead(Failed: true);
        }
    }

    private static bool DhcpMutationSucceeded(NetworkApplyResult mutation) =>
        mutation.FailureKind == NetworkMutationFailureKind.None &&
        mutation.Ipv4Step.IsSuccessful &&
        mutation.DnsStep.IsSuccessful;

    private static bool DhcpMutationIsPartialFailure(NetworkApplyResult mutation) =>
        mutation.Ipv4Step.IsSuccessful && !mutation.DnsStep.IsSuccessful;

    private static StaticIpv4SafetyBlock GetSafetyBlock(NetworkAdapterSnapshot snapshot)
    {
        if (snapshot.Ipv4Addresses.Count > 1)
        {
            return StaticIpv4SafetyBlock.MultipleIpv4Addresses;
        }

        if (snapshot.Ipv4Gateways.Count > 1)
        {
            return StaticIpv4SafetyBlock.MultipleIpv4Gateways;
        }

        return snapshot.Ipv4DnsServers.Count > 2
            ? StaticIpv4SafetyBlock.TooManyIpv4DnsServers
            : StaticIpv4SafetyBlock.None;
    }

    private RecoverySnapshot CreateRecoverySnapshot(NetworkAdapterRecoverySnapshot recovery) =>
        new(
            SchemaVersion: 2,
            SnapshotId: Guid.NewGuid().ToString("N"),
            CapturedAtUtc: _clock.UtcNow.ToUniversalTime(),
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

    private sealed record InitialAdapterRead(
        NetworkAdapterSnapshot? Snapshot = null,
        bool Failed = false,
        bool Cancelled = false);

    private sealed record PostMutationRead(
        NetworkAdapterSnapshot? Snapshot = null,
        bool Failed = false);
}
