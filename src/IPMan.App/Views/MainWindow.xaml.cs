using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.Services;
using IPMan.App.ViewModels;
using IPMan.Application.Settings;
using IPMan.Domain.Settings;

namespace IPMan.App.Views;

/// <summary>Main application window and its small amount of window-lifetime coordination.</summary>
public partial class MainWindow : Window
{
    private const double DefaultWidth = 1100;
    private const double DefaultHeight = 700;

    private readonly MainWindowViewModel _viewModel;
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly IUserConfirmationService _confirmationService;
    private readonly TrayIconManager _trayIconManager;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly WindowsThemeService _themeService;
    private AppSettings _settings;
    private bool _isHidingToTray;
    private bool _isExiting;
    private SettingsWindow? _settingsWindow;

    /// <summary>Creates the main window and restores the user-scoped preferences.</summary>
    public MainWindow(
        MainWindowViewModel viewModel,
        IAppSettingsRepository settingsRepository,
        IUserConfirmationService confirmationService,
        TrayIconManager trayIconManager,
        SettingsViewModel settingsViewModel,
        WindowsThemeService themeService)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(settingsRepository);
        ArgumentNullException.ThrowIfNull(confirmationService);
        ArgumentNullException.ThrowIfNull(trayIconManager);
        ArgumentNullException.ThrowIfNull(settingsViewModel);
        ArgumentNullException.ThrowIfNull(themeService);

        _viewModel = viewModel;
        _settingsRepository = settingsRepository;
        _confirmationService = confirmationService;
        _trayIconManager = trayIconManager;
        _settingsViewModel = settingsViewModel;
        _themeService = themeService;
        _settings = _settingsRepository.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        _viewModel.SetLastSelectedAdapterId(_settings.LastSelectedAdapterId);
        _viewModel.SetShowVirtualAdapters(_settings.ShowVirtualAdapters);
        _viewModel.ProfilePanel?.SetApplyProfileOnSelection(_settings.ApplyProfileOnSelection);
        _viewModel.SettingsWarningVisible = _settingsRepository.LastLoadFailed;

        InitializeComponent();
        DataContext = viewModel;
        SourceInitialized += OnSourceInitialized;
        _themeService.AttachWindow(this);
        Closing += OnClosing;
        StateChanged += OnStateChanged;
        _trayIconManager.OpenRequested += OnTrayOpenRequested;
        _trayIconManager.ExitRequested += OnTrayExitRequested;
        _viewModel.Actions.PropertyChanged += OnActionsPropertyChanged;
        _settingsViewModel.SettingsChanged += OnSettingsChanged;
    }

    /// <summary>Restores the window, makes it foreground and gives it keyboard focus.</summary>
    internal void RestoreAndActivate()
    {
        _trayIconManager.Hide();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Show();
        Activate();
        Focus();
    }

    /// <summary>Closes without prompts during Windows session ending.</summary>
    internal void ExitWithoutPrompt()
    {
        _isExiting = true;
        Close();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        AppWindowPlacement placement = WindowPlacementValidator.Validate(
            _settings.RememberWindowState ? _settings.WindowPlacement : null,
            GetWorkAreas(),
            GetPrimaryWorkArea(),
            MinWidth,
            MinHeight,
            DefaultWidth,
            DefaultHeight);
        Left = placement.Left!.Value;
        Top = placement.Top!.Value;
        Width = placement.Width!.Value;
        Height = placement.Height!.Value;
        if (placement.IsMaximized == true)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settingsViewModel) { Owner = this };
        _themeService.AttachWindow(_settingsWindow);
        _settingsWindow.Closed += OnSettingsWindowClosed;
        _settingsWindow.ShowDialog();
    }

    private void OnSettingsWindowClosed(object? sender, EventArgs e)
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Closed -= OnSettingsWindowClosed;
        }

        _settingsWindow = null;
    }

    private void OnProfileMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is TextBlock { DataContext: ProfileListItemViewModel item })
        {
            _viewModel.ProfilePanel?.ApplyOnExplicitSelection(item);
        }
    }

    private void OnProfileMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
        _viewModel.ProfilePanel?.BeginExplicitProfileSelection();

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExiting && _settings.CloseToTray == true)
        {
            e.Cancel = true;
            SendToTray();
            return;
        }

        if (!_isExiting && _settings.CloseToTray is null)
        {
            bool closeToTray = _confirmationService.Confirm(new UserConfirmationRequest(
                Strings.TrayPreferenceTitle,
                Strings.TrayPreferenceMessage));
            _settings = _settings with { CloseToTray = closeToTray };
            if (closeToTray)
            {
                PersistSettings();
                e.Cancel = true;
                SendToTray();
                return;
            }
        }

        if (!_isExiting && RequiresExitConfirmation() &&
            !_confirmationService.Confirm(new UserConfirmationRequest(
                Strings.ExitConfirmationTitle,
                Strings.ExitConfirmationMessage)))
        {
            e.Cancel = true;
            return;
        }

        _trayIconManager.Hide();
        PersistSettings();
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (_isHidingToTray || WindowState != WindowState.Minimized)
        {
            return;
        }

        if (_settings.CloseToTray is null)
        {
            _settings = _settings with
            {
                CloseToTray = _confirmationService.Confirm(new UserConfirmationRequest(
                    Strings.TrayPreferenceTitle,
                    Strings.TrayPreferenceMessage))
            };
            PersistSettings();
        }

        if (_settings.CloseToTray == true)
        {
            SendToTray();
        }
    }

    private void OnTrayOpenRequested(object? sender, EventArgs e) => RestoreAndActivate();

    private void OnTrayExitRequested(object? sender, EventArgs e)
    {
        if (RequiresExitConfirmation() &&
            !_confirmationService.Confirm(new UserConfirmationRequest(
                Strings.ExitConfirmationTitle,
                Strings.ExitConfirmationMessage)))
        {
            return;
        }

        _isExiting = true;
        Close();
    }

    private void OnActionsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AdapterActionsViewModel.StatusSeverity))
        {
            return;
        }

        if (_viewModel.Actions.StatusSeverity == ApplyStatusSeverity.Error)
        {
            if (!IsVisible)
            {
                RestoreAndActivate();
            }

            if (!string.IsNullOrWhiteSpace(_viewModel.Actions.StatusMessage))
            {
                MessageBox.Show(
                    _viewModel.Actions.StatusMessage,
                    Strings.ApplicationName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            return;
        }

        if (_viewModel.Actions.StatusSeverity == ApplyStatusSeverity.Information &&
            !IsVisible &&
            _settings.NotificationsEnabled &&
            !string.IsNullOrWhiteSpace(_viewModel.Actions.StatusMessage))
        {
            _trayIconManager.ShowBalloon(_viewModel.Actions.StatusMessage);
        }
    }

    private void SendToTray()
    {
        _isHidingToTray = true;
        try
        {
            PersistSettings();
            _trayIconManager.Show();
            Hide();
            if (_settings.NotificationsEnabled)
            {
                _trayIconManager.ShowBalloon(Strings.TrayBalloonMessage);
            }
        }
        finally
        {
            _isHidingToTray = false;
        }
    }

    private bool RequiresExitConfirmation() => _viewModel.IsBusy || _viewModel.HasDirtyDrafts;

    private void PersistSettings()
    {
        AppSettings updated = _settings with
        {
            WindowPlacement = _settings.RememberWindowState ? CapturePlacement() : null,
            LastSelectedAdapterId = _viewModel.LastSelectedAdapterId
        };
        if (updated == _settings)
        {
            return;
        }

        SettingsSaveResult result = _settingsRepository.SaveAsync(updated, CancellationToken.None).GetAwaiter().GetResult();
        _viewModel.SettingsWarningVisible = !result.IsSuccess;
        _settings = updated;
    }

    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        _settings = settings;
        _viewModel.SetShowVirtualAdapters(settings.ShowVirtualAdapters);
        _viewModel.ProfilePanel?.SetApplyProfileOnSelection(settings.ApplyProfileOnSelection);
        _viewModel.SettingsWarningVisible = _settingsViewModel.PersistenceWarning;
    }

    private AppWindowPlacement CapturePlacement()
    {
        Rect restoreBounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, Width, Height)
            : RestoreBounds;
        AppWindowPlacement placement = new(
            restoreBounds.Left,
            restoreBounds.Top,
            restoreBounds.Width,
            restoreBounds.Height,
            WindowState == WindowState.Maximized);
        return WindowPlacementValidator.Validate(
            placement,
            GetWorkAreas(),
            GetPrimaryWorkArea(),
            MinWidth,
            MinHeight,
            DefaultWidth,
            DefaultHeight);
    }

    private static Rect[] GetWorkAreas() =>
        Forms.Screen.AllScreens.Select(screen => ToRect(screen.WorkingArea)).ToArray();

    private static Rect GetPrimaryWorkArea()
    {
        Forms.Screen? primaryScreen = Forms.Screen.PrimaryScreen ?? Forms.Screen.AllScreens.FirstOrDefault();
        return primaryScreen is null
            ? new Rect(0, 0, DefaultWidth, DefaultHeight)
            : ToRect(primaryScreen.WorkingArea);
    }

    private static Rect ToRect(System.Drawing.Rectangle rectangle) =>
        new(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
}

/// <summary>Marshals activation requests from IPC onto the WPF UI thread.</summary>
internal sealed class MainWindowActivationHandler
{
    private readonly IUiDispatcher _uiDispatcher;
    private readonly Action _activateWindow;

    public MainWindowActivationHandler(
        IUiDispatcher uiDispatcher,
        MainWindow mainWindow)
        : this(uiDispatcher, mainWindow.RestoreAndActivate)
    {
    }

    internal MainWindowActivationHandler(
        IUiDispatcher uiDispatcher,
        Action activateWindow)
    {
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(activateWindow);

        _uiDispatcher = uiDispatcher;
        _activateWindow = activateWindow;
    }

    public void OnActivationRequested(object? sender, EventArgs e) =>
        _uiDispatcher.Post(_activateWindow);
}
