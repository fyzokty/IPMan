using System.Windows;
using IPMan.App.Presentation;

namespace IPMan.App.Views;

public partial class SafeSwitchExitWindow : Window
{
    public SafeSwitchExitWindow() => InitializeComponent();

    public SafeSwitchExitDecision Decision { get; private set; } = SafeSwitchExitDecision.Cancel;

    private void OnKeep(object sender, RoutedEventArgs e)
    {
        Decision = SafeSwitchExitDecision.Keep;
        DialogResult = true;
    }

    private void OnRevert(object sender, RoutedEventArgs e)
    {
        Decision = SafeSwitchExitDecision.Revert;
        DialogResult = true;
    }
}
