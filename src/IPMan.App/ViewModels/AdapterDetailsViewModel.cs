using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using IPMan.App.Presentation;
using IPMan.Domain.Networking;

namespace IPMan.App.ViewModels;

/// <summary>
/// Read-only projection of the latest verified discovery snapshot for one
/// adapter. Every value is already formatted for display; unavailable values use
/// the shared placeholder rather than a fabricated address.
/// </summary>
public sealed partial class AdapterDetailsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _connectionState = string.Empty;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _macAddress = string.Empty;

    [ObservableProperty]
    private string _ipv4Address = string.Empty;

    [ObservableProperty]
    private string _subnetMask = string.Empty;

    [ObservableProperty]
    private string _gateway = string.Empty;

    [ObservableProperty]
    private string _primaryDns = string.Empty;

    [ObservableProperty]
    private string _secondaryDns = string.Empty;

    [ObservableProperty]
    private string _configurationMode = string.Empty;

    [ObservableProperty]
    private string _linkSpeed = string.Empty;

    [ObservableProperty]
    private string _additionalIpv4Addresses = string.Empty;

    [ObservableProperty]
    private bool _hasAdditionalIpv4Addresses;

    /// <summary>
    /// Additional IPv4 addresses as individual entries, so the view can list
    /// them instead of only showing the joined text.
    /// </summary>
    public ObservableCollection<string> AdditionalIpv4AddressList { get; } = new();

    public void Update(NetworkAdapterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        IsConnected = snapshot.IsConnected;
        ConnectionState = AdapterDisplayFormatter.FormatConnectionState(snapshot.IsConnected);
        Name = snapshot.Name;
        Description = AdapterDisplayFormatter.OrUnavailable(snapshot.Description);
        MacAddress = AdapterDisplayFormatter.OrUnavailable(snapshot.MacAddress);
        Ipv4Address = AdapterDisplayFormatter.OrUnavailable(snapshot.Ipv4Address);
        SubnetMask = AdapterDisplayFormatter.OrUnavailable(snapshot.SubnetMask);
        Gateway = AdapterDisplayFormatter.OrUnavailable(snapshot.Gateway);
        PrimaryDns = AdapterDisplayFormatter.OrUnavailable(snapshot.PrimaryDns);
        SecondaryDns = AdapterDisplayFormatter.OrUnavailable(snapshot.SecondaryDns);
        ConfigurationMode = AdapterDisplayFormatter.FormatConfigurationMode(snapshot.Mode);
        LinkSpeed = AdapterDisplayFormatter.FormatLinkSpeed(
            snapshot.LinkSpeedBitsPerSecond,
            CultureInfo.CurrentCulture);

        IReadOnlyList<Ipv4AddressAssignment> additional = snapshot.AdditionalIpv4Addresses;
        AdditionalIpv4Addresses = AdapterDisplayFormatter.FormatAdditionalIpv4Addresses(additional);
        HasAdditionalIpv4Addresses = additional.Count > 0;

        AdditionalIpv4AddressList.Clear();

        foreach (Ipv4AddressAssignment address in additional)
        {
            AdditionalIpv4AddressList.Add(address.ToString());
        }
    }
}
