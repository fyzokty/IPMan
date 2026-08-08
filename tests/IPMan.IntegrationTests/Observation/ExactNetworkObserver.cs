using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Globalization;
using IPMan.Domain.Networking;
using IPMan.IntegrationTests.Harness;

namespace IPMan.IntegrationTests.Observation;

public sealed class ExactNetworkObserver
{
    public static NetworkObservation Read(NetworkAdapterId adapterId)
    {
        NetworkInterface? target = ExactIdentityMatcher.FindUnique(
            NetworkInterface.GetAllNetworkInterfaces(),
            adapter => adapter.Id,
            adapterId.Value);

        if (target is null)
        {
            throw new InvalidOperationException(
                $"Exact adapter {adapterId.Value} was unavailable or ambiguous; refusing observation and mutation.");
        }

        IPInterfaceProperties properties = target.GetIPProperties();
        bool ipv6Enabled = target.Supports(NetworkInterfaceComponent.IPv6);
        uint? ipv6InterfaceIndex = ipv6Enabled ? ReadIpv6InterfaceIndex(properties) : null;
        IpAddressObservation[] ipv4 = properties.UnicastAddresses
            .Where(address => address.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(address => new IpAddressObservation(
                address.Address.ToString(),
                ReadPrefixLength(address),
                ReadIpv4Mask(address)))
            .ToArray();
        IpAddressObservation[] ipv6 = properties.UnicastAddresses
            .Where(address => address.Address.AddressFamily == AddressFamily.InterNetworkV6)
            .Select(address => new IpAddressObservation(
                address.Address.ToString(),
                ReadPrefixLength(address),
                null))
            .ToArray();

        return new NetworkObservation(
            DateTimeOffset.UtcNow,
            adapterId.Value,
            target.Name,
            target.Description,
            FormatMac(target.GetPhysicalAddress()),
            ipv6Enabled,
            ipv4,
            ReadAddresses(properties.GatewayAddresses.Select(gateway => gateway.Address), AddressFamily.InterNetwork),
            ReadAddresses(properties.DnsAddresses, AddressFamily.InterNetwork),
            ipv6,
            ReadAddresses(properties.GatewayAddresses.Select(gateway => gateway.Address), AddressFamily.InterNetworkV6),
            ReadAddresses(properties.DnsAddresses, AddressFamily.InterNetworkV6),
            ipv6Enabled
                ? Ipv6RouteTableObserver.Read(ipv6InterfaceIndex)
                : Ipv6RouteTableObserver.NotApplicable(),
            DnsSettingsObserver.Read(adapterId));
    }

    private static string[] ReadAddresses(
        IEnumerable<System.Net.IPAddress> addresses,
        AddressFamily family) =>
        addresses
            .Where(address => address.AddressFamily == family)
            .Select(address => address.ToString())
            .ToArray();

    private static int? ReadPrefixLength(UnicastIPAddressInformation address)
    {
        try
        {
            return address.PrefixLength;
        }
        catch (PlatformNotSupportedException)
        {
            return null;
        }
    }

    private static string? ReadIpv4Mask(UnicastIPAddressInformation address)
    {
        try
        {
            return address.IPv4Mask?.ToString();
        }
        catch (PlatformNotSupportedException)
        {
            return null;
        }
        catch (NotImplementedException)
        {
            return null;
        }
    }

    private static uint? ReadIpv6InterfaceIndex(IPInterfaceProperties properties)
    {
        try
        {
            return checked((uint)properties.GetIPv6Properties().Index);
        }
        catch (NetworkInformationException)
        {
            return null;
        }
        catch (PlatformNotSupportedException)
        {
            return null;
        }
    }

    private static string FormatMac(PhysicalAddress address) =>
        string.Join(
            '-',
            address.GetAddressBytes().Select(value => value.ToString("X2", CultureInfo.InvariantCulture)));
}
