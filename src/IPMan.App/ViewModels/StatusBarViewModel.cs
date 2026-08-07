using CommunityToolkit.Mvvm.ComponentModel;

namespace IPMan.App.ViewModels;

/// <summary>
/// Bottom status bar state. Populated by <see cref="MainWindowViewModel"/>; it
/// holds no logic of its own.
/// </summary>
public sealed partial class StatusBarViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationState = string.Empty;

    [ObservableProperty]
    private string _selectedAdapterState = string.Empty;

    [ObservableProperty]
    private string _lastRefresh = string.Empty;

    [ObservableProperty]
    private string _administratorState = string.Empty;

    [ObservableProperty]
    private string _version = string.Empty;
}
