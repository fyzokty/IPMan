using System.Windows;
using IPMan.App.ViewModels;

namespace IPMan.App.Views;

public partial class InternetLossConfirmationWindow : Window
{
    public InternetLossConfirmationWindow(InternetLossConfirmationViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnAccepted(object sender, RoutedEventArgs e) => DialogResult = true;
}
