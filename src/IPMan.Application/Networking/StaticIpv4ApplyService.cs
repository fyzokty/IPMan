using System.Net.NetworkInformation;
using IPMan.Application.Common;
using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>
/// Owns the complete static apply transaction: preflight, safety gates,
/// rollback capture, one serialized mutation and bounded fresh-read verification.
/// </summary>
public sealed class StaticIpv4ApplyService : IStaticIpv4ApplyService
{
    private readonly INetworkConfigurationPreflightService _preflightService;
    private readonly IRollbackSnapshotRepository _rollbackRepository;
    private readonly INetworkAdapterConfigurator _configurator;
    private readonly INetworkAdapterReader _adapterReader;
    private readonly INetworkAdapterRecoveryReader _recoveryReader;
    private readonly INetworkMutationCoordinator _mutationCoordinator;
    private readonly IStaticIpv4ConfigurationComparer _comparer;
    private readonly IDelayProvider _delayProvider;
    private readonly IClock _clock;
    private readonly StaticIpv4ApplyOptions _options;

    public StaticIpv4ApplyService(
        INetworkConfigurationPreflightService preflightService,
        IRollbackSnapshotRepository rollbackRepository,
        INetworkAdapterConfigurator configurator,
        INetworkAdapterReader adapterReader,
        INetworkAdapterRecoveryReader recoveryReader,
        INetworkMutationCoordinator mutationCoordinator,
        IStaticIpv4ConfigurationComparer comparer,
        IDelayProvider delayProvider,
        IClock clock,
        StaticIpv4ApplyOptions options)
    {
        ArgumentNullException.ThrowIfNull(preflightService);
        ArgumentNullException.ThrowIfNull(rollbackRepository);
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(adapterReader);
        ArgumentNullException.ThrowIfNull(recoveryReader);
        ArgumentNullException.ThrowIfNull(mutationCoordinator);
        ArgumentNullException.ThrowIfNull(comparer);
        ArgumentNullException.ThrowIfNull(delayProvider);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(options);

        if (options.VerificationAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "At least one verification attempt is required.");
        }

        if (options.VerificationDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Verification delay cannot be negative.");
        }

        _preflightService = preflightService;
        _rollbackRepository = rollbackRepository;
        _configurator = configurator;
        _adapterReader = adapterReader;
        _recoveryReader = recoveryReader;
        _mutationCoordinator = mutationCoordinator;
        _comparer = comparer;
        _delayProvider = delayProvider;
        _clock = clock;
        _options = options;
    }

    public async Task<StaticIpv4ApplyResult> ApplyAsync(
        StaticIpv4ApplyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.DesiredConfiguration);

        IDisposable lease;
        try
        {
            lease = await _mutationCoordinator.AcquireAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.Cancelled);
        }

        using (lease)
        {
            return await ApplySerializedAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<StaticIpv4ApplyResult> ApplySerializedAsync(
        StaticIpv4ApplyRequest request,
        CancellationToken cancellationToken)
    {
        NetworkConfigurationPreflightResult preflight = await _preflightService
            .PreflightAsync(request.AdapterId, request.DesiredConfiguration, cancellationToken)
            .ConfigureAwait(false);

        StaticIpv4ApplyResult? stopped = HandlePreflightStop(request, preflight);

        if (stopped is not null)
        {
            return stopped;
        }

        StaticIpv4Configuration desired = preflight.Validation?.NormalizedConfiguration ??
            throw new InvalidOperationException("A successful preflight must contain normalized configuration.");

        NetworkAdapterRecoveryReadResult recoveryRead;

        try
        {
            recoveryRead = await _recoveryReader
                .ReadAsync(request.AdapterId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.Cancelled, preflight);
        }

        if (recoveryRead.Status != NetworkAdapterRecoveryReadStatus.Success ||
            recoveryRead.Snapshot is null)
        {
            return new StaticIpv4ApplyResult(
                recoveryRead.Status == NetworkAdapterRecoveryReadStatus.AdapterUnavailable
                    ? StaticIpv4ApplyStatus.AdapterUnavailable
                    : StaticIpv4ApplyStatus.AdapterReadFailed,
                preflight);
        }

        NetworkAdapterRecoverySnapshot recovery = recoveryRead.Snapshot;

        if (recovery.Adapter.Id != request.AdapterId)
        {
            return new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.AdapterReadFailed, preflight);
        }

        NetworkAdapterSnapshot current = recovery.Adapter;

        StaticIpv4SafetyBlock safetyBlock = GetSafetyBlock(current);

        if (safetyBlock != StaticIpv4SafetyBlock.None)
        {
            return new StaticIpv4ApplyResult(
                StaticIpv4ApplyStatus.SafetyBlocked,
                preflight,
                safetyBlock);
        }

        NetworkConfigurationComparisonResult refreshedComparison = CompareWithDnsSemantics(
            current,
            recovery,
            desired);

        if (refreshedComparison.IsEquivalent && FullStateMatches(current, recovery, desired))
        {
            return new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.NoChange, preflight);
        }

        if (!recovery.IsRestoreCapable || !CanPlanGatewayMutation(recovery, desired))
        {
            return new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.RecoveryStateUnavailable, preflight);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.Cancelled, preflight);
        }

        NetworkRollbackSnapshot rollbackSnapshot = CreateRollbackSnapshot(recovery);
        RollbackCaptureResult capture;

        try
        {
            capture = await _rollbackRepository
                .SaveAsync(rollbackSnapshot, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.Cancelled, preflight);
        }

        if (!capture.IsSuccess)
        {
            return new StaticIpv4ApplyResult(
                StaticIpv4ApplyStatus.RollbackCaptureFailed,
                preflight);
        }

        RollbackSnapshotReference rollback = capture.Reference!;

        if (cancellationToken.IsCancellationRequested)
        {
            return new StaticIpv4ApplyResult(
                StaticIpv4ApplyStatus.Cancelled,
                preflight,
                Rollback: rollback);
        }

        StaticIpv4MutationPlan plan = new(
            desired,
            GetGatewayMutationMode(current, desired),
            GetGatewayMetric(recovery, desired),
            GetDnsMutationMode(recovery, desired),
            current.Mode);

        // From this point onward cancellation cannot mean rollback. Complete the
        // critical call and fresh-read actual Windows state with a non-cancelled
        // token, then report the real outcome.
        NetworkApplyResult mutation = await _configurator
            .ApplyStaticAsync(request.AdapterId, plan, CancellationToken.None)
            .ConfigureAwait(false);

        if (!mutation.IsSuccess)
        {
            PostMutationRead read = mutation.WasMutationAttempted
                ? await ReadAfterMutationAsync(request.AdapterId).ConfigureAwait(false)
                : new PostMutationRead();

            return new StaticIpv4ApplyResult(
                mutation.IsPartialFailure
                    ? StaticIpv4ApplyStatus.PartialFailure
                    : StaticIpv4ApplyStatus.MutationFailed,
                preflight,
                Rollback: rollback,
                Mutation: mutation,
                ActualSnapshot: read.Snapshot);
        }

        return await VerifyAsync(
                request.AdapterId,
                plan,
                preflight,
                rollback,
                mutation)
            .ConfigureAwait(false);
    }

    private static StaticIpv4ApplyResult? HandlePreflightStop(
        StaticIpv4ApplyRequest request,
        NetworkConfigurationPreflightResult preflight) =>
        preflight.Status switch
        {
            NetworkConfigurationPreflightStatus.Ready => null,
            // The lightweight reader cannot distinguish automatic DNS from a
            // manual list whose effective servers happen to be identical.
            // Continue to the exact recovery read before declaring no change.
            NetworkConfigurationPreflightStatus.NoChange => null,
            NetworkConfigurationPreflightStatus.ValidationFailed =>
                new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.ValidationFailed, preflight),
            NetworkConfigurationPreflightStatus.AdapterUnavailable =>
                new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.AdapterUnavailable, preflight),
            NetworkConfigurationPreflightStatus.AdapterReadFailed =>
                new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.AdapterReadFailed, preflight),
            NetworkConfigurationPreflightStatus.MultipleIpv4RequiresSafetyDecision =>
                new StaticIpv4ApplyResult(
                    StaticIpv4ApplyStatus.SafetyBlocked,
                    preflight,
                    StaticIpv4SafetyBlock.MultipleIpv4Addresses),
            NetworkConfigurationPreflightStatus.PotentialAddressConflict
                when !request.ConfirmPotentialConflict =>
                new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.ConflictConfirmationRequired, preflight),
            NetworkConfigurationPreflightStatus.PotentialAddressConflict => null,
            NetworkConfigurationPreflightStatus.ProbeIndeterminate
                when !request.ContinueAfterIndeterminateProbe =>
                new StaticIpv4ApplyResult(
                    StaticIpv4ApplyStatus.ProbeIndeterminateConfirmationRequired,
                    preflight),
            NetworkConfigurationPreflightStatus.ProbeIndeterminate => null,
            NetworkConfigurationPreflightStatus.Cancelled =>
                new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.Cancelled, preflight),
            _ => throw new InvalidOperationException($"Unsupported preflight status: {preflight.Status}.")
        };

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

    private NetworkRollbackSnapshot CreateRollbackSnapshot(NetworkAdapterRecoverySnapshot recovery) =>
        new(
            SchemaVersion: 2,
            SnapshotId: Guid.NewGuid().ToString("N"),
            CapturedAtUtc: _clock.UtcNow.ToUniversalTime(),
            State: RollbackSnapshotState.Captured,
            AdapterId: recovery.Adapter.Id,
            AdapterName: recovery.Adapter.Name,
            AdapterDescription: recovery.Adapter.Description,
            Mode: recovery.Adapter.Mode,
            Ipv4Addresses: recovery.Adapter.Ipv4Addresses.ToArray(),
            Ipv4Gateways: recovery.Ipv4Gateways.ToArray(),
            DnsMode: recovery.DnsMode,
            ConfiguredIpv4DnsServers: recovery.ConfiguredIpv4DnsServers.ToArray(),
            Ipv4DnsServers: recovery.Adapter.Ipv4DnsServers.ToArray());

    private async Task<StaticIpv4ApplyResult> VerifyAsync(
        NetworkAdapterId adapterId,
        StaticIpv4MutationPlan plan,
        NetworkConfigurationPreflightResult preflight,
        RollbackSnapshotReference rollback,
        NetworkApplyResult mutation)
    {
        StaticIpv4Configuration desired = plan.Configuration;
        NetworkAdapterSnapshot? lastSnapshot = null;
        NetworkConfigurationComparisonResult? lastComparison = null;

        for (int attempt = 0; attempt < _options.VerificationAttempts; attempt++)
        {
            PostMutationRead read = await ReadAfterMutationAsync(adapterId).ConfigureAwait(false);

            if (read.Failed)
            {
                return new StaticIpv4ApplyResult(
                    StaticIpv4ApplyStatus.VerificationFailed,
                    preflight,
                    Rollback: rollback,
                    Mutation: mutation);
            }

            NetworkAdapterSnapshot? actual = read.Snapshot;

            if (actual is null)
            {
                return new StaticIpv4ApplyResult(
                    StaticIpv4ApplyStatus.AdapterUnavailableDuringVerification,
                    preflight,
                    Rollback: rollback,
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
                return new StaticIpv4ApplyResult(
                    StaticIpv4ApplyStatus.VerificationFailed,
                    preflight,
                    Rollback: rollback,
                    Mutation: mutation,
                    ActualSnapshot: actual);
            }

            NetworkAdapterRecoverySnapshot recovery = recoveryRead.Snapshot;
            lastComparison = CompareWithDnsSemantics(actual, recovery, desired);

            if (lastComparison.IsEquivalent &&
                FullStateMatches(actual, recovery, desired) &&
                GatewayMetricMatches(recovery, plan))
            {
                return new StaticIpv4ApplyResult(
                    StaticIpv4ApplyStatus.VerifiedSuccess,
                    preflight,
                    Rollback: rollback,
                    Mutation: mutation,
                    ActualSnapshot: actual,
                    VerificationComparison: lastComparison);
            }

            if (attempt + 1 < _options.VerificationAttempts)
            {
                await _delayProvider
                    .DelayAsync(_options.VerificationDelay, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        return new StaticIpv4ApplyResult(
            StaticIpv4ApplyStatus.VerificationFailed,
            preflight,
            Rollback: rollback,
            Mutation: mutation,
            ActualSnapshot: lastSnapshot,
            VerificationComparison: lastComparison);
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

    private NetworkConfigurationComparisonResult CompareWithDnsSemantics(
        NetworkAdapterSnapshot actual,
        NetworkAdapterRecoverySnapshot recovery,
        StaticIpv4Configuration desired)
    {
        NetworkConfigurationDifference differences = _comparer.Compare(actual, desired).Differences;

        if (DnsStateMatches(recovery, desired))
        {
            differences &= ~(NetworkConfigurationDifference.PrimaryDns |
                NetworkConfigurationDifference.SecondaryDns);
        }

        return new NetworkConfigurationComparisonResult(differences);
    }

    private static bool FullStateMatches(
        NetworkAdapterSnapshot actual,
        NetworkAdapterRecoverySnapshot recovery,
        StaticIpv4Configuration desired)
    {
        string[] desiredGateways = desired.Gateway is null ? Array.Empty<string>() : new[] { desired.Gateway };

        return actual.Mode == NetworkConfigurationMode.Static &&
            actual.Ipv4Addresses.Count == 1 &&
            actual.Ipv4Gateways.SequenceEqual(desiredGateways, StringComparer.Ordinal) &&
            DnsStateMatches(recovery, desired);
    }

    private static DnsMutationMode GetDnsMutationMode(
        NetworkAdapterRecoverySnapshot current,
        StaticIpv4Configuration desired)
    {
        string[] desiredDns = DesiredDns(desired);

        if (desiredDns.Length == 0)
        {
            return current.DnsMode == DnsConfigurationMode.Automatic
                ? DnsMutationMode.LeaveUnchanged
                : DnsMutationMode.ClearToAutomatic;
        }

        return current.DnsMode == DnsConfigurationMode.Manual &&
            current.ConfiguredIpv4DnsServers.SequenceEqual(desiredDns, StringComparer.Ordinal)
                ? DnsMutationMode.LeaveUnchanged
                : DnsMutationMode.Set;
    }

    private static GatewayMutationMode GetGatewayMutationMode(
        NetworkAdapterSnapshot current,
        StaticIpv4Configuration desired) =>
        desired.Gateway is null
            ? current.Ipv4Gateways.Count == 0
                ? GatewayMutationMode.LeaveAbsent
                : GatewayMutationMode.Clear
            : GatewayMutationMode.Set;

    private static bool CanPlanGatewayMutation(
        NetworkAdapterRecoverySnapshot current,
        StaticIpv4Configuration desired) =>
        !GatewayAddressIsUnchanged(current, desired) ||
        current.Ipv4Gateways[0].Metric.HasValue;

    private static ushort? GetGatewayMetric(
        NetworkAdapterRecoverySnapshot current,
        StaticIpv4Configuration desired)
    {
        if (desired.Gateway is null)
        {
            return null;
        }

        return GatewayAddressIsUnchanged(current, desired)
            ? current.Ipv4Gateways[0].Metric
            : (ushort)1;
    }

    private static bool GatewayAddressIsUnchanged(
        NetworkAdapterRecoverySnapshot current,
        StaticIpv4Configuration desired) =>
        desired.Gateway is not null &&
        current.Ipv4Gateways.Length == 1 &&
        string.Equals(
            current.Ipv4Gateways[0].Address,
            desired.Gateway,
            StringComparison.Ordinal);

    private static bool GatewayMetricMatches(
        NetworkAdapterRecoverySnapshot actual,
        StaticIpv4MutationPlan plan)
    {
        if (plan.Configuration.Gateway is null)
        {
            return actual.Ipv4Gateways.Length == 0;
        }

        return actual.Ipv4Gateways.Length == 1 &&
            string.Equals(
                actual.Ipv4Gateways[0].Address,
                plan.Configuration.Gateway,
                StringComparison.Ordinal) &&
            actual.Ipv4Gateways[0].Metric == plan.GatewayMetric;
    }

    private static bool DnsStateMatches(
        NetworkAdapterRecoverySnapshot actual,
        StaticIpv4Configuration desired)
    {
        string[] desiredDns = DesiredDns(desired);

        return desiredDns.Length == 0
            ? actual.DnsMode == DnsConfigurationMode.Automatic
            : actual.DnsMode == DnsConfigurationMode.Manual &&
                actual.ConfiguredIpv4DnsServers.SequenceEqual(desiredDns, StringComparer.Ordinal);
    }

    private static string[] DesiredDns(StaticIpv4Configuration desired) =>
        new[] { desired.PrimaryDns, desired.SecondaryDns }
            .Where(value => value is not null)
            .Select(value => value!)
            .ToArray();

    private sealed record PostMutationRead(
        NetworkAdapterSnapshot? Snapshot = null,
        bool Failed = false);
}
