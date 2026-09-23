using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;

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
    private readonly IStaticIpv4ConfigurationValidator _validator;

    private DraftValues _baseline = DraftValues.Empty;
    private DraftValues _currentSnapshotValues = DraftValues.Empty;
    private StaticIpv4ValidationResult? _validationResult;
    private bool _isApplyingValues;
    private bool _isIpv4AddressTouched;
    private bool _isSubnetMaskTouched;
    private bool _isGatewayTouched;
    private bool _isPrimaryDnsTouched;
    private bool _isSecondaryDnsTouched;

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

    /// <summary>The visible validation error for the IPv4 address field.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrors))]
    private string _ipv4AddressError = string.Empty;

    /// <summary>The visible validation error for the subnet mask field.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrors))]
    private string _subnetMaskError = string.Empty;

    /// <summary>The visible validation error for the gateway field.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrors))]
    private string _gatewayError = string.Empty;

    /// <summary>The visible validation error for the primary DNS field.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrors))]
    private string _primaryDnsError = string.Empty;

    /// <summary>The visible validation error for the secondary DNS field.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrors))]
    private string _secondaryDnsError = string.Empty;

    /// <summary>True when the draft differs from the values it was loaded with.</summary>
    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isIpv4AddressDifferentFromWindows;

    [ObservableProperty]
    private bool _isSubnetMaskDifferentFromWindows;

    [ObservableProperty]
    private bool _isGatewayDifferentFromWindows;

    [ObservableProperty]
    private bool _isPrimaryDnsDifferentFromWindows;

    [ObservableProperty]
    private bool _isSecondaryDnsDifferentFromWindows;

    [ObservableProperty]
    private string _profileGuidance = string.Empty;

    /// <summary>Creates an editable draft with clipboard and validation services.</summary>
    public AdapterDraftViewModel(
        IClipboardService clipboardService,
        IStaticIpv4ConfigurationValidator validator)
    {
        ArgumentNullException.ThrowIfNull(clipboardService);
        ArgumentNullException.ThrowIfNull(validator);

        _clipboardService = clipboardService;
        _validator = validator;

        Validate();
    }

    /// <summary>True when at least one touched field currently displays an error.</summary>
    public bool HasErrors =>
        Ipv4AddressError.Length > 0 ||
        SubnetMaskError.Length > 0 ||
        GatewayError.Length > 0 ||
        PrimaryDnsError.Length > 0 ||
        SecondaryDnsError.Length > 0;

    /// <summary>True when all draft values pass validation, regardless of touched state.</summary>
    public bool IsValid => _validationResult?.IsValid == true;

    /// <summary>The normalized configuration when the current draft is valid; otherwise null.</summary>
    public StaticIpv4Configuration? ValidatedConfiguration => _validationResult?.NormalizedConfiguration;

    /// <summary>All errors from the most recent validation pass.</summary>
    public IReadOnlyList<StaticIpv4ValidationError> Errors =>
        _validationResult?.Errors ?? Array.Empty<StaticIpv4ValidationError>();

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
        else
        {
            UpdateDifferenceFlags();
            Validate();
        }
    }

    /// <summary>Loads a profile into this draft without changing Windows.</summary>
    public void LoadProfile(NetworkProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (profile.Mode == NetworkConfigurationMode.Dhcp)
        {
            Apply(DraftValues.Empty);
            ProfileGuidance = Resources.Strings.ProfileDhcpGuidance;
            return;
        }

        Apply(new DraftValues(
            profile.Ipv4Address ?? string.Empty,
            profile.SubnetMask ?? string.Empty,
            profile.Gateway ?? string.Empty,
            profile.PrimaryDns ?? string.Empty,
            profile.SecondaryDns ?? string.Empty));
        ProfileGuidance = string.Empty;
    }

    /// <summary>Creates a profile containing the current draft values.</summary>
    public NetworkProfile CreateProfile(string name, string? description, bool useDhcp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new NetworkProfile(
            NetworkProfile.CurrentSchemaVersion,
            Guid.NewGuid().ToString("N"),
            name,
            string.IsNullOrWhiteSpace(description) ? null : description,
            useDhcp ? NetworkConfigurationMode.Dhcp : NetworkConfigurationMode.Static,
            useDhcp ? null : Ipv4Address,
            useDhcp ? null : SubnetMask,
            useDhcp ? null : NullIfEmpty(Gateway),
            useDhcp ? null : NullIfEmpty(PrimaryDns),
            useDhcp ? null : NullIfEmpty(SecondaryDns),
            false,
            null,
            now,
            now);
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

    /// <summary>Makes every current validation error visible after an apply attempt.</summary>
    public void MarkAllFieldsTouched()
    {
        _isIpv4AddressTouched = true;
        _isSubnetMaskTouched = true;
        _isGatewayTouched = true;
        _isPrimaryDnsTouched = true;
        _isSecondaryDnsTouched = true;
        UpdateVisibleErrors();
    }

    /// <summary>Accepts the current draft as the clean baseline after a successful static apply.</summary>
    public void MarkApplied()
    {
        _baseline = ToValues();
        IsDirty = false;
        ResetTouchedFields();
        Validate();
    }

    partial void OnIpv4AddressChanged(string value)
    {
        if (!_isApplyingValues)
        {
            _isIpv4AddressTouched = true;
        }

        UpdateDirtyState();
        UpdateDifferenceFlags();
        Validate();
    }

    partial void OnSubnetMaskChanged(string value)
    {
        if (!_isApplyingValues)
        {
            _isSubnetMaskTouched = true;
        }

        UpdateDirtyState();
        UpdateDifferenceFlags();
        Validate();
    }

    partial void OnGatewayChanged(string value)
    {
        if (!_isApplyingValues)
        {
            _isGatewayTouched = true;
        }

        UpdateDirtyState();
        UpdateDifferenceFlags();
        Validate();
    }

    partial void OnPrimaryDnsChanged(string value)
    {
        if (!_isApplyingValues)
        {
            _isPrimaryDnsTouched = true;
        }

        UpdateDirtyState();
        UpdateDifferenceFlags();
        Validate();
    }

    partial void OnSecondaryDnsChanged(string value)
    {
        if (!_isApplyingValues)
        {
            _isSecondaryDnsTouched = true;
        }

        UpdateDirtyState();
        UpdateDifferenceFlags();
        Validate();
    }

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
        ResetTouchedFields();
        UpdateDifferenceFlags();
        Validate();
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

    private void Validate()
    {
        _validationResult = _validator.Validate(CreateConfiguration());

        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(ValidatedConfiguration));
        OnPropertyChanged(nameof(Errors));

        UpdateVisibleErrors();
    }

    private void UpdateDifferenceFlags()
    {
        IsIpv4AddressDifferentFromWindows = Ipv4Address != _currentSnapshotValues.Ipv4Address;
        IsSubnetMaskDifferentFromWindows = SubnetMask != _currentSnapshotValues.SubnetMask;
        IsGatewayDifferentFromWindows = Gateway != _currentSnapshotValues.Gateway;
        IsPrimaryDnsDifferentFromWindows = PrimaryDns != _currentSnapshotValues.PrimaryDns;
        IsSecondaryDnsDifferentFromWindows = SecondaryDns != _currentSnapshotValues.SecondaryDns;
    }

    private StaticIpv4Configuration CreateConfiguration() =>
        new(
            Ipv4Address,
            SubnetMask,
            NullIfEmpty(Gateway),
            NullIfEmpty(PrimaryDns),
            NullIfEmpty(SecondaryDns));

    private void UpdateVisibleErrors()
    {
        Ipv4AddressError = GetVisibleError(
            StaticIpv4ConfigurationField.Ipv4Address,
            _isIpv4AddressTouched);
        SubnetMaskError = GetVisibleError(
            StaticIpv4ConfigurationField.SubnetMask,
            _isSubnetMaskTouched);
        GatewayError = GetVisibleError(
            StaticIpv4ConfigurationField.Gateway,
            _isGatewayTouched);
        PrimaryDnsError = GetVisibleError(
            StaticIpv4ConfigurationField.PrimaryDns,
            _isPrimaryDnsTouched);
        SecondaryDnsError = GetVisibleError(
            StaticIpv4ConfigurationField.SecondaryDns,
            _isSecondaryDnsTouched);
    }

    private string GetVisibleError(StaticIpv4ConfigurationField field, bool isTouched) =>
        isTouched
            ? string.Join(
                Environment.NewLine,
                Errors.Where(error => error.Field == field).Select(ValidationMessageFormatter.Describe))
            : string.Empty;

    private void ResetTouchedFields()
    {
        _isIpv4AddressTouched = false;
        _isSubnetMaskTouched = false;
        _isGatewayTouched = false;
        _isPrimaryDnsTouched = false;
        _isSecondaryDnsTouched = false;
    }

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
}
