using System.Windows;
using System.Windows.Controls;
using IPMan.App.ViewModels;

namespace IPMan.App.Views;

/// <summary>Modal host for the user settings editor.</summary>
public partial class SettingsWindow : Window
{
    /// <summary>Creates the settings window.</summary>
    public SettingsWindow(SettingsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
        AskRadioButton.IsChecked = viewModel.CloseToTray is null;
        TrayRadioButton.IsChecked = viewModel.CloseToTray == true;
        ExitRadioButton.IsChecked = viewModel.CloseToTray == false;
    }

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
