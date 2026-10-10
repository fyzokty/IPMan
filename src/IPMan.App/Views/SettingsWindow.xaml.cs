using System.Windows;
using System.Windows.Controls;
using IPMan.App.Services;
using IPMan.App.ViewModels;

namespace IPMan.App.Views;

/// <summary>Modal host for the user settings editor.</summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly Func<DiagnosticsViewModel> _diagnosticsFactory;
    private readonly WindowsThemeService _themeService;

    /// <summary>Creates the settings window.</summary>
    public SettingsWindow(
        SettingsViewModel viewModel,
        Func<DiagnosticsViewModel> diagnosticsFactory,
        WindowsThemeService themeService)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(diagnosticsFactory);
        ArgumentNullException.ThrowIfNull(themeService);
        _viewModel = viewModel;
        _diagnosticsFactory = diagnosticsFactory;
        _themeService = themeService;
        InitializeComponent();
        DataContext = viewModel;
        AskRadioButton.IsChecked = viewModel.CloseToTray is null;
        TrayRadioButton.IsChecked = viewModel.CloseToTray == true;
        ExitRadioButton.IsChecked = viewModel.CloseToTray == false;
        viewModel.DiagnosticsRequested += OnDiagnosticsRequested;
        Closed += OnClosed;
    }

    private void OnDiagnosticsRequested(object? sender, EventArgs e)
    {
        DiagnosticsWindow window = new(_diagnosticsFactory()) { Owner = this };
        _themeService.AttachWindow(window);
        window.ShowDialog();
    }

    private void OnClosed(object? sender, EventArgs e) =>
        _viewModel.DiagnosticsRequested -= OnDiagnosticsRequested;

    private void OnCloseClicked(object sender, RoutedEventArgs e) => Close();

    private void OnCloseBehaviorChecked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string tag } || DataContext is not SettingsViewModel viewModel)
        {
            return;
        }

        viewModel.CloseToTray = tag == "Tray" ? true : tag == "Exit" ? false : null;
    }
}
