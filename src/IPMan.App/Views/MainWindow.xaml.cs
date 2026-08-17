using System.Windows;
using IPMan.App.Presentation;
using IPMan.App.ViewModels;

namespace IPMan.App.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    internal void RestoreAndActivate()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Show();
        Activate();
    }
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
