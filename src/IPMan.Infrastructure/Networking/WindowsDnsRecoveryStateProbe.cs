using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using IPMan.Domain.Networking;

namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Reads configured IPv4 DNS source and adapter-level manual servers through
/// the documented DNS interface settings API.
/// </summary>
internal sealed class WindowsDnsRecoveryStateProbe : IDnsRecoveryStateProbe
{
    private const uint NoError = 0;
    private const uint SettingsVersion1 = 1;
    private const ulong NameServerFlag = 0x0002;
    private const ulong ProfileNameServerFlag = 0x0200;
    private static readonly char[] ServerSeparators = { ',', ' ', ';' };

    public DnsRecoveryState ReadIpv4State(string adapterId)
    {
        if (!Guid.TryParse(adapterId, out Guid interfaceId))
        {
            return Unknown();
        }

        DnsInterfaceSettings settings = new() { Version = SettingsVersion1 };
        bool mustFree = false;

        try
        {
            uint result = GetInterfaceDnsSettings(interfaceId, ref settings);

            if (result != NoError)
            {
                return Unknown();
            }

            mustFree = true;
            return MapSettings(
                settings.Flags,
                Marshal.PtrToStringUni(settings.NameServer));
        }
        catch (DllNotFoundException)
        {
            return Unknown();
        }
        catch (EntryPointNotFoundException)
        {
            return Unknown();
        }
        finally
        {
            if (mustFree)
            {
                FreeInterfaceDnsSettings(ref settings);
            }
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
            return Unknown();
        }

        if (!hasAdapterServers)
        {
            return new DnsRecoveryState(
                DnsConfigurationMode.Automatic,
                Array.Empty<string>());
        }

        string[] configuredServers = ParseIpv4Servers(nameServers);
        return configuredServers.Length > 0
            ? new DnsRecoveryState(DnsConfigurationMode.Manual, configuredServers)
            : Unknown();
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

    private static DnsRecoveryState Unknown() =>
        new(DnsConfigurationMode.Unknown, Array.Empty<string>());

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint GetInterfaceDnsSettings(
        Guid interfaceId,
        ref DnsInterfaceSettings settings);

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern void FreeInterfaceDnsSettings(ref DnsInterfaceSettings settings);

    [StructLayout(LayoutKind.Sequential)]
    private struct DnsInterfaceSettings
    {
        public uint Version;
        public ulong Flags;
        public IntPtr Domain;
        public IntPtr NameServer;
        public IntPtr SearchList;
        public uint RegistrationEnabled;
        public uint RegisterAdapterName;
        public uint EnableLlmnr;
        public uint QueryAdapterName;
        public IntPtr ProfileNameServer;
    }
}
