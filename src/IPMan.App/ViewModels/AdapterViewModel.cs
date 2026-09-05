using CommunityToolkit.Mvvm.ComponentModel;
using IPMan.App.Presentation;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.App.ViewModels;

/// <summary>
/// One adapter tab. Keyed by the stable Windows adapter identity, never by tab
/// index or display name, so a renamed adapter keeps its tab, its details and
/// its draft.
/// </summary>
public sealed partial class AdapterViewModel : ObservableObject
{
    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionState = string.Empty;

    /// <summary>
    /// Shape-based tab marker. Deliberately not colour-only: the tab also carries
    /// <see cref="AccessibleDescription"/> for assistive technology.
    /// </summary>
    [ObservableProperty]
    private string _connectionMarker = string.Empty;

    [ObservableProperty]
    private string _accessibleDescription = string.Empty;

    /// <summary>Creates a tab ViewModel for one adapter snapshot.</summary>
    public AdapterViewModel(
        NetworkAdapterSnapshot snapshot,
        IClipboardService clipboardService,
        IStaticIpv4ConfigurationValidator validator)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(clipboardService);
        ArgumentNullException.ThrowIfNull(validator);

        Id = snapshot.Id;
        Details = new AdapterDetailsViewModel();
        Draft = new AdapterDraftViewModel(clipboardService, validator);

        Update(snapshot);
    }

    /// <summary>Stable Windows identity of the adapter this tab represents.</summary>
    public NetworkAdapterId Id { get; }

    public AdapterDetailsViewModel Details { get; }

    public AdapterDraftViewModel Draft { get; }

    /// <summary>
    /// Applies a newly discovered snapshot. The read-only details always follow
    /// Windows; the draft decides for itself whether it may be synchronized.
    /// </summary>
    public void Update(NetworkAdapterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.Id != Id)
        {
            throw new ArgumentException(
                "A snapshot for a different adapter cannot update this tab.",
                nameof(snapshot));
        }

        DisplayName = snapshot.Name;
        IsConnected = snapshot.IsConnected;
        ConnectionState = AdapterDisplayFormatter.FormatConnectionState(snapshot.IsConnected);
        ConnectionMarker = snapshot.IsConnected ? "●" : "○";
        AccessibleDescription = $"{snapshot.Name} — {ConnectionState}";

        Details.Update(snapshot);
        Draft.UpdateCurrentValues(snapshot);
    }
}
