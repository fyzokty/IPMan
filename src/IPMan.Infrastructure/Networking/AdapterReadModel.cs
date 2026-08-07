using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace IPMan.Infrastructure.Networking;

/// <summary>
/// One unicast address as reported by Windows.
/// </summary>
/// <param name="Address">Textual address exactly as reported by the OS.</param>
/// <param name="AddressFamily">Address family reported by the OS.</param>
/// <param name="Ipv4Mask">IPv4 mask when the OS supplies one; otherwise <c>null</c>.</param>
public sealed record AdapterUnicastAddressReadModel(
    string Address,
    AddressFamily AddressFamily,
    string? Ipv4Mask);

/// <summary>
/// Raw, unmapped adapter data read from Windows.
/// <para>
/// This type exists so that mapping to <c>NetworkAdapterSnapshot</c> is a pure
/// function that can be unit tested without touching the local machine.
/// Values are reported exactly as Windows supplies them; interpretation belongs
/// to <see cref="NetworkAdapterMapper"/>.
/// </para>
/// </summary>
public sealed record AdapterReadModel(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<byte> PhysicalAddress,
    OperationalStatus OperationalStatus,
    NetworkInterfaceType InterfaceType,
    long SpeedBitsPerSecond,
    bool? IsDhcpEnabled,
    IReadOnlyList<AdapterUnicastAddressReadModel> UnicastAddresses,
    IReadOnlyList<string> GatewayAddresses,
    IReadOnlyList<string> DnsAddresses);
