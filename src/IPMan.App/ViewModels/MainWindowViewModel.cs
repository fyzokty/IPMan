using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.App.ViewModels;

/// <summary>
/// Sprint 04 shell ViewModel: it observes the refresh coordinator and projects
/// discovered adapters into a read-only diagnostic list. It performs no network
/// work itself and owns no background scheduling.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IAdapterRefreshCoordinator _refreshCoordinator;
    private readonly IUiDispatcher _uiDispatcher;

    private bool _isInitialized;
    private bool _isDisposed;

    [ObservableProperty]
    private string _statusText = Strings.StatusNotStarted;

    public MainWindowViewModel(
        IAdapterRefreshCoordinator refreshCoordinator,
        IUiDispatcher uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(refreshCoordinator);
        ArgumentNullException.ThrowIfNull(uiDispatcher);

        _refreshCoordinator = refreshCoordinator;
        _uiDispatcher = uiDispatcher;
    }

    public string ApplicationName { get; } = "IPMan";

    public string DiagnosticViewHeader { get; } = Strings.DiagnosticViewHeader;

    public ObservableCollection<AdapterDiagnosticItem> Adapters { get; } = new();

    /// <summary>
    /// Starts observation. Called once by the composition root after startup.
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized || _isDisposed)
        {
            return;
        }

        _isInitialized = true;

        _refreshCoordinator.Refreshed += OnAdaptersRefreshed;
        _refreshCoordinator.RefreshFailed += OnAdapterRefreshFailed;
        _refreshCoordinator.StartCoordinating();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        _refreshCoordinator.Refreshed -= OnAdaptersRefreshed;
        _refreshCoordinator.RefreshFailed -= OnAdapterRefreshFailed;
    }

    private void OnAdaptersRefreshed(object? sender, AdapterRefreshedEventArgs e) =>
        _uiDispatcher.Post(() => ApplyRefresh(e));

    private void OnAdapterRefreshFailed(object? sender, AdapterRefreshFailedEventArgs e) =>
        _uiDispatcher.Post(() => StatusText = Strings.FormatStatusRefreshFailed(e.Reason));

    private void ApplyRefresh(AdapterRefreshedEventArgs refresh)
    {
        Adapters.Clear();

        foreach (NetworkAdapterSnapshot snapshot in refresh.Adapters)
        {
            Adapters.Add(AdapterDiagnosticItem.FromSnapshot(snapshot));
        }

        StatusText = Strings.FormatStatusRefreshed(refresh.Adapters.Count, refresh.Reason);
    }
}
