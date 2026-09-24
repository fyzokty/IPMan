using System.Windows;
using Forms = System.Windows.Forms;
using IPMan.App.Presentation;
using IPMan.App.Resources;
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
    private AppSettings _settings;
    private bool _isHidingToTray;
    private bool _isExiting;

    /// <summary>Creates the main window and restores the user-scoped preferences.</summary>
    public MainWindow(
        MainWindowViewModel viewModel,
        IAppSettingsRepository settingsRepository,
        IUserConfirmationService confirmationService,
        TrayIconManager trayIconManager)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(settingsRepository);
        ArgumentNullException.ThrowIfNull(confirmationService);
        ArgumentNullException.ThrowIfNull(trayIconManager);

        _viewModel = viewModel;
        _settingsRepository = settingsRepository;
        _confirmationService = confirmationService;
        _trayIconManager = trayIconManager;
        _settings = _settingsRepository.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        _viewModel.SetLastSelectedAdapterId(_settings.LastSelectedAdapterId);

        InitializeComponent();
        DataContext = viewModel;
        SourceInitialized += OnSourceInitialized;
        Closing += OnClosing;
        StateChanged += OnStateChanged;
        _trayIconManager.OpenRequested += OnTrayOpenRequested;
        _trayIconManager.ExitRequested += OnTrayExitRequested;
        _viewModel.Actions.PropertyChanged += OnActionsPropertyChanged;
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
            _settings.WindowPlacement,
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
        if (e.PropertyName != nameof(AdapterActionsViewModel.StatusSeverity) || IsVisible)
        {
            return;
        }

        if (_viewModel.Actions.StatusSeverity == ApplyStatusSeverity.Error)
        {
            RestoreAndActivate();
            return;
        }

        if (_viewModel.Actions.StatusSeverity == ApplyStatusSeverity.Information &&
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
            _trayIconManager.ShowBalloon(Strings.TrayBalloonMessage);
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
            WindowPlacement = CapturePlacement(),
            LastSelectedAdapterId = _viewModel.LastSelectedAdapterId
        };
        if (updated == _settings)
        {
            return;
        }

        _ = _settingsRepository.SaveAsync(updated, CancellationToken.None).GetAwaiter().GetResult();
        _settings = updated;
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
