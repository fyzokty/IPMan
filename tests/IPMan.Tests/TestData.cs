using System.Net.NetworkInformation;
using System.Net.Sockets;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;
using IPMan.Infrastructure.Networking;

namespace IPMan.Tests;

/// <summary>
/// Builders keeping the intent of each test visible instead of long argument lists.
/// </summary>
internal static class TestData
{
    private static readonly byte[] DefaultPhysicalAddress = { 0x00, 0x1A, 0x2B, 0x3C, 0x4D, 0x5E };

    private static readonly string[] DefaultGateways = { "192.168.1.1" };

    private static readonly string[] DefaultDnsServers = { "192.168.1.1", "8.8.8.8" };

    public static AdapterReadModel Adapter(
        string id = "{11111111-2222-3333-4444-555555555555}",
        string name = "Ethernet",
        string description = "Contoso Gigabit Adapter",
        byte[]? physicalAddress = null,
        OperationalStatus operationalStatus = OperationalStatus.Up,
        NetworkInterfaceType interfaceType = NetworkInterfaceType.Ethernet,
        long speedBitsPerSecond = 1_000_000_000,
        bool? isDhcpEnabled = true,
        IReadOnlyList<AdapterUnicastAddressReadModel>? unicastAddresses = null,
        IReadOnlyList<string>? gatewayAddresses = null,
        IReadOnlyList<string>? dnsAddresses = null) =>
        new(
            id,
            name,
            description,
            physicalAddress ?? DefaultPhysicalAddress,
            operationalStatus,
            interfaceType,
            speedBitsPerSecond,
            isDhcpEnabled,
            unicastAddresses ?? new[] { Ipv4("192.168.1.50", "255.255.255.0") },
            gatewayAddresses ?? DefaultGateways,
            dnsAddresses ?? DefaultDnsServers);

    public static AdapterUnicastAddressReadModel Ipv4(string address, string? mask = "255.255.255.0") =>
        new(address, AddressFamily.InterNetwork, mask);

    public static AdapterUnicastAddressReadModel Ipv6(string address) =>
        new(address, AddressFamily.InterNetworkV6, null);

    public static NetworkAdapterSnapshot Snapshot(
        string id = "{11111111-2222-3333-4444-555555555555}",
        string name = "Ethernet",
        bool isConnected = true,
        string? ipv4Address = "192.168.1.50",
        Ipv4AddressCollection? ipv4Addresses = null,
        string description = "Contoso Gigabit Adapter",
        string macAddress = "00-1A-2B-3C-4D-5E",
        long? linkSpeedBitsPerSecond = 1_000_000_000,
        NetworkConfigurationMode mode = NetworkConfigurationMode.Dhcp,
        string? subnetMask = "255.255.255.0",
        string? gateway = "192.168.1.1",
        string? primaryDns = "192.168.1.1",
        string? secondaryDns = null,
        Ipv4AddressValueCollection? ipv4Gateways = null,
        Ipv4AddressValueCollection? ipv4DnsServers = null) =>
        new(
            new NetworkAdapterId(id),
            name,
            description,
            macAddress,
            isConnected,
            linkSpeedBitsPerSecond,
            mode,
            ipv4Address,
            subnetMask,
            gateway,
            primaryDns,
            secondaryDns,
            ipv4Addresses ?? DefaultIpv4Addresses(ipv4Address),
            ipv4Gateways ?? Values(gateway),
            ipv4DnsServers ?? Values(primaryDns, secondaryDns));

    public static NetworkProfile Profile(
        int schemaVersion = NetworkProfile.CurrentSchemaVersion,
        string profileId = "profile-1",
        string name = "PLC",
        string? description = "Factory controller",
        NetworkConfigurationMode mode = NetworkConfigurationMode.Static,
        string? ipv4Address = "192.168.1.50",
        string? subnetMask = "255.255.255.0",
        string? gateway = "192.168.1.1",
        string? primaryDns = "1.1.1.1",
        string? secondaryDns = "8.8.8.8",
        bool isFavorite = false,
        string? originAdapterName = "Ethernet",
        DateTimeOffset? createdAtUtc = null,
        DateTimeOffset? modifiedAtUtc = null) =>
        new(
            schemaVersion,
            profileId,
            name,
            description,
            mode,
            ipv4Address,
            subnetMask,
            gateway,
            primaryDns,
            secondaryDns,
            isFavorite,
            originAdapterName,
            createdAtUtc ?? new DateTimeOffset(2026, 8, 20, 9, 0, 0, TimeSpan.Zero),
            modifiedAtUtc ?? new DateTimeOffset(2026, 8, 20, 9, 30, 0, TimeSpan.Zero));

    private static Ipv4AddressCollection DefaultIpv4Addresses(string? ipv4Address) =>
        ipv4Address is null
            ? Ipv4AddressCollection.Empty
            : new Ipv4AddressCollection(new[] { new Ipv4AddressAssignment(ipv4Address, "255.255.255.0") });

    private static Ipv4AddressValueCollection Values(params string?[] values) =>
        new(values.Where(value => value is not null).Select(value => value!));
}
