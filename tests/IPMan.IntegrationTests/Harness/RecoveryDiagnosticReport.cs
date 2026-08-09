using System.Globalization;
using System.Net.NetworkInformation;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;

namespace IPMan.IntegrationTests.Harness;

public enum ManagedAdapterReadDiagnosticStatus
{
    Found = 0,
    NotFound = 1,
    ReadFailed = 2
}

public enum RestoreCapabilityDiagnosticStatus
{
    Unavailable = 0,
    Capable = 1,
    NotCapable = 2
}

public sealed record ManagedAdapterDiagnosticRead(
    ManagedAdapterReadDiagnosticStatus Status,
    NetworkAdapterSnapshot? Snapshot);

public sealed record RecoveryDiagnosticReport(
    ManagedAdapterReadDiagnosticStatus ManagedAdapterRead,
    bool? ManagedAdapterIdentityMatches,
    NetworkAdapterRecoveryReadStatus RecoveryStatus,
    bool? RecoveryIdentityMatches,
    NetworkConfigurationMode? AdapterMode,
    DnsConfigurationMode? DnsMode,
    DnsRecoveryProbeStatus? DnsProbeStatus,
    uint? DnsNativeResult,
    ulong? DnsNativeFlags,
    bool? DnsNameServerPresent,
    int? ConfiguredIpv4DnsCount,
    int? Ipv4Count,
    bool? SubnetMasksComplete,
    int? GatewayCount,
    bool? GatewayMetricsComplete,
    RestoreCapabilityDiagnosticStatus RestoreCapability,
    IReadOnlyList<NetworkRecoveryCapabilityReason> RestoreCapabilityReasons)
{
    public bool CanProceed =>
        ManagedAdapterRead == ManagedAdapterReadDiagnosticStatus.Found &&
        ManagedAdapterIdentityMatches == true &&
        RecoveryStatus == NetworkAdapterRecoveryReadStatus.Success &&
        RecoveryIdentityMatches == true &&
        RestoreCapability == RestoreCapabilityDiagnosticStatus.Capable;

    public static RecoveryDiagnosticReport Create(
        NetworkAdapterId expectedAdapterId,
        ManagedAdapterDiagnosticRead managed,
        NetworkAdapterRecoveryDiagnosticReadResult recoveryDiagnostic)
    {
        ArgumentNullException.ThrowIfNull(managed);
        ArgumentNullException.ThrowIfNull(recoveryDiagnostic);
        NetworkAdapterRecoverySnapshot? snapshot = recoveryDiagnostic.RecoveryRead.Snapshot;
        NetworkRecoveryCapabilityEvaluation? capability = snapshot?.RestoreCapability;

        return new RecoveryDiagnosticReport(
            managed.Status,
            managed.Snapshot is null ? null : managed.Snapshot.Id == expectedAdapterId,
            recoveryDiagnostic.RecoveryRead.Status,
            snapshot is null ? null : snapshot.Adapter.Id == expectedAdapterId,
            snapshot?.Adapter.Mode,
            snapshot?.DnsMode,
            recoveryDiagnostic.DnsProbe?.Status,
            recoveryDiagnostic.DnsProbe?.NativeResult,
            recoveryDiagnostic.DnsProbe?.NativeFlags,
            recoveryDiagnostic.DnsProbe?.NameServerPresent,
            recoveryDiagnostic.DnsProbe?.UsableIpv4ServerCount,
            snapshot?.Adapter.Ipv4Addresses.Count,
            snapshot is null
                ? null
                : snapshot.Adapter.Ipv4Addresses.All(address => address.SubnetMask is not null),
            snapshot?.Ipv4Gateways.Length,
            snapshot is null
                ? null
                : snapshot.Ipv4Gateways.All(gateway => gateway.Metric.HasValue),
            capability is null
                ? RestoreCapabilityDiagnosticStatus.Unavailable
                : capability.IsCapable
                    ? RestoreCapabilityDiagnosticStatus.Capable
                    : RestoreCapabilityDiagnosticStatus.NotCapable,
            capability?.BlockingReasons ?? Array.Empty<NetworkRecoveryCapabilityReason>());
    }

    public IReadOnlyList<string> FormatSanitizedLines() =>
        new[]
        {
            $"ManagedAdapterRead: {ManagedAdapterRead}",
            $"ManagedAdapterIdentityMatches: {Format(ManagedAdapterIdentityMatches)}",
            $"RecoveryStatus: {RecoveryStatus}",
            $"RecoveryIdentityMatches: {Format(RecoveryIdentityMatches)}",
            $"AdapterMode: {Format(AdapterMode)}",
            $"DnsMode: {Format(DnsMode)}",
            $"DnsProbeStatus: {Format(DnsProbeStatus)}",
            $"DnsNativeResult: {Format(DnsNativeResult)}",
            $"DnsNativeFlags: {Format(DnsNativeFlags)}",
            $"DnsNameServerPresent: {Format(DnsNameServerPresent)}",
            $"ConfiguredIpv4DnsCount: {Format(ConfiguredIpv4DnsCount)}",
            $"Ipv4Count: {Format(Ipv4Count)}",
            $"SubnetMasksComplete: {Format(SubnetMasksComplete)}",
            $"GatewayCount: {Format(GatewayCount)}",
            $"GatewayMetricsComplete: {Format(GatewayMetricsComplete)}",
            $"RestoreCapability: {RestoreCapability}",
            "RestoreCapabilityReasons: " +
                (RestoreCapabilityReasons.Count == 0
                    ? "None"
                    : string.Join(",", RestoreCapabilityReasons))
        };

    private static string Format<T>(T? value) where T : struct =>
        value?.ToString() ?? "N/A";

    private static string Format(uint? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? "N/A";

    private static string Format(ulong? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? "N/A";
}

public static class RecoveryDiagnosticCapture
{
    public static async Task<ManagedAdapterDiagnosticRead> ReadManagedAdapterAsync(
        INetworkAdapterReader reader,
        NetworkAdapterId adapterId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reader);

        try
        {
            NetworkAdapterSnapshot? snapshot = await reader
                .GetAdapterAsync(adapterId, cancellationToken)
                .ConfigureAwait(false);
            return new ManagedAdapterDiagnosticRead(
                snapshot is null
                    ? ManagedAdapterReadDiagnosticStatus.NotFound
                    : ManagedAdapterReadDiagnosticStatus.Found,
                snapshot);
        }
        catch (NetworkInformationException)
        {
            return new ManagedAdapterDiagnosticRead(
                ManagedAdapterReadDiagnosticStatus.ReadFailed,
                Snapshot: null);
        }
        catch (PlatformNotSupportedException)
        {
            return new ManagedAdapterDiagnosticRead(
                ManagedAdapterReadDiagnosticStatus.ReadFailed,
                Snapshot: null);
        }
    }
}
