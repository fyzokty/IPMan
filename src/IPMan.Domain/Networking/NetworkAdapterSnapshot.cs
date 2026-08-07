namespace IPMan.Domain.Networking;

/// <summary>
/// Immutable point-in-time view of the network state actually read from Windows.
/// </summary>
/// <param name="Ipv4Address">
/// Primary IPv4 address: the one release 1.0 displays and edits. <c>null</c>
/// when the adapter has no IPv4 address.
/// </param>
/// <param name="SubnetMask">Subnet mask of the primary IPv4 address, when reported.</param>
/// <param name="Ipv4Addresses">
/// Every IPv4 address configured on the adapter, in the order Windows reports
/// them, including the primary one. Additional addresses are preserved so later
/// mutation logic cannot overwrite or delete configuration it never saw.
/// </param>
/// <param name="Ipv4Gateways">Every IPv4 default gateway, in Windows-reported order.</param>
/// <param name="Ipv4DnsServers">Every IPv4 DNS server, in Windows-reported order.</param>
public sealed record NetworkAdapterSnapshot(
    NetworkAdapterId Id,
    string Name,
    string Description,
    string MacAddress,
    bool IsConnected,
    long? LinkSpeedBitsPerSecond,
    NetworkConfigurationMode Mode,
    string? Ipv4Address,
    string? SubnetMask,
    string? Gateway,
    string? PrimaryDns,
    string? SecondaryDns,
    Ipv4AddressCollection Ipv4Addresses,
    Ipv4AddressValueCollection Ipv4Gateways,
    Ipv4AddressValueCollection Ipv4DnsServers)
{
    /// <summary>
    /// IPv4 addresses configured on the adapter other than <see cref="Ipv4Address"/>.
    /// </summary>
    public IReadOnlyList<Ipv4AddressAssignment> AdditionalIpv4Addresses =>
        Ipv4Addresses
            .Where(address => !string.Equals(address.Address, Ipv4Address, StringComparison.Ordinal))
            .ToArray();

    /// <summary>True when the adapter carries IPv4 addresses beyond the primary one.</summary>
    public bool HasAdditionalIpv4Addresses => AdditionalIpv4Addresses.Count > 0;
}
