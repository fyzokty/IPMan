using System.Windows;
using IPMan.App.ViewModels;

namespace IPMan.App.Views;

/// <summary>Modal, selectable diagnostics report owned by the settings window.</summary>
public partial class DiagnosticsWindow : Window
{
    public DiagnosticsWindow(DiagnosticsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        ((DiagnosticsViewModel)DataContext).RefreshCommand.Execute(null);

    private void OnCloseClicked(object sender, RoutedEventArgs e) => Close();
}
