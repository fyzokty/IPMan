using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.Services;
using IPMan.Application.Settings;
using IPMan.Domain.Settings;

namespace IPMan.App.ViewModels;

/// <summary>Edits user-scoped settings and persists every non-critical change immediately.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly IUserConfirmationService _confirmationService;
    private readonly WindowsThemeService _themeService;
    private readonly string _persistenceWarningText = Strings.SettingsPersistenceWarning;
    private AppSettings _settings = AppSettings.Default;
    private bool _isLoading;

    [ObservableProperty]
    private AppTheme _theme;

    [ObservableProperty]
    private bool? _closeToTray;

    [ObservableProperty]
    private NotificationMode _notificationMode;

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
        IUserConfirmationService confirmationService,
        WindowsThemeService themeService)
    {
        ArgumentNullException.ThrowIfNull(settingsRepository);
        ArgumentNullException.ThrowIfNull(confirmationService);
        ArgumentNullException.ThrowIfNull(themeService);

        _settingsRepository = settingsRepository;
        _confirmationService = confirmationService;
        _themeService = themeService;
        AppSettings settings = _settingsRepository.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        _settings = settings;
        _isLoading = true;
        Theme = settings.Theme;
        CloseToTray = settings.CloseToTray;
        NotificationMode = settings.NotificationMode;
        ApplyProfileOnSelection = settings.ApplyProfileOnSelection;
        ShowVirtualAdapters = settings.ShowVirtualAdapters;
        RememberWindowState = settings.RememberWindowState;
        PersistenceWarning = _settingsRepository.LastLoadFailed;
        _isLoading = false;
    }

    /// <summary>Gets localized text for the persistence warning.</summary>
    public string PersistenceWarningText => _persistenceWarningText;

    /// <summary>Stores the one-time notification prompt response.</summary>
    public void CompleteNotificationPrompt(bool accepted)
    {
        _settings = _settings with { NotificationPromptShown = true };
        NotificationMode = accepted ? NotificationMode.WhenUnfocused : NotificationMode.Disabled;
        Save();
    }

    /// <summary>Gets or sets whether the light theme is selected.</summary>
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

    /// <summary>Gets or sets whether the dark theme is selected.</summary>
    public bool IsDarkTheme
    {
        get => Theme == AppTheme.Dark;
        set
        {
            if (value)
            {
                Theme = AppTheme.Dark;
            }
        }
    }

    /// <summary>Gets or sets whether the Windows theme is followed.</summary>
    public bool IsSystemTheme
    {
        get => Theme == AppTheme.System;
        set
        {
            if (value)
            {
                Theme = AppTheme.System;
            }
        }
    }

    /// <summary>Gets or sets whether operating-system notifications are disabled.</summary>
    public bool IsNotificationModeDisabled
    {
        get => NotificationMode == NotificationMode.Disabled;
        set
        {
            if (value)
            {
                NotificationMode = NotificationMode.Disabled;
            }
        }
    }

    /// <summary>Gets or sets whether notifications are shown while the window is unfocused.</summary>
    public bool IsNotificationModeWhenUnfocused
    {
        get => NotificationMode == NotificationMode.WhenUnfocused;
        set
        {
            if (value)
            {
                NotificationMode = NotificationMode.WhenUnfocused;
            }
        }
    }

    /// <summary>Gets or sets whether notifications are always shown.</summary>
    public bool IsNotificationModeAlways
    {
        get => NotificationMode == NotificationMode.Always;
        set
        {
            if (value)
            {
                NotificationMode = NotificationMode.Always;
            }
        }
    }

    partial void OnThemeChanged(AppTheme value)
    {
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(IsSystemTheme));
        SaveIfReady();
    }
    partial void OnCloseToTrayChanged(bool? value) => SaveIfReady();
    partial void OnNotificationModeChanged(NotificationMode value)
    {
        OnPropertyChanged(nameof(IsNotificationModeDisabled));
        OnPropertyChanged(nameof(IsNotificationModeWhenUnfocused));
        OnPropertyChanged(nameof(IsNotificationModeAlways));
        SaveIfReady();
    }
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
        NotificationMode = defaults.NotificationMode;
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
            Theme = Theme,
            ApplyProfileOnSelection = ApplyProfileOnSelection,
            NotificationsEnabled = NotificationMode != NotificationMode.Disabled,
            NotificationMode = NotificationMode,
            CloseToTray = CloseToTray,
            ShowVirtualAdapters = ShowVirtualAdapters,
            RememberWindowState = RememberWindowState,
            WindowPlacement = RememberWindowState ? _settings.WindowPlacement : null
        };
        _themeService.Apply(Theme);
        SettingsSaveResult result = _settingsRepository
            .SaveAsync(settings, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        PersistenceWarning = !result.IsSuccess;
        _settings = settings;
        SettingsChanged?.Invoke(this, settings);
    }
}
