using System.Globalization;
using System.Management;
using System.Net;
using System.Net.Sockets;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Exact-identity recovery read using Win32_NetworkAdapterConfiguration plus
/// the Windows DNS interface settings API for configured DNS source semantics.
/// </summary>
public sealed class WmiNetworkAdapterRecoveryReader : INetworkAdapterRecoveryReader
{
    private readonly IWmiNetworkAdapterSessionFactory _sessionFactory;
    private readonly IDnsRecoveryStateProbe _dnsStateProbe;

    public WmiNetworkAdapterRecoveryReader()
        : this(new SystemWmiNetworkAdapterSessionFactory(), new WindowsDnsRecoveryStateProbe())
    {
    }

    internal WmiNetworkAdapterRecoveryReader(
        IWmiNetworkAdapterSessionFactory sessionFactory,
        IDnsRecoveryStateProbe dnsStateProbe)
    {
        ArgumentNullException.ThrowIfNull(sessionFactory);
        ArgumentNullException.ThrowIfNull(dnsStateProbe);
        _sessionFactory = sessionFactory;
        _dnsStateProbe = dnsStateProbe;
    }

    public Task<NetworkAdapterRecoveryReadResult> ReadAsync(
        NetworkAdapterId adapterId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => ReadCore(adapterId), CancellationToken.None);
    }

    private NetworkAdapterRecoveryReadResult ReadCore(NetworkAdapterId adapterId)
    {
        WmiAdapterResolution resolution;

        try
        {
            resolution = _sessionFactory.ResolveBySettingId(adapterId.Value);
        }
        catch (ManagementException)
        {
            return new(NetworkAdapterRecoveryReadStatus.ReadFailed);
        }

        if (resolution.Status != WmiAdapterResolutionStatus.Found || resolution.Session is null)
        {
            return new(
                resolution.Status == WmiAdapterResolutionStatus.Ambiguous
                    ? NetworkAdapterRecoveryReadStatus.AdapterMappingAmbiguous
                    : NetworkAdapterRecoveryReadStatus.AdapterUnavailable);
        }

        using IWmiNetworkAdapterSession session = resolution.Session;

        try
        {
            string[] addresses = ReadStrings(session, "IPAddress");
            string[] subnets = ReadStrings(session, "IPSubnet");
            Ipv4AddressCollection ipv4Addresses = ReadIpv4Addresses(addresses, subnets);
            Ipv4GatewayRecoveryState[] gateways = ReadIpv4Gateways(
                ReadStrings(session, "DefaultIPGateway"),
                ReadUshorts(session, "GatewayCostMetric"));
            Ipv4AddressValueCollection gatewayValues = new(gateways.Select(gateway => gateway.Address));
            Ipv4AddressValueCollection dnsServers = ReadIpv4Values(
                ReadStrings(session, "DNSServerSearchOrder"));
            Ipv4AddressAssignment? primary = ipv4Addresses.Count > 0 ? ipv4Addresses[0] : null;
            NetworkConfigurationMode mode = ReadMode(session.ReadProperty("DHCPEnabled"));
            string description = ReadString(session, "Description") ?? string.Empty;
            string name = ReadString(session, "Caption") ?? description;

            NetworkAdapterSnapshot adapter = new(
                adapterId,
                name,
                description,
                ReadString(session, "MACAddress") ?? string.Empty,
                IsConnected: true,
                LinkSpeedBitsPerSecond: null,
                mode,
                primary?.Address,
                primary?.SubnetMask,
                gatewayValues.Count > 0 ? gatewayValues[0] : null,
                dnsServers.Count > 0 ? dnsServers[0] : null,
                dnsServers.Count > 1 ? dnsServers[1] : null,
                ipv4Addresses,
                gatewayValues,
                dnsServers);

            DnsRecoveryState dns = _dnsStateProbe.ReadIpv4State(adapterId.Value);
            return NetworkAdapterRecoveryReadResult.Success(
                new NetworkAdapterRecoverySnapshot(
                    adapter,
                    dns.Mode,
                    dns.ConfiguredServers,
                    gateways));
        }
        catch (ManagementException)
        {
            return new(NetworkAdapterRecoveryReadStatus.ReadFailed);
        }
    }

    private static Ipv4AddressCollection ReadIpv4Addresses(
        IReadOnlyList<string> addresses,
        IReadOnlyList<string> subnets)
    {
        List<Ipv4AddressAssignment> values = new();

        for (int index = 0; index < addresses.Count; index++)
        {
            if (!TryIpv4(addresses[index], out string address))
            {
                continue;
            }

            string? subnet = index < subnets.Count && TryIpv4(subnets[index], out string parsedSubnet)
                ? parsedSubnet
                : null;
            values.Add(new Ipv4AddressAssignment(address, subnet));
        }

        return new Ipv4AddressCollection(values);
    }

    private static Ipv4GatewayRecoveryState[] ReadIpv4Gateways(
        IReadOnlyList<string> gateways,
        IReadOnlyList<ushort> metrics)
    {
        List<Ipv4GatewayRecoveryState> values = new();

        for (int index = 0; index < gateways.Count; index++)
        {
            if (TryIpv4(gateways[index], out string address) && address != "0.0.0.0")
            {
                values.Add(new Ipv4GatewayRecoveryState(
                    address,
                    index < metrics.Count ? metrics[index] : null));
            }
        }

        return values.ToArray();
    }

    private static Ipv4AddressValueCollection ReadIpv4Values(IEnumerable<string> values) =>
        new(values
            .Select(value => TryIpv4(value, out string parsed) ? parsed : null)
            .Where(value => value is not null)
            .Select(value => value!));

    private static string[] ReadStrings(IWmiNetworkAdapterSession session, string propertyName) =>
        session.ReadProperty(propertyName) is Array values
            ? values.Cast<object?>().Select(value => value?.ToString()).Where(value => value is not null).Select(value => value!).ToArray()
            : Array.Empty<string>();

    private static ushort[] ReadUshorts(IWmiNetworkAdapterSession session, string propertyName) =>
        session.ReadProperty(propertyName) is Array values
            ? values.Cast<object?>()
                .Where(value => value is not null)
                .Select(value => Convert.ToUInt16(value, CultureInfo.InvariantCulture))
                .ToArray()
            : Array.Empty<ushort>();

    private static string? ReadString(IWmiNetworkAdapterSession session, string propertyName) =>
        session.ReadProperty(propertyName)?.ToString();

    private static NetworkConfigurationMode ReadMode(object? value) =>
        value switch
        {
            true => NetworkConfigurationMode.Dhcp,
            false => NetworkConfigurationMode.Static,
            _ => NetworkConfigurationMode.Unknown
        };

    private static bool TryIpv4(string value, out string normalized)
    {
        if (IPAddress.TryParse(value, out IPAddress? address) &&
            address.AddressFamily == AddressFamily.InterNetwork)
        {
            normalized = address.ToString();
            return true;
        }

        normalized = string.Empty;
        return false;
    }
}
