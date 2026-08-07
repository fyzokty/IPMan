using IPMan.App.Resources;
using IPMan.Domain.Networking;

namespace IPMan.App.ViewModels;

/// <summary>
/// Read-only presentation projection of a <see cref="NetworkAdapterSnapshot"/>
/// for the temporary Sprint 04 diagnostic view.
/// </summary>
public sealed class AdapterDiagnosticItem
{
    private const long BitsPerMegabit = 1_000_000;

    private AdapterDiagnosticItem(
        string name,
        string description,
        string connectionState,
        string mode,
        string ipv4Address,
        string subnetMask,
        string gateway,
        string primaryDns,
        string secondaryDns,
        string additionalIpv4Addresses,
        string macAddress,
        string linkSpeed)
    {
        Name = name;
        Description = description;
        ConnectionState = connectionState;
        Mode = mode;
        Ipv4Address = ipv4Address;
        SubnetMask = subnetMask;
        Gateway = gateway;
        PrimaryDns = primaryDns;
        SecondaryDns = secondaryDns;
        AdditionalIpv4Addresses = additionalIpv4Addresses;
        MacAddress = macAddress;
        LinkSpeed = linkSpeed;
    }

    public string Name { get; }

    public string Description { get; }

    public string ConnectionState { get; }

    public string Mode { get; }

    public string Ipv4Address { get; }

    public string SubnetMask { get; }

    public string Gateway { get; }

    public string PrimaryDns { get; }

    public string SecondaryDns { get; }

    /// <summary>IPv4 addresses configured on the adapter besides the primary one.</summary>
    public string AdditionalIpv4Addresses { get; }

    public string MacAddress { get; }

    public string LinkSpeed { get; }

    public static AdapterDiagnosticItem FromSnapshot(NetworkAdapterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new AdapterDiagnosticItem(
            snapshot.Name,
            snapshot.Description,
            snapshot.IsConnected ? Strings.ConnectionStateConnected : Strings.ConnectionStateDisconnected,
            DescribeMode(snapshot.Mode),
            OrUnavailable(snapshot.Ipv4Address),
            OrUnavailable(snapshot.SubnetMask),
            OrUnavailable(snapshot.Gateway),
            OrUnavailable(snapshot.PrimaryDns),
            OrUnavailable(snapshot.SecondaryDns),
            DescribeAdditionalAddresses(snapshot.AdditionalIpv4Addresses),
            OrUnavailable(snapshot.MacAddress),
            DescribeLinkSpeed(snapshot.LinkSpeedBitsPerSecond));
    }

    private static string DescribeAdditionalAddresses(
        IReadOnlyList<Ipv4AddressAssignment> additionalAddresses) =>
        additionalAddresses.Count == 0
            ? Strings.ValueUnavailable
            : string.Join(", ", additionalAddresses.Select(address => address.Address));

    private static string DescribeMode(NetworkConfigurationMode mode) => mode switch
    {
        NetworkConfigurationMode.Dhcp => Strings.ModeDhcp,
        NetworkConfigurationMode.Static => Strings.ModeStatic,
        _ => Strings.ModeUnknown
    };

    private static string DescribeLinkSpeed(long? linkSpeedBitsPerSecond) =>
        linkSpeedBitsPerSecond is null
            ? Strings.ValueUnavailable
            : Strings.FormatLinkSpeedMegabitsPerSecond(linkSpeedBitsPerSecond.Value / BitsPerMegabit);

    private static string OrUnavailable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Strings.ValueUnavailable : value;
}
