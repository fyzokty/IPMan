using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.App.ViewModels;

/// <summary>
/// One connection. Keyed by the stable Windows adapter identity, never by list
/// index or display name, so a renamed adapter keeps its details and
/// its draft.
/// </summary>
public sealed partial class AdapterViewModel : ObservableObject
{
    private readonly IClipboardService _clipboardService;

    [ObservableProperty]
    private bool _copySucceeded;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionState = string.Empty;

    [ObservableProperty]
    private string _accessibleDescription = string.Empty;

    /// <summary>Creates a connection ViewModel for one adapter snapshot.</summary>
    public AdapterViewModel(
        NetworkAdapterSnapshot snapshot,
        IClipboardService clipboardService,
        IStaticIpv4ConfigurationValidator validator)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(clipboardService);
        ArgumentNullException.ThrowIfNull(validator);

        Id = snapshot.Id;
        _clipboardService = clipboardService;
        Details = new AdapterDetailsViewModel();
        Draft = new AdapterDraftViewModel(clipboardService, validator);

        Update(snapshot);
    }

    /// <summary>Stable Windows identity of the adapter this connection represents.</summary>
    public NetworkAdapterId Id { get; }

    /// <summary>
    /// The most recently read Windows state for quick actions. Change
    /// notification is raised only after every derived value has been updated,
    /// so observers always see a consistent connection.
    /// </summary>
    public NetworkAdapterSnapshot Snapshot { get; private set; } = null!;

    public AdapterDetailsViewModel Details { get; }

    public AdapterDraftViewModel Draft { get; }

    /// <summary>Copies the latest Windows value without changing the editable draft.</summary>
    [RelayCommand]
    private void CopyCurrentField(DraftField field)
    {
        string text = field switch
        {
            DraftField.Ipv4Address => Snapshot.Ipv4Address ?? string.Empty,
            DraftField.SubnetMask => Snapshot.SubnetMask ?? string.Empty,
            DraftField.Gateway => Snapshot.Gateway ?? string.Empty,
            DraftField.PrimaryDns => Snapshot.PrimaryDns ?? string.Empty,
            DraftField.SecondaryDns => Snapshot.SecondaryDns ?? string.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };
        try
        {
            CopySucceeded = _clipboardService.SetText(text);
        }
        catch (Exception)
        {
            CopySucceeded = false;
        }
    }

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
                "A snapshot for a different adapter cannot update this connection.",
                nameof(snapshot));
        }

        bool snapshotChanged = Snapshot != snapshot;
        Snapshot = snapshot;

        DisplayName = AdapterDisplayFormatter.FormatFriendlyName(snapshot);
        IsConnected = snapshot.IsConnected;
        ConnectionState = AdapterDisplayFormatter.FormatConnectionStatus(snapshot);
        AccessibleDescription = Strings.FormatAdapterAccessibleDescription(DisplayName, ConnectionState);

        Details.Update(snapshot);
        Draft.UpdateCurrentValues(snapshot);

        if (snapshotChanged)
        {
            OnPropertyChanged(nameof(Snapshot));
        }
    }
}
