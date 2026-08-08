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
    private const ulong NameServerFlag = 0x0002;
    private const ulong ProfileNameServerFlag = 0x0200;
    private static readonly char[] ServerSeparators = { ',', ' ', ';' };
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

            return MapSettings(settings.Flags, settings.NameServers);
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

    internal static DnsRecoveryState MapSettings(ulong flags, string? nameServers)
    {
        bool hasAdapterServers = (flags & NameServerFlag) != 0;
        bool hasProfileServers = (flags & ProfileNameServerFlag) != 0;

        if (hasProfileServers)
        {
            // Profile/policy DNS is a distinct source that the Sprint 07 WMI
            // mutator cannot faithfully restore through SetDNSServerSearchOrder.
            return Unknown(
                DnsRecoveryProbeStatus.ProfileOrPolicyDnsDetected,
                adapterManualServerFlag: hasAdapterServers,
                profileServerFlag: true);
        }

        if (!hasAdapterServers)
        {
            return new DnsRecoveryState(
                DnsConfigurationMode.Automatic,
                Array.Empty<string>(),
                new DnsRecoveryProbeDiagnostic(
                    DnsRecoveryProbeStatus.Automatic,
                    NativeResult: 0,
                    AdapterManualServerFlag: false,
                    ProfileServerFlag: false,
                    UsableIpv4ServerCount: 0));
        }

        string[] configuredServers = ParseIpv4Servers(nameServers);
        return configuredServers.Length > 0
            ? new DnsRecoveryState(
                DnsConfigurationMode.Manual,
                configuredServers,
                new DnsRecoveryProbeDiagnostic(
                    DnsRecoveryProbeStatus.Manual,
                    NativeResult: 0,
                    AdapterManualServerFlag: true,
                    ProfileServerFlag: false,
                    configuredServers.Length))
            : Unknown(
                DnsRecoveryProbeStatus.ManualAdapterFlagWithoutUsableIpv4Servers,
                adapterManualServerFlag: true);
    }

    private static string[] ParseIpv4Servers(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(ServerSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(candidate => IPAddress.TryParse(candidate, out IPAddress? address) &&
                    address.AddressFamily == AddressFamily.InterNetwork
                ? address.ToString()
                : null)
            .Where(address => address is not null)
            .Select(address => address!)
            .ToArray();
    }

    private static DnsRecoveryState Unknown(
        DnsRecoveryProbeStatus status,
        uint? nativeResult = null,
        bool adapterManualServerFlag = false,
        bool profileServerFlag = false) =>
        new(
            DnsConfigurationMode.Unknown,
            Array.Empty<string>(),
            new DnsRecoveryProbeDiagnostic(
                status,
                nativeResult,
                adapterManualServerFlag,
                profileServerFlag,
                UsableIpv4ServerCount: 0));
}
