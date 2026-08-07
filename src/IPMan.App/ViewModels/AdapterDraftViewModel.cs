using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.Domain.Networking;

namespace IPMan.App.ViewModels;

/// <summary>
/// Editable draft for one adapter. Purely presentation state: nothing here
/// touches Windows, and the domain snapshot is never modified.
/// <para>
/// A clean draft follows the current snapshot automatically. A dirty draft — one
/// the user has edited away from the values it was loaded with — is never
/// silently replaced by a refresh; only <c>Mevcut Değeri Getir</c> resets it.
/// </para>
/// </summary>
public sealed partial class AdapterDraftViewModel : ObservableObject
{
    private readonly IClipboardService _clipboardService;

    private DraftValues _baseline = DraftValues.Empty;
    private DraftValues _currentSnapshotValues = DraftValues.Empty;
    private bool _isApplyingValues;

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

    /// <summary>True when the draft differs from the values it was loaded with.</summary>
    [ObservableProperty]
    private bool _isDirty;

    public AdapterDraftViewModel(IClipboardService clipboardService)
    {
        ArgumentNullException.ThrowIfNull(clipboardService);
        _clipboardService = clipboardService;
    }

    /// <summary>
    /// Records the adapter's latest current state. A clean draft is synchronized
    /// with it; a dirty draft keeps the user's edits.
    /// </summary>
    public void UpdateCurrentValues(NetworkAdapterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _currentSnapshotValues = DraftValues.FromSnapshot(snapshot);

        if (!IsDirty)
        {
            Apply(_currentSnapshotValues);
        }
    }

    public DraftValues ToValues() =>
        new(Ipv4Address, SubnetMask, Gateway, PrimaryDns, SecondaryDns);

    public string GetFieldText(DraftField field) => field switch
    {
        DraftField.Ipv4Address => Ipv4Address,
        DraftField.SubnetMask => SubnetMask,
        DraftField.Gateway => Gateway,
        DraftField.PrimaryDns => PrimaryDns,
        DraftField.SecondaryDns => SecondaryDns,
        _ => string.Empty
    };

    /// <summary>
    /// <c>Mevcut Değeri Getir</c>: explicit reset of this adapter's draft to its
    /// latest discovered values. This is the only action that discards edits.
    /// </summary>
    [RelayCommand]
    public void GetCurrentValues() => Apply(_currentSnapshotValues);

    /// <summary>Copies a single field's draft text. Empty text clears the clipboard.</summary>
    [RelayCommand]
    public void CopyField(DraftField field) => _clipboardService.SetText(GetFieldText(field));

    partial void OnIpv4AddressChanged(string value) => UpdateDirtyState();

    partial void OnSubnetMaskChanged(string value) => UpdateDirtyState();

    partial void OnGatewayChanged(string value) => UpdateDirtyState();

    partial void OnPrimaryDnsChanged(string value) => UpdateDirtyState();

    partial void OnSecondaryDnsChanged(string value) => UpdateDirtyState();

    private void Apply(DraftValues values)
    {
        _isApplyingValues = true;

        try
        {
            Ipv4Address = values.Ipv4Address;
            SubnetMask = values.SubnetMask;
            Gateway = values.Gateway;
            PrimaryDns = values.PrimaryDns;
            SecondaryDns = values.SecondaryDns;
        }
        finally
        {
            _isApplyingValues = false;
        }

        _baseline = values;
        IsDirty = false;
    }

    private void UpdateDirtyState()
    {
        if (_isApplyingValues)
        {
            return;
        }

        // Editing a value back to what it was loaded with makes the draft clean again.
        IsDirty = ToValues() != _baseline;
    }
}
