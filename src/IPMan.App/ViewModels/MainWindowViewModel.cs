using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Application.Common;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.App.ViewModels;

/// <summary>
/// Main window coordinator: owns the adapter tab collection, the selected
/// adapter, the global loading/error state and the status bar.
/// <para>
/// It never touches Windows networking APIs. Adapter data arrives only through
/// <see cref="IAdapterRefreshCoordinator"/>, and every update is marshalled onto
/// the UI thread through <see cref="IUiDispatcher"/>.
/// </para>
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IAdapterRefreshCoordinator _refreshCoordinator;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IClipboardService _clipboardService;
    private readonly IClock _clock;
    private readonly IStaticIpv4ConfigurationValidator _validator;

    private DateTimeOffset? _lastSuccessfulRefreshUtc;
    private bool _isInitialized;
    private bool _isDisposed;

    /// <summary>True until the first discovery pass completes or fails.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    [NotifyPropertyChangedFor(nameof(IsContentVisible))]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _hasRefreshError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    [NotifyPropertyChangedFor(nameof(IsContentVisible))]
    private AdapterViewModel? _selectedAdapter;

    public MainWindowViewModel(
        IAdapterRefreshCoordinator refreshCoordinator,
        IUiDispatcher uiDispatcher,
        IClipboardService clipboardService,
        IClock clock,
        IElevationStateProvider elevationStateProvider,
        IApplicationVersionProvider versionProvider,
        AdapterActionsViewModel actions,
        IStaticIpv4ConfigurationValidator validator)
    {
        ArgumentNullException.ThrowIfNull(refreshCoordinator);
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(clipboardService);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(elevationStateProvider);
        ArgumentNullException.ThrowIfNull(versionProvider);
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(validator);

        _refreshCoordinator = refreshCoordinator;
        _uiDispatcher = uiDispatcher;
        _clipboardService = clipboardService;
        _clock = clock;
        _validator = validator;

        Actions = actions;
        Actions.PropertyChanged += OnActionsPropertyChanged;

        StatusBar = new StatusBarViewModel
        {
            ApplicationState = Strings.StateLoading,
            SelectedAdapterState = Strings.StatusNoSelectedAdapter,
            LastRefresh = Strings.StatusLastRefreshNever,
            AdministratorState = elevationStateProvider.IsElevated
                ? Strings.StatusAdministratorYes
                : Strings.StatusAdministratorNo,
            Version = Strings.FormatVersion(versionProvider.Version)
        };
    }

    public string ApplicationName { get; } = "IPMan";

    public string ApplicationTagline { get; } = Strings.ApplicationTagline;

    public string RefreshErrorText { get; } = Strings.RefreshFailedTitle;

    public ObservableCollection<AdapterViewModel> Adapters { get; } = new();

    public StatusBarViewModel StatusBar { get; }

    /// <summary>The shared action state for the currently selected adapter.</summary>
    public AdapterActionsViewModel Actions { get; }

    /// <summary>Shown when discovery has completed and Windows reported no adapters.</summary>
    public bool IsEmptyStateVisible => !IsLoading && Adapters.Count == 0;

    public bool IsContentVisible => !IsLoading && Adapters.Count > 0;

    /// <summary>Starts observation. Called once by the composition root.</summary>
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
        Actions.PropertyChanged -= OnActionsPropertyChanged;
        Actions.Dispose();
    }

    partial void OnSelectedAdapterChanged(AdapterViewModel? value)
    {
        UpdateSelectedAdapterStatus();
        Actions.Attach(value);
    }

    private void OnAdaptersRefreshed(object? sender, AdapterRefreshedEventArgs e) =>
        _uiDispatcher.Post(() => ApplyRefresh(e.Adapters));

    private void OnAdapterRefreshFailed(object? sender, AdapterRefreshFailedEventArgs e) =>
        _uiDispatcher.Post(ApplyRefreshFailure);

    private void ApplyRefresh(IReadOnlyList<NetworkAdapterSnapshot> adapters)
    {
        MergeAdapters(adapters);

        _lastSuccessfulRefreshUtc = _clock.UtcNow;
        HasRefreshError = false;
        IsLoading = false;

        StatusBar.ApplicationState = Strings.StateReady;
        StatusBar.LastRefresh = AdapterDisplayFormatter.FormatRefreshTime(
            _lastSuccessfulRefreshUtc,
            CultureInfo.CurrentCulture);

        NotifyStateVisibilityChanged();
    }

    /// <summary>
    /// A failed pass is surfaced inline. The last valid adapter state is kept:
    /// ordinary refresh failure must not blank the window or open a modal.
    /// </summary>
    private void ApplyRefreshFailure()
    {
        HasRefreshError = true;
        IsLoading = false;

        StatusBar.ApplicationState = Strings.StateError;

        NotifyStateVisibilityChanged();
    }

    /// <summary>
    /// Reconciles the tab collection with a discovery result, reusing existing
    /// tab ViewModels so per-adapter drafts survive, and keeping the displayed
    /// order identical to the discovery order.
    /// </summary>
    private void MergeAdapters(IReadOnlyList<NetworkAdapterSnapshot> adapters)
    {
        NetworkAdapterId? previouslySelectedId = SelectedAdapter?.Id;

        for (int targetIndex = 0; targetIndex < adapters.Count; targetIndex++)
        {
            NetworkAdapterSnapshot snapshot = adapters[targetIndex];
            int existingIndex = IndexOf(snapshot.Id);

            if (existingIndex < 0)
            {
                Adapters.Insert(targetIndex, new AdapterViewModel(snapshot, _clipboardService, _validator));
                continue;
            }

            Adapters[existingIndex].Update(snapshot);

            if (existingIndex != targetIndex)
            {
                Adapters.Move(existingIndex, targetIndex);
            }
        }

        // Everything past the discovered set no longer exists in Windows.
        while (Adapters.Count > adapters.Count)
        {
            Adapters.RemoveAt(Adapters.Count - 1);
        }

        RestoreSelection(previouslySelectedId);
    }

    /// <summary>
    /// Selection is identity-based: the previously selected adapter stays
    /// selected when it still exists, otherwise the first adapter in the
    /// displayed order is selected, otherwise nothing is selected.
    /// </summary>
    private void RestoreSelection(NetworkAdapterId? previouslySelectedId)
    {
        if (previouslySelectedId is not null)
        {
            int index = IndexOf(previouslySelectedId.Value);

            if (index >= 0)
            {
                SelectedAdapter = Adapters[index];
                UpdateSelectedAdapterStatus();
                return;
            }
        }

        SelectedAdapter = Adapters.Count > 0 ? Adapters[0] : null;
        UpdateSelectedAdapterStatus();
    }

    private int IndexOf(NetworkAdapterId id)
    {
        for (int index = 0; index < Adapters.Count; index++)
        {
            if (Adapters[index].Id == id)
            {
                return index;
            }
        }

        return -1;
    }

    private void UpdateSelectedAdapterStatus() =>
        StatusBar.SelectedAdapterState = SelectedAdapter is null
            ? Strings.StatusNoSelectedAdapter
            : Strings.FormatSelectedAdapter(
                SelectedAdapter.DisplayName,
                SelectedAdapter.ConnectionState);

    private void OnActionsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AdapterActionsViewModel.IsBusy))
        {
            StatusBar.ApplicationState = Actions.IsBusy
                ? Strings.StateApplying
                : HasRefreshError
                    ? Strings.StateError
                    : Strings.StateReady;
        }
    }

    private void NotifyStateVisibilityChanged()
    {
        OnPropertyChanged(nameof(IsEmptyStateVisible));
        OnPropertyChanged(nameof(IsContentVisible));
    }
}
