using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Application.Common;
using IPMan.Application.Networking;
using IPMan.Application.Profiles;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;

namespace IPMan.App.ViewModels;

/// <summary>Independent editor. Saves documents; the host owns navigation and safe switching.</summary>
public sealed partial class ProfileEditorViewModel : ObservableObject, IDisposable
{
    public static readonly TimeSpan SuggestionDebounce = TimeSpan.FromMilliseconds(300);
    private readonly IProfileCatalog _catalog;
    private readonly IStaticIpv4ConfigurationValidator _validator;
    private readonly Ipv4AddressSuggestionService _suggestions;
    private readonly IDelayProvider _delay;
    private readonly string? _currentGateway;
    private readonly HashSet<string> _touched = new(StringComparer.Ordinal);
    private NetworkProfile? _profile;
    private CancellationTokenSource? _suggestionCancellation;
    private bool _loading;
    private bool _disposed;
    private bool _showAllErrors;
    private bool _isValid;
    private EditorValues _savedValues;

    [ObservableProperty]
    private string _name = string.Empty;
    [ObservableProperty]
    private string _deviceAddress = string.Empty;
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
    private string _nameError = string.Empty;
    [ObservableProperty]
    private string _deviceAddressError = string.Empty;
    [ObservableProperty]
    private string _ipv4AddressError = string.Empty;
    [ObservableProperty]
    private string _subnetMaskError = string.Empty;
    [ObservableProperty]
    private string _gatewayError = string.Empty;
    [ObservableProperty]
    private string _primaryDnsError = string.Empty;
    [ObservableProperty]
    private string _secondaryDnsError = string.Empty;
    [ObservableProperty]
    private string _gatewayWarning = string.Empty;
    [ObservableProperty]
    private string _saveError = string.Empty;
    [ObservableProperty]
    private bool _isMaskAutoFilled;
    [ObservableProperty]
    private bool _isAdvancedExpanded;
    [ObservableProperty]
    private bool _isBusy;
    [ObservableProperty]
    private bool _isAdapterBusy;
    [ObservableProperty]
    private bool _isStatic = true;
    [ObservableProperty]
    private Ipv4AddressSuggestion? _suggestion;

    public ProfileEditorViewModel(IProfileCatalog catalog,
        IStaticIpv4ConfigurationValidator validator, Ipv4AddressSuggestionService suggestions,
        IDelayProvider delay, NetworkProfile? profile = null, string? currentGateway = null)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _suggestions = suggestions ?? throw new ArgumentNullException(nameof(suggestions));
        _delay = delay ?? throw new ArgumentNullException(nameof(delay));
        _currentGateway = currentGateway;
        _profile = profile;
        _loading = true;
        if (profile is not null)
        {
            Name = profile.Name;
            IsStatic = profile.Mode == NetworkConfigurationMode.Static;
            Ipv4Address = profile.Ipv4Address ?? string.Empty;
            SubnetMask = profile.SubnetMask ?? string.Empty;
            Gateway = profile.Gateway ?? string.Empty;
            PrimaryDns = profile.PrimaryDns ?? string.Empty;
            SecondaryDns = profile.SecondaryDns ?? string.Empty;
        }
        _loading = false;
        _savedValues = CurrentValues();
        Validate();
    }

    public string Title => _profile is null ? Strings.ProfileEditorNewTitle : Strings.ProfileEditorEditTitle;
    public bool IsAutomatic { get => !IsStatic; set => IsStatic = !value; }
    public bool IsValid => _isValid;
    public bool HasErrors =>
        NameError.Length > 0 || DeviceAddressError.Length > 0 || Ipv4AddressError.Length > 0 ||
        SubnetMaskError.Length > 0 || GatewayError.Length > 0 || PrimaryDnsError.Length > 0 || SecondaryDnsError.Length > 0;
    public bool CanSave => !IsBusy && !HasErrors && !_disposed;
    public bool CanSaveAndSwitch => CanSave && !IsAdapterBusy;
    public bool IsDirty => CurrentValues() != _savedValues;
    public bool HasSuggestion => Suggestion is not null;
    public bool IsSuggestionVerified => Suggestion?.Status == Ipv4AddressSuggestionStatus.VerifiedFree;
    public string SuggestionStatusText => IsSuggestionVerified
        ? Strings.ProfileEditorVerified : Strings.ProfileEditorUnverified;
    public Task SuggestionTask { get; private set; } = Task.CompletedTask;

    public event EventHandler? BackRequested;
    public event EventHandler<ProfileEditorSavedEventArgs>? SaveRequested;
    public event EventHandler<ProfileEditorSavedEventArgs>? SaveAndSwitchRequested;

    public void Prefill(NetworkConfigurationMode mode, DraftValues values)
    {
        _loading = true;
        IsStatic = mode == NetworkConfigurationMode.Static;
        Ipv4Address = values.Ipv4Address;
        SubnetMask = values.SubnetMask;
        Gateway = values.Gateway;
        PrimaryDns = values.PrimaryDns;
        SecondaryDns = values.SecondaryDns;
        _loading = false;
        Validate();
        OnPropertyChanged(nameof(IsDirty));
    }

    [RelayCommand]
    private void Back()
    {
        CancelSuggestion();
        BackRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(HasSuggestion))]
    private void UseSuggestion() => Ipv4Address = Suggestion!.Address;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveOnlyAsync(CancellationToken cancellationToken) => SaveAsync(false, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanSaveAndSwitch))]
    private Task SaveAndSwitchAsync(CancellationToken cancellationToken) => SaveAsync(true, cancellationToken);

    /// <summary>Saves and raises exactly one host event only after persistence succeeds.</summary>
    public async Task SaveAsync(bool switchAfterSave, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsBusy || (switchAfterSave && IsAdapterBusy)) return;
        _showAllErrors = true;
        Validate();
        if (!IsValid) return;
        SaveError = string.Empty;
        IsBusy = true;
        NetworkProfile profile = CreateProfile();
        ProfileSaveResult result;
        try
        {
            result = await _catalog.SaveAsync(profile, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            SaveError = Strings.ProfileEditorSaveCancelled;
            return;
        }
        catch (Exception)
        {
            SaveError = Strings.ProfileEditorSaveFailed;
            return;
        }
        finally
        {
            IsBusy = false;
        }
        if (!result.IsSuccess)
        {
            SaveError = Strings.ProfileEditorSaveFailed;
            return;
        }
        _profile = result.Profile!;
        _savedValues = CurrentValues();
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(Title));
        ProfileEditorSavedEventArgs args = new(_profile);
        if (switchAfterSave) SaveAndSwitchRequested?.Invoke(this, args);
        else SaveRequested?.Invoke(this, args);
    }

    private NetworkProfile CreateProfile()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        NetworkProfile baseline = _profile ?? new(NetworkProfile.CurrentSchemaVersion,
            Guid.NewGuid().ToString("N"), Name.Trim(), null, NetworkConfigurationMode.Dhcp,
            null, null, null, null, null, false, null, now, now);
        // The editor admits GatewayOutsideSubnet as advisory; keep its normalized address.
        StaticIpv4Configuration? normalized = IsStatic
            ? _validator.Validate(new(Ipv4Address, SubnetMask, null, PrimaryDns, SecondaryDns)).NormalizedConfiguration
            : null;
        return baseline with
        {
            Name = Name.Trim(), Mode = IsStatic ? NetworkConfigurationMode.Static : NetworkConfigurationMode.Dhcp,
            Ipv4Address = normalized?.Ipv4Address, SubnetMask = normalized?.SubnetMask,
            Gateway = IsStatic && !string.IsNullOrWhiteSpace(Gateway)
                ? _validator.Validate(new(Gateway, "255.255.255.255", null, null, null)).NormalizedConfiguration?.Ipv4Address : null,
            PrimaryDns = normalized?.PrimaryDns, SecondaryDns = normalized?.SecondaryDns,
            ModifiedAtUtc = now
        };
    }

    private void Edited(string field)
    {
        if (_loading) return;
        OnPropertyChanged(nameof(IsDirty));
        _touched.Add(field);
        SaveError = string.Empty;
        Validate();
    }

    private void Validate()
    {
        string nameError = string.IsNullOrWhiteSpace(Name) ? Strings.ProfileEditorNameRequired :
            _catalog.Profiles.Any(profile => profile.ProfileId != _profile?.ProfileId &&
                string.Equals(profile.Name.Trim(), Name.Trim(), StringComparison.OrdinalIgnoreCase))
                ? Strings.ProfileEditorDuplicateName : string.Empty;
        NameError = Visible(nameof(Name)) ? nameError : string.Empty;
        StaticIpv4ValidationResult result = _validator.Validate(new(Ipv4Address, SubnetMask,
            Gateway, PrimaryDns, SecondaryDns));
        StaticIpv4ValidationError[] blocking = IsStatic ? result.Errors
            .Where(error => error.Code != StaticIpv4ValidationErrorCode.GatewayOutsideSubnet).ToArray() : [];
        string deviceError = string.Empty;
        if (IsStatic && !string.IsNullOrWhiteSpace(DeviceAddress))
        {
            IReadOnlyList<StaticIpv4ValidationError> deviceErrors = _validator.Validate(new(DeviceAddress, "255.255.255.255", null, null, null)).Errors;
            StaticIpv4ValidationError? error = deviceErrors.Count == 0 ? null : deviceErrors[0];
            if (error is not null) deviceError = Describe(error, DeviceAddress, Strings.ProfileEditorDeviceShortLabel);
        }
        DeviceAddressError = Visible(nameof(DeviceAddress)) ? deviceError : string.Empty;
        Ipv4AddressError = FieldError(blocking, StaticIpv4ConfigurationField.Ipv4Address, nameof(Ipv4Address), Ipv4Address);
        SubnetMaskError = FieldError(blocking, StaticIpv4ConfigurationField.SubnetMask, nameof(SubnetMask), SubnetMask);
        GatewayError = FieldError(blocking, StaticIpv4ConfigurationField.Gateway, nameof(Gateway), Gateway);
        PrimaryDnsError = FieldError(blocking, StaticIpv4ConfigurationField.PrimaryDns, nameof(PrimaryDns), PrimaryDns);
        SecondaryDnsError = FieldError(blocking, StaticIpv4ConfigurationField.SecondaryDns, nameof(SecondaryDns), SecondaryDns);
        if (_showAllErrors && (SubnetMaskError.Length > 0 || PrimaryDnsError.Length > 0 || SecondaryDnsError.Length > 0))
        {
            IsAdvancedExpanded = true;
        }
        GatewayWarning = IsStatic && Visible(nameof(Gateway)) && result.Errors.Any(error =>
            error.Code == StaticIpv4ValidationErrorCode.GatewayOutsideSubnet) ? Strings.ProfileEditorGatewayWarning : string.Empty;
        _isValid = nameError.Length == 0 && deviceError.Length == 0 && blocking.Length == 0;
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(HasErrors));
        NotifySaveCommands();
    }

    private bool Visible(string field) => _showAllErrors || _touched.Contains(field);

    private string FieldError(IEnumerable<StaticIpv4ValidationError> errors,
        StaticIpv4ConfigurationField field, string property, string value)
    {
        StaticIpv4ValidationError? error = errors.FirstOrDefault(error => error.Field == field);
        return Visible(property) && error is not null ? Describe(error, value) : string.Empty;
    }

    private static string Describe(StaticIpv4ValidationError error, string value, string? fieldLabel = null)
    {
        if (error.Code == StaticIpv4ValidationErrorCode.InvalidIpv4)
        {
            string example = ValidationMessageFormatter.DescribeExample(error.Field);
            string[] parts = value.Trim().Split('.');
            return parts.Length == 4 && parts.Any(part => int.TryParse(part, out int number) && (number < 0 || number > 255))
                ? Strings.FormatProfileEditorOctetError(example) : Strings.FormatProfileEditorAddressFormatError(example);
        }
        return ValidationMessageFormatter.Describe(error, fieldLabel);
    }

    private void NotifySaveCommands()
    {
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanSaveAndSwitch));
        SaveOnlyCommand.NotifyCanExecuteChanged();
        SaveAndSwitchCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value) => NotifySaveCommands();
    partial void OnIsAdapterBusyChanged(bool value) => NotifySaveCommands();
    partial void OnIsStaticChanged(bool value)
    {
        OnPropertyChanged(nameof(IsAutomatic));
        if (_loading) return;
        OnPropertyChanged(nameof(IsDirty));
        Validate();
        RestartSuggestion();
    }
    partial void OnSuggestionChanged(Ipv4AddressSuggestion? value)
    {
        OnPropertyChanged(nameof(HasSuggestion));
        OnPropertyChanged(nameof(IsSuggestionVerified));
        OnPropertyChanged(nameof(SuggestionStatusText));
        UseSuggestionCommand.NotifyCanExecuteChanged();
    }
    partial void OnNameChanged(string value)
    {
        Edited(nameof(Name));
    }
    partial void OnDeviceAddressChanged(string value)
    {
        Edited(nameof(DeviceAddress));
        if (!_loading) RestartSuggestion();
    }
    partial void OnIpv4AddressChanged(string value)
    {
        if (!_loading && string.IsNullOrWhiteSpace(SubnetMask) &&
            _validator.Validate(new(value, "255.255.255.255", null, null, null)).IsValid)
        {
            SubnetMask = "255.255.255.0";
            IsMaskAutoFilled = true;
        }
        Edited(nameof(Ipv4Address));
    }
    partial void OnSubnetMaskChanged(string value)
    {
        IsMaskAutoFilled = false;
        Edited(nameof(SubnetMask));
    }
    partial void OnGatewayChanged(string value)
    {
        Edited(nameof(Gateway));
    }
    partial void OnPrimaryDnsChanged(string value)
    {
        Edited(nameof(PrimaryDns));
    }
    partial void OnSecondaryDnsChanged(string value)
    {
        Edited(nameof(SecondaryDns));
    }

    private void RestartSuggestion()
    {
        CancelSuggestion();
        Suggestion = null;
        if (_disposed || !IsStatic || string.IsNullOrWhiteSpace(DeviceAddress) ||
            Ipv4AddressSuggestionService.GenerateCandidates(DeviceAddress, _currentGateway).Count == 0) return;
        _suggestionCancellation = new();
        SuggestionTask = UpdateSuggestionAsync(DeviceAddress, _suggestionCancellation.Token);
    }

    private async Task UpdateSuggestionAsync(string deviceAddress, CancellationToken cancellationToken)
    {
        try
        {
            await _delay.DelayAsync(SuggestionDebounce, cancellationToken);
            Ipv4AddressSuggestion? suggestion = await _suggestions.SuggestAsync(deviceAddress, _currentGateway, cancellationToken);
            if (!cancellationToken.IsCancellationRequested && !_disposed) Suggestion = suggestion;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception)
        {
            if (!cancellationToken.IsCancellationRequested && !_disposed)
            {
                IReadOnlyList<string> candidates = Ipv4AddressSuggestionService.GenerateCandidates(deviceAddress, _currentGateway);
                string? candidate = candidates.Count == 0 ? null : candidates[0];
                Suggestion = candidate is null ? null : new(candidate, Ipv4AddressSuggestionStatus.Unverified);
            }
        }
    }

    private void CancelSuggestion()
    {
        _suggestionCancellation?.Cancel();
        _suggestionCancellation?.Dispose();
        _suggestionCancellation = null;
    }

    private EditorValues CurrentValues() => new(Name, IsStatic, DeviceAddress, Ipv4Address,
        SubnetMask, Gateway, PrimaryDns, SecondaryDns);

    private readonly record struct EditorValues(string Name, bool IsStatic, string DeviceAddress,
        string Ipv4Address, string SubnetMask, string Gateway, string PrimaryDns, string SecondaryDns);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CancelSuggestion();
        NotifySaveCommands();
    }
}

public sealed class ProfileEditorSavedEventArgs(NetworkProfile profile) : EventArgs
{
    public NetworkProfile Profile { get; } = profile;
}
