using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Application.Common;
using IPMan.Application.Networking;
using IPMan.Domain.Adapters;
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
    private string? _lastSelectedAdapterId;
    private bool _isInitialized;
    private bool _isDisposed;
    private bool _showVirtualAdapters = true;

    /// <summary>True until the first discovery pass completes or fails.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    [NotifyPropertyChangedFor(nameof(IsContentVisible))]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _hasRefreshError;

    [ObservableProperty]
    private bool _settingsWarningVisible;

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
        : this(
            refreshCoordinator,
            uiDispatcher,
            clipboardService,
            clock,
            elevationStateProvider,
            versionProvider,
            actions,
            validator,
            null)
    {
    }

    /// <summary>Creates the main-window coordinator with the fixed profile panel.</summary>
    public MainWindowViewModel(
        IAdapterRefreshCoordinator refreshCoordinator,
        IUiDispatcher uiDispatcher,
        IClipboardService clipboardService,
        IClock clock,
        IElevationStateProvider elevationStateProvider,
        IApplicationVersionProvider versionProvider,
        AdapterActionsViewModel actions,
        IStaticIpv4ConfigurationValidator validator,
        ProfilePanelViewModel? profilePanel)
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
        ProfilePanel = profilePanel;

        Actions = actions;
        Actions.PropertyChanged += OnActionsPropertyChanged;
        Actions.ApplyCompleted += OnApplyCompleted;
        if (ProfilePanel is not null)
        {
            ProfilePanel.ExplicitProfileApplyRequested += OnExplicitProfileApplyRequested;
        }

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

    public string SettingsWarningText { get; } = Strings.SettingsPersistenceWarning;

    public ObservableCollection<AdapterViewModel> Adapters { get; } = new();

    public StatusBarViewModel StatusBar { get; }

    /// <summary>The shared action state for the currently selected adapter.</summary>
    public AdapterActionsViewModel Actions { get; }

    /// <summary>The fixed profile-management panel shown beside adapter tabs.</summary>
    public ProfilePanelViewModel? ProfilePanel { get; }

    /// <summary>Shown when discovery has completed and Windows reported no adapters.</summary>
    public bool IsEmptyStateVisible => !IsLoading && Adapters.Count == 0;

    public bool IsContentVisible => !IsLoading && Adapters.Count > 0;

    /// <summary>Gets the stable identifier that should be persisted for the selected adapter.</summary>
    public string? LastSelectedAdapterId => _lastSelectedAdapterId;

    /// <summary>Gets whether a network configuration action is currently running.</summary>
    public bool IsBusy => Actions.IsBusy;

    /// <summary>Gets whether any adapter draft contains unapplied changes.</summary>
    public bool HasDirtyDrafts => Adapters.Any(adapter => adapter.Draft.IsDirty);

    /// <summary>Sets the adapter identity restored from application settings before discovery.</summary>
    public void SetLastSelectedAdapterId(string? adapterId)
    {
        _lastSelectedAdapterId = Guid.TryParse(adapterId, out Guid parsed)
            ? parsed.ToString("B").ToUpperInvariant()
            : null;
    }

    /// <summary>Updates virtual-adapter visibility for subsequent refreshes.</summary>
    public void SetShowVirtualAdapters(bool showVirtualAdapters)
    {
        _showVirtualAdapters = showVirtualAdapters;
        _refreshCoordinator.RequestRefresh("SettingsChanged");
    }

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
        Actions.ApplyCompleted -= OnApplyCompleted;
        if (ProfilePanel is not null)
        {
            ProfilePanel.ExplicitProfileApplyRequested -= OnExplicitProfileApplyRequested;
        }
        Actions.Dispose();
        ProfilePanel?.Dispose();
    }

    partial void OnSelectedAdapterChanged(AdapterViewModel? value)
    {
        _lastSelectedAdapterId = value?.Id.Value;
        UpdateSelectedAdapterStatus();
        Actions.Attach(value);
        ProfilePanel?.SetDraft(value?.Draft);
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
        IReadOnlyList<NetworkAdapterSnapshot> displayedAdapters = adapters
            .Where(adapter => AdapterCategoryClassifier.IsVisible(adapter, _showVirtualAdapters) ||
                adapter.Id == SelectedAdapter?.Id)
            .ToArray();
        NetworkAdapterId? previouslySelectedId = SelectedAdapter?.Id;

        for (int targetIndex = 0; targetIndex < displayedAdapters.Count; targetIndex++)
        {
            NetworkAdapterSnapshot snapshot = displayedAdapters[targetIndex];
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
        while (Adapters.Count > displayedAdapters.Count)
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

        if (_lastSelectedAdapterId is not null)
        {
            NetworkAdapterId restoredId = new(_lastSelectedAdapterId);
            int restoredIndex = IndexOf(restoredId);

            if (restoredIndex >= 0)
            {
                SelectedAdapter = Adapters[restoredIndex];
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
            OnPropertyChanged(nameof(IsBusy));
            StatusBar.ApplicationState = Actions.IsBusy
                ? Strings.StateApplying
                : HasRefreshError
                    ? Strings.StateError
                    : Strings.StateReady;
            }

            OnPropertyChanged(nameof(HasDirtyDrafts));
        }

    private void NotifyStateVisibilityChanged()
    {
        OnPropertyChanged(nameof(IsEmptyStateVisible));
        OnPropertyChanged(nameof(IsContentVisible));
    }

    private void OnExplicitProfileApplyRequested(object? sender, IPMan.Domain.Profiles.NetworkProfile profile)
    {
        if (profile.Mode == NetworkConfigurationMode.Dhcp)
        {
            Actions.ApplyDhcpCommand.Execute(null);
            return;
        }

        Actions.ApplyStaticCommand.Execute(null);
    }

    private void OnApplyCompleted(object? sender, bool succeeded) =>
        ProfilePanel?.CompleteExplicitProfileApply(succeeded);
}
