using System.Net;
using System.Net.Sockets;
using IPMan.Domain.Networking;

namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Reads configured IPv4 DNS source and adapter-level manual servers through
/// the documented DNS interface settings API.
/// </summary>
internal sealed class WindowsDnsRecoveryStateProbe : IDnsRecoveryStateProbe
{
    private const ulong Ipv6Flag = 0x0001;
    private const ulong NameServerFlag = 0x0002;
    private const ulong SearchListFlag = 0x0004;
    private const ulong RegistrationEnabledFlag = 0x0008;
    private const ulong DomainFlag = 0x0020;
    private const ulong EnableLlmnrFlag = 0x0080;
    private const ulong QueryAdapterNameFlag = 0x0100;
    private const ulong ProfileNameServerFlag = 0x0200;
    private const ulong SourceNeutralIpv4V1Flags =
        SearchListFlag |
        RegistrationEnabledFlag |
        DomainFlag |
        EnableLlmnrFlag |
        QueryAdapterNameFlag;
    private const ulong DocumentedIpv4V1Flags =
        NameServerFlag |
        ProfileNameServerFlag |
        SourceNeutralIpv4V1Flags;
    private static readonly char[] ServerSeparators = { ',', ' ' };
    private readonly IDnsInterfaceSettingsReader _settingsReader;

    public WindowsDnsRecoveryStateProbe()
        : this(new WindowsDnsInterfaceSettingsReader())
    {
    }

    internal WindowsDnsRecoveryStateProbe(IDnsInterfaceSettingsReader settingsReader)
    {
        ArgumentNullException.ThrowIfNull(settingsReader);
        _settingsReader = settingsReader;
    }

    public DnsRecoveryState ReadIpv4State(string adapterId)
    {
        if (!Guid.TryParse(adapterId, out Guid interfaceId))
        {
            return Unknown(DnsRecoveryProbeStatus.InvalidAdapterGuid);
        }

        try
        {
            DnsInterfaceSettingsReadResult settings = _settingsReader.Read(interfaceId);

            if (settings.NativeResult != 0)
            {
                return Unknown(
                    DnsRecoveryProbeStatus.NativeCallFailed,
                    settings.NativeResult);
            }

            return MapSettings(
                settings.Flags,
                settings.NameServers,
                settings.ProfileNameServers);
        }
        catch (DllNotFoundException)
        {
            return Unknown(DnsRecoveryProbeStatus.NativeLibraryUnavailable);
        }
        catch (EntryPointNotFoundException)
        {
            return Unknown(DnsRecoveryProbeStatus.NativeEntryPointUnavailable);
        }
    }

    internal static DnsRecoveryState MapSettings(
        ulong flags,
        string? nameServers,
        string? profileNameServers = null)
    {
        bool hasAdapterServers = (flags & NameServerFlag) != 0;
        bool hasProfileServers = (flags & ProfileNameServerFlag) != 0 ||
            !string.IsNullOrWhiteSpace(profileNameServers);
        bool hasNameServerPayload = !string.IsNullOrWhiteSpace(nameServers);

        if (hasProfileServers)
        {
            // Profile/policy DNS is a distinct source that the Sprint 07 WMI
            // mutator cannot faithfully restore through SetDNSServerSearchOrder.
            return Unknown(
                DnsRecoveryProbeStatus.ProfileOrPolicyDnsDetected,
                nativeFlags: flags,
                nameServerPresent: hasNameServerPayload,
                adapterManualServerFlag: hasAdapterServers,
                profileServerFlag: true);
        }

        if ((flags & Ipv6Flag) != 0 ||
            (flags & ~DocumentedIpv4V1Flags) != 0)
        {
            return Unknown(
                DnsRecoveryProbeStatus.UnsupportedRicherDnsState,
                nativeFlags: flags,
                nameServerPresent: hasNameServerPayload,
                adapterManualServerFlag: hasAdapterServers);
        }

        Ipv4ServerParseResult parsed = ParseIpv4Servers(nameServers);

        if (!parsed.IsValid)
        {
            return Unknown(
                DnsRecoveryProbeStatus.InvalidNameServerPayload,
                nativeFlags: flags,
                nameServerPresent: hasNameServerPayload,
                adapterManualServerFlag: hasAdapterServers);
        }

        if (parsed.Servers.Length == 0)
        {
            if (hasAdapterServers)
            {
                return Unknown(
                    DnsRecoveryProbeStatus.ManualAdapterFlagWithoutUsableIpv4Servers,
                    nativeFlags: flags,
                    nameServerPresent: false,
                    adapterManualServerFlag: true);
            }

            return new DnsRecoveryState(
                DnsConfigurationMode.Automatic,
                Array.Empty<string>(),
                new DnsRecoveryProbeDiagnostic(
                    DnsRecoveryProbeStatus.Automatic,
                    NativeResult: 0,
                    NativeFlags: flags,
                    NameServerPresent: false,
                    AdapterManualServerFlag: false,
                    ProfileServerFlag: false,
                    UsableIpv4ServerCount: 0));
        }

        return new DnsRecoveryState(
            DnsConfigurationMode.Manual,
            parsed.Servers,
            new DnsRecoveryProbeDiagnostic(
                DnsRecoveryProbeStatus.Manual,
                NativeResult: 0,
                NativeFlags: flags,
                NameServerPresent: true,
                AdapterManualServerFlag: hasAdapterServers,
                ProfileServerFlag: false,
                parsed.Servers.Length));
    }

    private static Ipv4ServerParseResult ParseIpv4Servers(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new Ipv4ServerParseResult(true, Array.Empty<string>());
        }

        string[] candidates = value
            .Split(ServerSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        if (candidates.Length is < 1 or > 2)
        {
            return new Ipv4ServerParseResult(false, Array.Empty<string>());
        }

        List<string> servers = new(candidates.Length);

        foreach (string candidate in candidates)
        {
            if (!IPAddress.TryParse(candidate, out IPAddress? address) ||
                address.AddressFamily != AddressFamily.InterNetwork)
            {
                return new Ipv4ServerParseResult(false, Array.Empty<string>());
            }

            servers.Add(address.ToString());
        }

        return new Ipv4ServerParseResult(true, servers.ToArray());
    }

    private static DnsRecoveryState Unknown(
        DnsRecoveryProbeStatus status,
        uint? nativeResult = null,
        ulong nativeFlags = 0,
        bool nameServerPresent = false,
        bool adapterManualServerFlag = false,
        bool profileServerFlag = false) =>
        new(
            DnsConfigurationMode.Unknown,
            Array.Empty<string>(),
            new DnsRecoveryProbeDiagnostic(
                status,
                nativeResult,
                nativeFlags,
                nameServerPresent,
                adapterManualServerFlag,
                profileServerFlag,
                UsableIpv4ServerCount: 0));

    private sealed record Ipv4ServerParseResult(bool IsValid, string[] Servers);
}
