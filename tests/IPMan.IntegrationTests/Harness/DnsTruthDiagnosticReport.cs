using System.Globalization;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using IPMan.IntegrationTests.Observation;

namespace IPMan.IntegrationTests.Harness;

internal interface IDnsSettingsTruthReader
{
    DnsInterfaceSettingsTruthReadResult Read(NetworkAdapterId adapterId);
}

internal sealed class DnsSettingsTruthReader : IDnsSettingsTruthReader
{
    public DnsInterfaceSettingsTruthReadResult Read(NetworkAdapterId adapterId) =>
        DnsInterfaceSettingsTruthReadResult.FromObservation(
            DnsSettingsObserver.Read(adapterId));
}

internal sealed class DnsTruthDiagnosticCapture
{
    private readonly IWindowsInterfaceIdentityResolver _identityResolver;
    private readonly IDnsSettingsTruthReader _dnsSettingsReader;
    private readonly IWindowsDnsClientServerAddressReader _cimReader;
    private readonly IWindowsAdaptersAddressesDnsReader _adaptersAddressesReader;

    public DnsTruthDiagnosticCapture()
        : this(
            new SystemWindowsInterfaceIdentityResolver(),
            new DnsSettingsTruthReader(),
            new WindowsDnsClientServerAddressReader(),
            new WindowsAdaptersAddressesDnsReader())
    {
    }

    internal DnsTruthDiagnosticCapture(
        IWindowsInterfaceIdentityResolver identityResolver,
        IDnsSettingsTruthReader dnsSettingsReader,
        IWindowsDnsClientServerAddressReader cimReader,
        IWindowsAdaptersAddressesDnsReader adaptersAddressesReader)
    {
        ArgumentNullException.ThrowIfNull(identityResolver);
        ArgumentNullException.ThrowIfNull(dnsSettingsReader);
        ArgumentNullException.ThrowIfNull(cimReader);
        ArgumentNullException.ThrowIfNull(adaptersAddressesReader);
        _identityResolver = identityResolver;
        _dnsSettingsReader = dnsSettingsReader;
        _cimReader = cimReader;
        _adaptersAddressesReader = adaptersAddressesReader;
    }

    public DnsTruthDiagnosticReport Capture(NetworkAdapterId adapterId)
    {
        InterfaceIdentityResolution resolution = _identityResolver.Resolve(adapterId);

        if (!resolution.IsSuccess)
        {
            return DnsTruthDiagnosticReport.IdentityFailure(
                adapterId,
                resolution.Status.ToString(),
                resolution.TechnicalCode);
        }

        WindowsInterfaceIdentity identity = resolution.Identity!;
        InterfaceIdentityResolution beforeRead = _identityResolver.Revalidate(identity);

        if (!beforeRead.IsSuccess)
        {
            return DnsTruthDiagnosticReport.IdentityFailure(
                adapterId,
                beforeRead.Status.ToString(),
                beforeRead.TechnicalCode,
                identity.InterfaceIndex);
        }

        DnsInterfaceSettingsTruthReadResult dnsSettings =
            _dnsSettingsReader.Read(adapterId);
        DnsClientServerAddressReadResult cim = _cimReader.Read(identity);
        AdaptersAddressesDnsReadResult adaptersAddresses =
            _adaptersAddressesReader.Read(identity);
        InterfaceIdentityResolution afterRead = _identityResolver.Revalidate(identity);

        return new DnsTruthDiagnosticReport(
            adapterId,
            ExactIdentityResolved: afterRead.IsSuccess,
            IdentityStatus: afterRead.Status.ToString(),
            afterRead.TechnicalCode,
            identity.InterfaceIndex,
            dnsSettings,
            cim,
            adaptersAddresses);
    }
}

internal sealed record DnsTruthDiagnosticReport(
    NetworkAdapterId AdapterId,
    bool ExactIdentityResolved,
    string IdentityStatus,
    uint? IdentityTechnicalCode,
    uint? InterfaceIndex,
    DnsInterfaceSettingsTruthReadResult? DnsSettings,
    DnsClientServerAddressReadResult? Cim,
    AdaptersAddressesDnsReadResult? AdaptersAddresses)
{
    public static bool MutationPerformed => false;

    public bool AllSourcesReadSuccessfully =>
        ExactIdentityResolved &&
        DnsSettings?.ReadSuccess == true &&
        Cim?.Ipv4.ReadSuccess == true &&
        Cim.Ipv6.ReadSuccess &&
        AdaptersAddresses?.Ipv4.ReadSuccess == true &&
        AdaptersAddresses.Ipv6.ReadSuccess;

    public static DnsTruthDiagnosticReport IdentityFailure(
        NetworkAdapterId adapterId,
        string status,
        uint? technicalCode,
        uint? interfaceIndex = null) =>
        new(
            adapterId,
            ExactIdentityResolved: false,
            status,
            technicalCode,
            interfaceIndex,
            DnsSettings: null,
            Cim: null,
            AdaptersAddresses: null);

    public IReadOnlyList<string> FormatLines()
    {
        List<string> lines = new()
        {
            "DnsTruthDiagnostic: Enabled",
            $"ExactAdapterId: {AdapterId.Value}",
            $"ExactIdentityResolved: {ExactIdentityResolved}",
            $"IdentityStatus: {IdentityStatus}",
            $"IdentityTechnicalCode: {FormatNumber(IdentityTechnicalCode)}",
            $"InterfaceIndex: {FormatNumber(InterfaceIndex)}",
            string.Empty,
            "GetInterfaceDnsSettings:"
        };

        AppendDnsSettings(lines, DnsSettings);
        lines.Add(string.Empty);
        lines.Add("DnsClientCimIPv4:");
        AppendCim(lines, Cim?.Ipv4, WindowsDnsClientServerAddressReader.AfInet);
        lines.Add(string.Empty);
        lines.Add("DnsClientCimIPv6:");
        AppendCim(lines, Cim?.Ipv6, WindowsDnsClientServerAddressReader.AfInet6);
        lines.Add(string.Empty);
        lines.Add("GetAdaptersAddressesIPv4:");
        AppendAdaptersAddresses(
            lines,
            AdaptersAddresses?.Ipv4,
            WindowsAdaptersAddressesDnsReader.AfInet);
        lines.Add(string.Empty);
        lines.Add("GetAdaptersAddressesIPv6:");
        AppendAdaptersAddresses(
            lines,
            AdaptersAddresses?.Ipv6,
            WindowsAdaptersAddressesDnsReader.AfInet6);
        lines.Add(string.Empty);
        lines.Add($"MutationPerformed: {MutationPerformed}");
        return lines;
    }

    private static void AppendDnsSettings(
        List<string> lines,
        DnsInterfaceSettingsTruthReadResult? settings)
    {
        lines.Add($"  ReadSuccess: {settings?.ReadSuccess == true}");
        lines.Add($"  Status: {settings?.Status.ToString() ?? "NotReadDueToIdentityFailure"}");
        lines.Add($"  Supported: {settings?.Supported == true}");
        lines.Add($"  NativeResult: {FormatNumber(settings?.NativeResult)}");
        lines.Add($"  Version: {FormatNumber(settings?.Version)}");
        lines.Add($"  Flags: {(settings is null ? "N/A" : $"0x{settings.Flags:X}")}");
        lines.Add($"  NameServer: {FormatText(settings?.NameServer)}");
        lines.Add($"  ProfileNameServer: {FormatText(settings?.ProfileNameServer)}");
        lines.Add($"  SupplementalSearchListPresent: {settings?.SupplementalSearchListPresent == true}");
        lines.Add($"  SupplementalSearchListHash: {FormatText(settings?.SupplementalSearchListHash)}");
        lines.Add($"  RicherPropertiesStatus: {settings?.RicherPropertiesStatus.ToString() ?? "NotRead"}");
        lines.Add($"  UnsupportedPropertyCount: {settings?.UnsupportedPropertyTypes.Count ?? 0}");
        lines.Add($"  UnsupportedPropertyTypes: {FormatNumbers(settings?.UnsupportedPropertyTypes)}");
        lines.Add($"  ServerPropertiesCount: {settings?.ServerProperties.Count ?? 0}");
        lines.Add($"  ServerProperties: {FormatServerProperties(settings?.ServerProperties)}");
        lines.Add($"  ProfileServerPropertiesCount: {settings?.ProfileServerProperties.Count ?? 0}");
        lines.Add($"  ProfileServerProperties: {FormatServerProperties(settings?.ProfileServerProperties)}");
    }

    private static void AppendCim(
        List<string> lines,
        DnsClientServerAddressFamilyReadResult? result,
        ushort family)
    {
        lines.Add($"  ReadSuccess: {result?.ReadSuccess == true}");
        lines.Add($"  Status: {result?.Status.ToString() ?? "NotReadDueToIdentityFailure"}");
        lines.Add($"  TechnicalCode: {FormatNumber(result?.TechnicalCode)}");
        lines.Add($"  InterfaceIndex: {FormatNumber(result?.InterfaceIndex)}");
        lines.Add($"  InterfaceAlias: {FormatText(result?.InterfaceAlias)}");
        lines.Add($"  AddressFamily: {result?.AddressFamily ?? family}");
        lines.Add($"  ServerCount: {result?.Servers.Count ?? 0}");
        lines.Add($"  Servers: {FormatServers(result?.Servers)}");
    }

    private static void AppendAdaptersAddresses(
        List<string> lines,
        AdaptersAddressesDnsFamilyReadResult? result,
        ushort family)
    {
        lines.Add($"  ReadSuccess: {result?.ReadSuccess == true}");
        lines.Add($"  Status: {result?.Status.ToString() ?? "NotReadDueToIdentityFailure"}");
        lines.Add($"  NativeResult: {FormatNumber(result?.NativeResult)}");
        lines.Add($"  ExactIdentityMatches: {result?.ExactIdentityMatches == true}");
        lines.Add($"  AddressFamily: {result?.AddressFamily ?? family}");
        lines.Add($"  ServerCount: {result?.Servers.Count ?? 0}");
        lines.Add($"  Servers: {FormatServers(result?.Servers)}");
    }

    private static string FormatServers(IReadOnlyList<string>? servers) =>
        servers is null ? "[]" : $"[{string.Join(", ", servers)}]";

    private static string FormatNumbers(IReadOnlyList<uint>? values) =>
        values is null ? "[]" : $"[{string.Join(", ", values)}]";

    private static string FormatServerProperties(
        IReadOnlyList<DnsServerPropertyObservation>? properties) =>
        properties is null
            ? "[]"
            : "[" + string.Join(
                ", ",
                properties.Select(property =>
                    $"Version={property.Version};ServerIndex={property.ServerIndex};" +
                    $"Type={property.Type};PayloadSupported={property.PayloadSupported};" +
                    $"DohFlags={FormatNumber(property.DohFlags)};" +
                    $"DohTemplatePresent={property.DohTemplatePresent};" +
                    $"DohTemplateHash={FormatText(property.DohTemplateHash)}")) + "]";

    private static string FormatNumber<T>(T? value)
        where T : struct, IFormattable =>
        value.HasValue
            ? value.Value.ToString(null, CultureInfo.InvariantCulture)
            : "N/A";

    private static string FormatText(string? value) => value ?? "<null>";
}
