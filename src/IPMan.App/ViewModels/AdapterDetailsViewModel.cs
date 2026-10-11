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
    private string _description = string.Empty;

    [ObservableProperty]
    private string _macAddress = string.Empty;

    [ObservableProperty]
    private string _linkSpeed = string.Empty;

    public void Update(NetworkAdapterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        Description = AdapterDisplayFormatter.OrUnavailable(snapshot.Description);
        MacAddress = AdapterDisplayFormatter.OrUnavailable(snapshot.MacAddress);
        LinkSpeed = AdapterDisplayFormatter.FormatLinkSpeed(
            snapshot.LinkSpeedBitsPerSecond,
            CultureInfo.CurrentCulture);
    }
}
