using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using IPMan.Domain.Networking;

namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Pure mapping from raw Windows adapter data to the domain snapshot.
/// <para>
/// Mapping never invents values: anything Windows does not report is mapped to
/// <c>null</c> (or an empty string for the non-nullable MAC field).
/// </para>
/// </summary>
public static class NetworkAdapterMapper
{
    private const string UnspecifiedIpv4 = "0.0.0.0";

    /// <summary>
    /// Maps a single adapter.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the adapter has no usable Windows identity. Callers treat this
    /// as "skip this adapter", never as a discovery failure.
    /// </exception>
    public static NetworkAdapterSnapshot Map(AdapterReadModel adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);

        AdapterUnicastAddressReadModel? primaryIpv4 = SelectPrimaryIpv4(adapter.UnicastAddresses);
        Ipv4AddressValueCollection gateways = MapAllIpv4Gateways(adapter.GatewayAddresses);
        Ipv4AddressValueCollection dnsServers = MapAllIpv4DnsServers(adapter.DnsAddresses);

        return new NetworkAdapterSnapshot(
            new NetworkAdapterId(adapter.Id),
            adapter.Name,
            adapter.Description,
            FormatMacAddress(adapter.PhysicalAddress),
            adapter.OperationalStatus == OperationalStatus.Up,
            MapLinkSpeed(adapter.SpeedBitsPerSecond),
            MapConfigurationMode(adapter.IsDhcpEnabled),
            primaryIpv4?.Address,
            MapSubnetMask(primaryIpv4?.Ipv4Mask),
            gateways.Count > 0 ? gateways[0] : null,
            dnsServers.Count > 0 ? dnsServers[0] : null,
            dnsServers.Count > 1 ? dnsServers[1] : null,
            MapAllIpv4Addresses(adapter.UnicastAddresses),
            gateways,
            dnsServers);
    }

    /// <summary>
    /// Maps every IPv4 address on the adapter, preserving the order Windows
    /// reports. Nothing is dropped: the primary address is included as well.
    /// </summary>
    public static Ipv4AddressCollection MapAllIpv4Addresses(
        IReadOnlyList<AdapterUnicastAddressReadModel> unicastAddresses)
    {
        ArgumentNullException.ThrowIfNull(unicastAddresses);

        return new Ipv4AddressCollection(unicastAddresses
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
            .Where(address => TryParseIpv4(address.Address, out _))
            .Select(address => new Ipv4AddressAssignment(
                address.Address,
                MapSubnetMask(address.Ipv4Mask))));
    }

    /// <summary>Preserves every usable IPv4 default gateway in reported order.</summary>
    public static Ipv4AddressValueCollection MapAllIpv4Gateways(
        IReadOnlyList<string> gatewayAddresses)
    {
        ArgumentNullException.ThrowIfNull(gatewayAddresses);

        return new Ipv4AddressValueCollection(gatewayAddresses
            .Select(candidate => TryParseIpv4(candidate, out IPAddress? parsed) ? parsed : null)
            .Where(parsed => parsed is not null && !parsed.Equals(IPAddress.Any))
            .Select(parsed => parsed!.ToString()));
    }

    /// <summary>Preserves every IPv4 DNS server in reported order.</summary>
    public static Ipv4AddressValueCollection MapAllIpv4DnsServers(
        IReadOnlyList<string> dnsAddresses)
    {
        ArgumentNullException.ThrowIfNull(dnsAddresses);

        return new Ipv4AddressValueCollection(dnsAddresses
            .Select(candidate => TryParseIpv4(candidate, out IPAddress? parsed) ? parsed : null)
            .Where(parsed => parsed is not null)
            .Select(parsed => parsed!.ToString()));
    }

    /// <summary>
    /// Selects the address IPMan treats as the adapter's primary IPv4 address.
    /// Additional IPv4 addresses are left untouched; release 1.0 simply does not
    /// display them.
    /// </summary>
    public static AdapterUnicastAddressReadModel? SelectPrimaryIpv4(
        IReadOnlyList<AdapterUnicastAddressReadModel> unicastAddresses)
    {
        ArgumentNullException.ThrowIfNull(unicastAddresses);

        List<AdapterUnicastAddressReadModel> ipv4Addresses = unicastAddresses
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
            .Where(address => TryParseIpv4(address.Address, out _))
            .ToList();

        return ipv4Addresses.Find(IsRoutableIpv4) ?? ipv4Addresses.FirstOrDefault();
    }

    public static NetworkConfigurationMode MapConfigurationMode(bool? isDhcpEnabled) =>
        isDhcpEnabled switch
        {
            true => NetworkConfigurationMode.Dhcp,
            false => NetworkConfigurationMode.Static,
            null => NetworkConfigurationMode.Unknown
        };

    /// <summary>
    /// Formats physical address bytes as <c>AA-BB-CC-DD-EE-FF</c>.
    /// Adapters without a physical address map to an empty string.
    /// </summary>
    public static string FormatMacAddress(IReadOnlyList<byte> physicalAddress)
    {
        ArgumentNullException.ThrowIfNull(physicalAddress);

        return physicalAddress.Count == 0
            ? string.Empty
            : string.Join('-', physicalAddress.Select(part => part.ToString("X2", CultureInfo.InvariantCulture)));
    }

    private static long? MapLinkSpeed(long speedBitsPerSecond) =>
        speedBitsPerSecond > 0 ? speedBitsPerSecond : null;

    private static string? MapSubnetMask(string? mask)
    {
        if (string.IsNullOrWhiteSpace(mask))
        {
            return null;
        }

        // Windows reports 0.0.0.0 when no meaningful IPv4 mask exists.
        return string.Equals(mask, UnspecifiedIpv4, StringComparison.Ordinal) ? null : mask;
    }

    private static bool IsRoutableIpv4(AdapterUnicastAddressReadModel address)
    {
        if (!TryParseIpv4(address.Address, out IPAddress? parsed))
        {
            return false;
        }

        byte[] octets = parsed.GetAddressBytes();

        bool isLinkLocal = octets[0] == 169 && octets[1] == 254;
        bool isLoopback = octets[0] == 127;

        return !isLinkLocal && !isLoopback;
    }

    private static bool TryParseIpv4(string? value, out IPAddress parsed)
    {
        parsed = IPAddress.None;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!IPAddress.TryParse(value, out IPAddress? candidate) ||
            candidate.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        parsed = candidate;
        return true;
    }
}
