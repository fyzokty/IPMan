using System.ComponentModel;
using System.Windows;
using IPMan.App.Presentation;
using IPMan.App.ViewModels;

namespace IPMan.App.Views;

public partial class SafetyCountdownWindow : Window
{
    private readonly SafetyCountdownViewModel _viewModel;

    public SafetyCountdownWindow(SafetyCountdownViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, CancelEventArgs e) =>
        _viewModel.Complete(SafetyCountdownDecision.Revert);
}
