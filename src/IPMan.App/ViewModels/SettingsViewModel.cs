using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Application.Settings;
using IPMan.Domain.Settings;

namespace IPMan.App.ViewModels;

/// <summary>Edits user-scoped settings and persists every non-critical change immediately.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly IUserConfirmationService _confirmationService;
    private readonly string _persistenceWarningText = Strings.SettingsPersistenceWarning;
    private AppSettings _settings = AppSettings.Default;
    private bool _isLoading;

    [ObservableProperty]
    private AppTheme _theme;

    [ObservableProperty]
    private bool? _closeToTray;

    [ObservableProperty]
    private bool _notificationsEnabled;

    [ObservableProperty]
    private bool _applyProfileOnSelection;

    [ObservableProperty]
    private bool _showVirtualAdapters;

    [ObservableProperty]
    private bool _rememberWindowState;

    [ObservableProperty]
    private bool _persistenceWarning;

    /// <summary>Raised after a setting is persisted or reset so the main window can apply it live.</summary>
    public event EventHandler<AppSettings>? SettingsChanged;

    /// <summary>Creates the settings editor from the persisted user preferences.</summary>
    public SettingsViewModel(
        IAppSettingsRepository settingsRepository,
        IUserConfirmationService confirmationService)
    {
        ArgumentNullException.ThrowIfNull(settingsRepository);
        ArgumentNullException.ThrowIfNull(confirmationService);

        _settingsRepository = settingsRepository;
        _confirmationService = confirmationService;
        AppSettings settings = _settingsRepository.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        _settings = settings;
        _isLoading = true;
        Theme = AppTheme.Light;
        CloseToTray = settings.CloseToTray;
        NotificationsEnabled = settings.NotificationsEnabled;
        ApplyProfileOnSelection = settings.ApplyProfileOnSelection;
        ShowVirtualAdapters = settings.ShowVirtualAdapters;
        RememberWindowState = settings.RememberWindowState;
        PersistenceWarning = _settingsRepository.LastLoadFailed;
        _isLoading = false;
    }

    /// <summary>Gets localized text for the persistence warning.</summary>
    public string PersistenceWarningText => _persistenceWarningText;

    /// <summary>Gets or sets whether the currently supported light theme is selected.</summary>
    public bool IsLightTheme
    {
        get => Theme == AppTheme.Light;
        set
        {
            if (value)
            {
                Theme = AppTheme.Light;
            }
        }
    }

    partial void OnThemeChanged(AppTheme value)
    {
        OnPropertyChanged(nameof(IsLightTheme));
        SaveIfReady();
    }
    partial void OnCloseToTrayChanged(bool? value) => SaveIfReady();
    partial void OnNotificationsEnabledChanged(bool value) => SaveIfReady();
    partial void OnApplyProfileOnSelectionChanged(bool value) => SaveIfReady();
    partial void OnShowVirtualAdaptersChanged(bool value) => SaveIfReady();
    partial void OnRememberWindowStateChanged(bool value) => SaveIfReady();

    [RelayCommand]
    private void ResetToDefaults()
    {
        if (!_confirmationService.Confirm(new UserConfirmationRequest(
                Strings.SettingsResetTitle,
                Strings.SettingsResetMessage)))
        {
            return;
        }

        AppSettings defaults = AppSettings.Default;
        _settings = defaults;
        _isLoading = true;
        Theme = defaults.Theme;
        CloseToTray = defaults.CloseToTray;
        NotificationsEnabled = defaults.NotificationsEnabled;
        ApplyProfileOnSelection = defaults.ApplyProfileOnSelection;
        ShowVirtualAdapters = defaults.ShowVirtualAdapters;
        RememberWindowState = defaults.RememberWindowState;
        _isLoading = false;
        Save();
    }

    private void SaveIfReady()
    {
        if (!_isLoading)
        {
            Save();
        }
    }

    private void Save()
    {
        AppSettings settings = _settings with
        {
            SchemaVersion = AppSettings.CurrentSchemaVersion,
            Theme = AppTheme.Light,
            ApplyProfileOnSelection = ApplyProfileOnSelection,
            NotificationsEnabled = NotificationsEnabled,
            CloseToTray = CloseToTray,
            ShowVirtualAdapters = ShowVirtualAdapters,
            RememberWindowState = RememberWindowState,
            WindowPlacement = RememberWindowState ? _settings.WindowPlacement : null
        };
        SettingsSaveResult result = _settingsRepository
            .SaveAsync(settings, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        PersistenceWarning = !result.IsSuccess;
        _settings = settings;
        SettingsChanged?.Invoke(this, settings);
    }
}
