using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.IntegrationTests.Harness;

public sealed class RecoveryDiagnosticReportTests
{
    private static readonly NetworkAdapterId AdapterId =
        new("{11111111-2222-3333-4444-555555555555}");

    private static readonly string[] GatewayValues = { "192.0.2.1" };

    [Fact]
    public void Create_WhenAllReadsAndCapabilityAreValid_AllowsProceeding()
    {
        NetworkAdapterRecoverySnapshot snapshot = Snapshot();

        RecoveryDiagnosticReport report = CreateReport(
            new ManagedAdapterDiagnosticRead(
                ManagedAdapterReadDiagnosticStatus.Found,
                snapshot.Adapter),
            RecoveryDiagnostic(snapshot));

        Assert.True(report.CanProceed);
        Assert.Equal(RestoreCapabilityDiagnosticStatus.Capable, report.RestoreCapability);
        Assert.Empty(report.RestoreCapabilityReasons);
    }

    [Fact]
    public void FormatSanitizedLines_WithManualDns_IncludesNativeSourceShapeWithoutValues()
    {
        NetworkAdapterRecoverySnapshot snapshot = Snapshot(
            dnsMode: DnsConfigurationMode.Manual,
            configuredDns: ["1.1.1.1"]);
        RecoveryDiagnosticReport report = CreateReport(
            new ManagedAdapterDiagnosticRead(
                ManagedAdapterReadDiagnosticStatus.Found,
                snapshot.Adapter),
            RecoveryDiagnostic(snapshot));

        string output = string.Join(Environment.NewLine, report.FormatSanitizedLines());

        Assert.Contains("DnsNativeFlags: 2", output, StringComparison.Ordinal);
        Assert.Contains("DnsNameServerPresent: True", output, StringComparison.Ordinal);
        Assert.Contains("ConfiguredIpv4DnsCount: 1", output, StringComparison.Ordinal);
        Assert.DoesNotContain("1.1.1.1", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WhenAnyRestoreCapabilityReasonExists_CannotPassDestructiveGate()
    {
        NetworkAdapterRecoverySnapshot[] blockedSnapshots =
        {
            Snapshot(mode: NetworkConfigurationMode.Unknown),
            Snapshot(dnsMode: DnsConfigurationMode.Unknown),
            Snapshot(dnsMode: DnsConfigurationMode.Manual, configuredDns: Array.Empty<string>()),
            Snapshot(recoveryGateway: "192.0.2.254"),
            Snapshot(gatewayMetric: null),
            Snapshot(subnetMask: null)
        };

        foreach (NetworkAdapterRecoverySnapshot snapshot in blockedSnapshots)
        {
            RecoveryDiagnosticReport report = CreateReport(
                new ManagedAdapterDiagnosticRead(
                    ManagedAdapterReadDiagnosticStatus.Found,
                    snapshot.Adapter),
                RecoveryDiagnostic(snapshot));

            Assert.False(report.CanProceed);
            Assert.Equal(RestoreCapabilityDiagnosticStatus.NotCapable, report.RestoreCapability);
            Assert.NotEmpty(report.RestoreCapabilityReasons);
        }
    }

    [Fact]
    public void Create_WhenManagedReadRecoveryStatusOrIdentityFails_CannotProceed()
    {
        NetworkAdapterRecoverySnapshot snapshot = Snapshot();
        NetworkAdapterSnapshot wrongIdentity = snapshot.Adapter with
        {
            Id = new NetworkAdapterId("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}")
        };
        RecoveryDiagnosticReport[] reports =
        {
            CreateReport(
                new ManagedAdapterDiagnosticRead(
                    ManagedAdapterReadDiagnosticStatus.NotFound,
                    Snapshot: null),
                RecoveryDiagnostic(snapshot)),
            CreateReport(
                new ManagedAdapterDiagnosticRead(
                    ManagedAdapterReadDiagnosticStatus.ReadFailed,
                    Snapshot: null),
                RecoveryDiagnostic(snapshot)),
            CreateReport(
                new ManagedAdapterDiagnosticRead(
                    ManagedAdapterReadDiagnosticStatus.Found,
                    wrongIdentity),
                RecoveryDiagnostic(snapshot)),
            CreateReport(
                new ManagedAdapterDiagnosticRead(
                    ManagedAdapterReadDiagnosticStatus.Found,
                    snapshot.Adapter),
                new NetworkAdapterRecoveryDiagnosticReadResult(
                    new NetworkAdapterRecoveryReadResult(
                        NetworkAdapterRecoveryReadStatus.ReadFailed),
                    DnsProbe: null)),
            CreateReport(
                new ManagedAdapterDiagnosticRead(
                    ManagedAdapterReadDiagnosticStatus.Found,
                    snapshot.Adapter),
                RecoveryDiagnostic(snapshot with { Adapter = wrongIdentity }))
        };

        Assert.All(reports, report => Assert.False(report.CanProceed));
    }

    [Fact]
    public async Task FormatSanitizedLines_WhenManagedReadThrows_DoesNotExposeExceptionOrNetworkValues()
    {
        const string sensitiveException = "SECRET RAW EXCEPTION";
        ManagedAdapterDiagnosticRead managed = await RecoveryDiagnosticCapture
            .ReadManagedAdapterAsync(
                new ThrowingAdapterReader(new PlatformNotSupportedException(sensitiveException)),
                AdapterId,
                CancellationToken.None);
        NetworkAdapterRecoverySnapshot snapshot = Snapshot();
        RecoveryDiagnosticReport report = CreateReport(managed, RecoveryDiagnostic(snapshot));

        string output = string.Join(Environment.NewLine, report.FormatSanitizedLines());

        Assert.DoesNotContain(sensitiveException, output, StringComparison.Ordinal);
        Assert.DoesNotContain("192.0.2.10", output, StringComparison.Ordinal);
        Assert.DoesNotContain(AdapterId.Value, output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ManagedAdapterRead: ReadFailed", output, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadOnlySettings_WhenOptInIsAbsent_RefusesWithoutDestructiveConfiguration()
    {
        ReadOnlyRecoveryDiagnosticSettingsResult result =
            ReadOnlyRecoveryDiagnosticSettingsParser.Parse(
                new Dictionary<string, string?>(StringComparer.Ordinal));

        Assert.False(result.CanRun);
        Assert.Contains(result.RefusalReasons, reason =>
            reason.Contains(
                ReadOnlyRecoveryDiagnosticSettingsParser.EnableVariable,
                StringComparison.Ordinal));
    }

    [Fact]
    public void ReadOnlySettings_WhenExactGuidAndDiagnosticOptInArePresent_AllowsOnlyThatGuid()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            [ReadOnlyRecoveryDiagnosticSettingsParser.EnableVariable] = "1",
            [ReadOnlyRecoveryDiagnosticSettingsParser.AdapterVariable] = AdapterId.Value
        };

        ReadOnlyRecoveryDiagnosticSettingsResult result =
            ReadOnlyRecoveryDiagnosticSettingsParser.Parse(values);

        Assert.True(result.CanRun);
        Assert.Equal(AdapterId, result.Settings!.AdapterId);
    }

    private static RecoveryDiagnosticReport CreateReport(
        ManagedAdapterDiagnosticRead managed,
        NetworkAdapterRecoveryDiagnosticReadResult recovery) =>
        RecoveryDiagnosticReport.Create(AdapterId, managed, recovery);

    private static NetworkAdapterRecoveryDiagnosticReadResult RecoveryDiagnostic(
        NetworkAdapterRecoverySnapshot snapshot) =>
        new(
            NetworkAdapterRecoveryReadResult.Success(snapshot),
            new DnsRecoveryProbeDiagnostic(
                snapshot.DnsMode switch
                {
                    DnsConfigurationMode.Automatic => DnsRecoveryProbeStatus.Automatic,
                    DnsConfigurationMode.Manual => DnsRecoveryProbeStatus.Manual,
                    _ => DnsRecoveryProbeStatus.NativeCallFailed
                },
                NativeResult: snapshot.DnsMode == DnsConfigurationMode.Unknown ? 87u : 0u,
                NativeFlags: snapshot.DnsMode == DnsConfigurationMode.Manual ? 0x0002UL : 0,
                NameServerPresent: snapshot.DnsMode == DnsConfigurationMode.Manual,
                AdapterManualServerFlag: snapshot.DnsMode == DnsConfigurationMode.Manual,
                ProfileServerFlag: false,
                UsableIpv4ServerCount: snapshot.ConfiguredIpv4DnsServers.Length));

    private static NetworkAdapterRecoverySnapshot Snapshot(
        NetworkConfigurationMode mode = NetworkConfigurationMode.Static,
        DnsConfigurationMode dnsMode = DnsConfigurationMode.Automatic,
        string[]? configuredDns = null,
        string recoveryGateway = "192.0.2.1",
        ushort? gatewayMetric = 25,
        string? subnetMask = "255.255.255.0")
    {
        configuredDns ??= Array.Empty<string>();
        NetworkAdapterSnapshot adapter = new(
            AdapterId,
            "Sensitive adapter name",
            "Sensitive adapter description",
            "00-11-22-33-44-55",
            IsConnected: true,
            LinkSpeedBitsPerSecond: 1_000_000_000,
            mode,
            "192.0.2.10",
            subnetMask,
            "192.0.2.1",
            PrimaryDns: null,
            SecondaryDns: null,
            new Ipv4AddressCollection(
                new[] { new Ipv4AddressAssignment("192.0.2.10", subnetMask) }),
            new Ipv4AddressValueCollection(GatewayValues),
            Ipv4AddressValueCollection.Empty);

        return new NetworkAdapterRecoverySnapshot(
            adapter,
            dnsMode,
            configuredDns,
            new[] { new Ipv4GatewayRecoveryState(recoveryGateway, gatewayMetric) });
    }

    private sealed class ThrowingAdapterReader : INetworkAdapterReader
    {
        private readonly Exception _exception;

        public ThrowingAdapterReader(Exception exception) => _exception = exception;

        public Task<IReadOnlyList<NetworkAdapterSnapshot>> GetAdaptersAsync(
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<NetworkAdapterSnapshot?> GetAdapterAsync(
            NetworkAdapterId adapterId,
            CancellationToken cancellationToken) =>
            throw _exception;
    }
}
