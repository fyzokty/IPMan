using System.Reflection;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using IPMan.IntegrationTests.Observation;
using Xunit;

namespace IPMan.IntegrationTests.Harness;

public sealed class DnsTruthDiagnosticReportTests
{
    private static readonly string[] ExpectedIpv4Servers = { "8.8.8.8", "1.1.1.1" };

    private static readonly NetworkAdapterId AdapterId =
        new("{11111111-2222-3333-4444-555555555555}");
    private static readonly WindowsInterfaceIdentity Identity =
        new(new Guid(AdapterId.Value), 1234, 9);

    [Fact]
    public void Capture_WhenExactIdentityResolves_ReadsAllSourcesWithSameIdentity()
    {
        FakeIdentityResolver resolver = new(Identity);
        FakeDnsSettingsReader settings = new(DnsSettings(readSuccess: true));
        FakeCimReader cim = new(CimSuccess());
        FakeAdaptersAddressesReader adapters = new(AdaptersSuccess());
        DnsTruthDiagnosticCapture capture = new(resolver, settings, cim, adapters);

        DnsTruthDiagnosticReport report = capture.Capture(AdapterId);

        Assert.True(report.ExactIdentityResolved);
        Assert.True(report.AllSourcesReadSuccessfully);
        Assert.Equal(AdapterId, resolver.AdapterId);
        Assert.Equal(AdapterId, settings.AdapterId);
        Assert.Equal(Identity, cim.Identity);
        Assert.Equal(Identity, adapters.Identity);
        Assert.Equal(2, resolver.RevalidateCount);
        Assert.False(DnsTruthDiagnosticReport.MutationPerformed);
    }

    [Fact]
    public void Capture_WhenIdentityCannotResolve_FailsClosedWithoutSourceReads()
    {
        FakeIdentityResolver resolver = new(Identity)
        {
            ResolveResult = new InterfaceIdentityResolution(
                Ipv4DefaultRouteClearStatus.InterfaceResolutionFailed,
                TechnicalCode: 1168)
        };
        FakeDnsSettingsReader settings = new(DnsSettings(readSuccess: true));
        FakeCimReader cim = new(CimSuccess());
        FakeAdaptersAddressesReader adapters = new(AdaptersSuccess());
        DnsTruthDiagnosticCapture capture = new(resolver, settings, cim, adapters);

        DnsTruthDiagnosticReport report = capture.Capture(AdapterId);

        Assert.False(report.ExactIdentityResolved);
        Assert.False(report.AllSourcesReadSuccessfully);
        Assert.Equal((uint)1168, report.IdentityTechnicalCode);
        Assert.Equal(0, settings.CallCount);
        Assert.Null(cim.Identity);
        Assert.Null(adapters.Identity);
    }

    [Fact]
    public void Capture_WhenIdentityDriftsAfterReads_DoesNotReportSuccess()
    {
        FakeIdentityResolver resolver = new(Identity);
        resolver.Revalidations.Enqueue(new InterfaceIdentityResolution(
            Ipv4DefaultRouteClearStatus.Success,
            Identity));
        resolver.Revalidations.Enqueue(new InterfaceIdentityResolution(
            Ipv4DefaultRouteClearStatus.InterfaceResolutionFailed));
        DnsTruthDiagnosticCapture capture = new(
            resolver,
            new FakeDnsSettingsReader(DnsSettings(readSuccess: true)),
            new FakeCimReader(CimSuccess()),
            new FakeAdaptersAddressesReader(AdaptersSuccess()));

        DnsTruthDiagnosticReport report = capture.Capture(AdapterId);

        Assert.False(report.ExactIdentityResolved);
        Assert.False(report.AllSourcesReadSuccessfully);
        Assert.Equal(Ipv4DefaultRouteClearStatus.InterfaceResolutionFailed.ToString(), report.IdentityStatus);
    }

    [Fact]
    public void FormatLines_ReportsAllTruthSourcesAndExplicitReadOnlyResult()
    {
        DnsTruthDiagnosticReport report = new(
            AdapterId,
            ExactIdentityResolved: true,
            IdentityStatus: "Success",
            IdentityTechnicalCode: null,
            InterfaceIndex: 9,
            DnsSettings(readSuccess: true),
            CimSuccess(),
            AdaptersSuccess());

        IReadOnlyList<string> lines = report.FormatLines();

        Assert.Contains("GetInterfaceDnsSettings:", lines);
        Assert.Contains("DnsClientCimIPv4:", lines);
        Assert.Contains("DnsClientCimIPv6:", lines);
        Assert.Contains("GetAdaptersAddressesIPv4:", lines);
        Assert.Contains("GetAdaptersAddressesIPv6:", lines);
        Assert.Contains("  Servers: [8.8.8.8, 1.1.1.1]", lines);
        Assert.Contains("MutationPerformed: False", lines);
    }

    [Fact]
    public void AllSourcesReadSuccessfully_WhenDnsRicherStateIsIncomplete_RemainsFalse()
    {
        DnsTruthDiagnosticReport report = new(
            AdapterId,
            ExactIdentityResolved: true,
            IdentityStatus: "Success",
            IdentityTechnicalCode: null,
            InterfaceIndex: 9,
            DnsSettings(readSuccess: true) with
            {
                Status = DnsInterfaceSettingsTruthReadStatus.IncompleteRicherState,
                RicherPropertiesStatus = DnsRicherPropertiesObservationStatus.Incomplete
            },
            CimSuccess(),
            AdaptersSuccess());

        Assert.False(report.AllSourcesReadSuccessfully);
    }

    [Fact]
    public void Capture_DependsOnlyOnReadInterfaces()
    {
        Type[] dependencyTypes = typeof(DnsTruthDiagnosticCapture)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Select(field => field.FieldType)
            .ToArray();

        Assert.Contains(typeof(IWindowsInterfaceIdentityResolver), dependencyTypes);
        Assert.Contains(typeof(IDnsSettingsTruthReader), dependencyTypes);
        Assert.Contains(typeof(IWindowsDnsClientServerAddressReader), dependencyTypes);
        Assert.Contains(typeof(IWindowsAdaptersAddressesDnsReader), dependencyTypes);
        Assert.DoesNotContain(dependencyTypes, type =>
            type.Name.Contains("Writer", StringComparison.Ordinal) ||
            type.Name.Contains("Configurator", StringComparison.Ordinal) ||
            type.Name.Contains("Mutation", StringComparison.Ordinal));
    }

    private static DnsInterfaceSettingsTruthReadResult DnsSettings(bool readSuccess) =>
        new(
            readSuccess
                ? DnsInterfaceSettingsTruthReadStatus.Success
                : DnsInterfaceSettingsTruthReadStatus.NativeFailure,
            Supported: true,
            NativeResult: readSuccess ? 0u : 5u,
            Version: 3,
            Flags: 2,
            NameServer: "1.1.1.1",
            ProfileNameServer: null,
            SupplementalSearchListPresent: false,
            SupplementalSearchListHash: null,
            DnsRicherPropertiesObservationStatus.Complete,
            Array.Empty<uint>(),
            Array.Empty<DnsServerPropertyObservation>(),
            Array.Empty<DnsServerPropertyObservation>());

    private static DnsClientServerAddressReadResult CimSuccess() =>
        new(
            new DnsClientServerAddressFamilyReadResult(
                DnsTruthSourceReadStatus.Success,
                null,
                9,
                "Ethernet",
                2,
                ExpectedIpv4Servers),
            new DnsClientServerAddressFamilyReadResult(
                DnsTruthSourceReadStatus.Success,
                null,
                9,
                "Ethernet",
                23,
                Array.Empty<string>()));

    private static AdaptersAddressesDnsReadResult AdaptersSuccess() =>
        new(
            new AdaptersAddressesDnsFamilyReadResult(
                DnsTruthSourceReadStatus.Success,
                0,
                true,
                2,
                ExpectedIpv4Servers),
            new AdaptersAddressesDnsFamilyReadResult(
                DnsTruthSourceReadStatus.Success,
                0,
                true,
                23,
                Array.Empty<string>()));

    private sealed class FakeIdentityResolver : IWindowsInterfaceIdentityResolver
    {
        private readonly WindowsInterfaceIdentity _identity;

        public FakeIdentityResolver(WindowsInterfaceIdentity identity) => _identity = identity;

        public InterfaceIdentityResolution ResolveResult { get; set; } =
            new(Ipv4DefaultRouteClearStatus.Success, Identity);

        public Queue<InterfaceIdentityResolution> Revalidations { get; } = new();

        public NetworkAdapterId? AdapterId { get; private set; }

        public int RevalidateCount { get; private set; }

        public InterfaceIdentityResolution Resolve(NetworkAdapterId adapterId)
        {
            AdapterId = adapterId;
            return ResolveResult;
        }

        public InterfaceIdentityResolution Revalidate(WindowsInterfaceIdentity identity)
        {
            RevalidateCount++;
            return Revalidations.Count > 0
                ? Revalidations.Dequeue()
                : new InterfaceIdentityResolution(Ipv4DefaultRouteClearStatus.Success, _identity);
        }
    }

    private sealed class FakeDnsSettingsReader : IDnsSettingsTruthReader
    {
        private readonly DnsInterfaceSettingsTruthReadResult _result;

        public FakeDnsSettingsReader(DnsInterfaceSettingsTruthReadResult result) =>
            _result = result;

        public int CallCount { get; private set; }

        public NetworkAdapterId? AdapterId { get; private set; }

        public DnsInterfaceSettingsTruthReadResult Read(NetworkAdapterId adapterId)
        {
            CallCount++;
            AdapterId = adapterId;
            return _result;
        }
    }

    private sealed class FakeCimReader : IWindowsDnsClientServerAddressReader
    {
        private readonly DnsClientServerAddressReadResult _result;

        public FakeCimReader(DnsClientServerAddressReadResult result) => _result = result;

        public WindowsInterfaceIdentity? Identity { get; private set; }

        public DnsClientServerAddressReadResult Read(WindowsInterfaceIdentity identity)
        {
            Identity = identity;
            return _result;
        }
    }

    private sealed class FakeAdaptersAddressesReader : IWindowsAdaptersAddressesDnsReader
    {
        private readonly AdaptersAddressesDnsReadResult _result;

        public FakeAdaptersAddressesReader(AdaptersAddressesDnsReadResult result) => _result = result;

        public WindowsInterfaceIdentity? Identity { get; private set; }

        public AdaptersAddressesDnsReadResult Read(WindowsInterfaceIdentity identity)
        {
            Identity = identity;
            return _result;
        }
    }
}
