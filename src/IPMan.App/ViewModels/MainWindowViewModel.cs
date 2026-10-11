using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.Services;
using IPMan.Application.Common;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Domain.Settings;
using IPMan.Domain.Adapters;
using IPMan.Application.Profiles;
using IPMan.Domain.Profiles;

namespace IPMan.App.ViewModels;

/// <summary>
/// Main window coordinator: owns the connection collection, the selected
/// adapter, the global loading/error state and the connection status.
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
    private readonly OperationNotificationService? _operationNotificationService;
    private readonly Func<NetworkProfile?, NetworkAdapterSnapshot?, ProfileEditorViewModel>? _editorFactory;
    private readonly IUserConfirmationService? _confirmationService;
    private NetworkAdapterId? _editorAdapterId;

    private DateTimeOffset? _lastSuccessfulRefreshUtc;
    private string? _lastSelectedAdapterId;
    private bool _isInitialized;
    private bool _isDisposed;
    private bool _showVirtualAdapters;

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
            null,
            null)
    {
    }

    /// <summary>Creates the main-window coordinator with the saved settings catalog.</summary>
    public MainWindowViewModel(
        IAdapterRefreshCoordinator refreshCoordinator,
        IUiDispatcher uiDispatcher,
        IClipboardService clipboardService,
        IClock clock,
        IElevationStateProvider elevationStateProvider,
        IApplicationVersionProvider versionProvider,
        AdapterActionsViewModel actions,
        IStaticIpv4ConfigurationValidator validator,
        ProfilePanelViewModel? profilePanel,
        OperationNotificationService? operationNotificationService = null,
        Func<NetworkProfile?, NetworkAdapterSnapshot?, ProfileEditorViewModel>? editorFactory = null,
        IUserConfirmationService? confirmationService = null)
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
        _operationNotificationService = operationNotificationService;
        _editorFactory = editorFactory;
        _confirmationService = confirmationService;
        ProfilePanel = profilePanel;

        Actions = actions;
        Actions.PropertyChanged += OnActionsPropertyChanged;

        if (ProfilePanel is not null)
        {
            ProfilePanel.PropertyChanged += OnProfilesPropertyChanged;
        }

        ApplicationState = Strings.StateLoading;
        SelectedAdapterState = Strings.StatusNoSelectedAdapter;
        LastRefresh = Strings.StatusLastRefreshNever;
        AdministratorState = elevationStateProvider.IsElevated ? Strings.StatusAdministratorYes : Strings.StatusAdministratorNo;
        Version = Strings.FormatVersion(versionProvider.Version);
        UpdateHome();
    }

    public string ApplicationName { get; } = Strings.ApplicationName;

    public string RefreshErrorText { get; } = Strings.RefreshFailedTitle;

    public string SettingsWarningText { get; } = Strings.SettingsPersistenceWarning;

    public ObservableCollection<AdapterViewModel> Adapters { get; } = new();

    [ObservableProperty]
    private string _applicationState = string.Empty;

    [ObservableProperty]
    private string _selectedAdapterState = string.Empty;

    [ObservableProperty]
    private string _lastRefresh = string.Empty;

    public string AdministratorState { get; }
    public string Version { get; }

    /// <summary>The shared action state for the currently selected adapter.</summary>
    public AdapterActionsViewModel Actions { get; }

    /// <summary>The shared saved-setting catalog and operations.</summary>
    public ProfilePanelViewModel? ProfilePanel { get; }

    /// <summary>Gets the session-only history of adapter operation notifications.</summary>
    public ObservableCollection<OperationNotificationService.OperationNotificationRecord> NotificationHistory =>
        _operationNotificationService?.History ?? _emptyNotificationHistory;

    private static readonly ObservableCollection<OperationNotificationService.OperationNotificationRecord>
        _emptyNotificationHistory = new();

    [ObservableProperty]
    private bool _isHistoryOpen;

    /// <summary>Shown when discovery has completed and Windows reported no adapters.</summary>
    public bool IsEmptyStateVisible => !IsLoading && Adapters.Count == 0 && !IsProfileEditorOpen;

    public bool IsContentVisible => !IsLoading && (Adapters.Count > 0 || IsProfileEditorOpen);

    /// <summary>Gets the stable identifier that should be persisted for the selected adapter.</summary>
    public string? LastSelectedAdapterId => _lastSelectedAdapterId;

    /// <summary>Gets whether a network configuration action is currently running.</summary>
    public bool IsBusy => Actions.IsBusy;

    /// <summary>Connection selection remains fixed while an action targets the selected adapter.</summary>
    public bool IsConnectionSelectionEnabled => !IsBusy && !IsProfileEditorOpen;

    /// <summary>Gets whether the manual page contains unapplied changes.</summary>
    public bool HasUnappliedChanges => DetailsPage?.HasUnappliedChanges == true;

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

    /// <summary>Selects an adapter when a related operating-system notification is clicked.</summary>
    public void SelectAdapter(string adapterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterId);
        // An action targets the selected adapter; notification clicks must not retarget it.
        if (IsBusy)
        {
            return;
        }
        if (Guid.TryParse(adapterId, out Guid parsed))
        {
            string normalizedAdapterId = parsed.ToString("B");
            AdapterViewModel? adapter = Adapters.FirstOrDefault(item =>
                string.Equals(item.Id.Value, normalizedAdapterId, StringComparison.OrdinalIgnoreCase));
            if (adapter is not null)
            {
                SelectedAdapter = adapter;
            }
        }
    }

    [RelayCommand]
    private void ToggleHistory() => IsHistoryOpen = !IsHistoryOpen;

    [RelayCommand]
    private void ClearHistory() => _operationNotificationService?.Clear();

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
        CloseEditor();
        CloseDetails();

        _refreshCoordinator.Refreshed -= OnAdaptersRefreshed;
        _refreshCoordinator.RefreshFailed -= OnAdapterRefreshFailed;
        Actions.PropertyChanged -= OnActionsPropertyChanged;

        if (ProfilePanel is not null)
        {
            ProfilePanel.PropertyChanged -= OnProfilesPropertyChanged;
        }
        Actions.Dispose();
        ProfilePanel?.Dispose();
    }

    partial void OnSelectedAdapterChanged(AdapterViewModel? value)
    {
        if (DetailsPage is not null && !ReferenceEquals(DetailsPage.Adapter, value))
        {
            CloseDetails();
            CurrentPage = MainWindowPage.Home;
        }
        _lastSelectedAdapterId = value?.Id.Value;
        UpdateSelectedAdapterStatus();
        Actions.Attach(value);
        UpdateEditorAvailability();
        UpdateHome();
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

        ApplicationState = Strings.StateReady;
        LastRefresh = AdapterDisplayFormatter.FormatRefreshTime(
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

        ApplicationState = Strings.StateError;

        NotifyStateVisibilityChanged();
    }

    /// <summary>
    /// Reconciles the connection collection with a discovery result, reusing existing
    /// connection ViewModels so per-adapter drafts survive, and keeping the displayed
    /// order identical to the discovery order.
    /// </summary>
    private void MergeAdapters(IReadOnlyList<NetworkAdapterSnapshot> adapters)
    {
        if (Actions.IsBusy && Actions.CurrentAdapter is AdapterViewModel activeAdapter)
        {
            NetworkAdapterSnapshot? latest = adapters.FirstOrDefault(adapter => adapter.Id == activeAdapter.Id);
            if (latest is null)
            {
                Actions.CancelUnavailableAction();
            }
            else if (activeAdapter.IsConnected && !latest.IsConnected)
            {
                // A link that merely drops (e.g. while the driver applies a new address) does not
                // end a safe switch; only a vanished adapter does.
                Actions.CancelUnavailableAction(adapterRemoved: false);
            }
        }

        IReadOnlyList<NetworkAdapterSnapshot> displayedAdapters = AdapterVisibilityFilter
            .SelectVisible(adapters, _showVirtualAdapters, SelectedAdapter?.Id);
        NetworkAdapterId? previouslySelectedId = SelectedAdapter?.Id;
        HashSet<NetworkAdapterId> displayedAdapterIds = displayedAdapters
            .Select(adapter => adapter.Id)
            .ToHashSet();

        for (int index = Adapters.Count - 1; index >= 0; index--)
        {
            if (!displayedAdapterIds.Contains(Adapters[index].Id))
            {
                Adapters.RemoveAt(index);
            }
        }

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

        RestoreSelection(previouslySelectedId);
        UpdateHome();
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
        SelectedAdapterState = SelectedAdapter is null
            ? Strings.StatusNoSelectedAdapter
            : Strings.FormatSelectedAdapter(
                SelectedAdapter.DisplayName,
                SelectedAdapter.IsConnected && SelectedAdapter.Snapshot.InternetConnectivity == InternetConnectivity.Unknown
                ? Strings.ConnectionStatusUnknown : SelectedAdapter.ConnectionState);

    private void OnActionsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AdapterActionsViewModel.IsBusy))
        {
            UpdateEditorAvailability();
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(IsConnectionSelectionEnabled));
            foreach (SettingItemViewModel item in Settings)
            {
                item.SwitchCommand.NotifyCanExecuteChanged();
            }
            ApplicationState = Actions.IsBusy
                ? Strings.StateApplying
                : HasRefreshError
                    ? Strings.StateError
                    : Strings.StateReady;
        }

        OnPropertyChanged(nameof(HasUnappliedChanges));
    }

    private void NotifyStateVisibilityChanged()
    {
        OnPropertyChanged(nameof(IsEmptyStateVisible));
        OnPropertyChanged(nameof(IsContentVisible));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHomeVisible))]
    [NotifyPropertyChangedFor(nameof(IsDetailsOpen))]
    [NotifyPropertyChangedFor(nameof(IsProfileEditorOpen))]
    [NotifyPropertyChangedFor(nameof(IsConnectionSelectionEnabled))]
    [NotifyPropertyChangedFor(nameof(IsContentVisible))]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    private MainWindowPage _currentPage;

    [ObservableProperty]
    private ProfileEditorViewModel? _profileEditor;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnappliedChanges))]
    private AdapterDetailsPageViewModel? _detailsPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCardView))]
    [NotifyPropertyChangedFor(nameof(IsListView))]
    private PreferredViewMode _preferredViewMode;

    public event EventHandler? ViewModeChanged;

    public bool IsHomeVisible => CurrentPage == MainWindowPage.Home;
    public bool IsDetailsOpen => CurrentPage == MainWindowPage.Details;
    public bool IsProfileEditorOpen => CurrentPage == MainWindowPage.ProfileEditor;
    public Task ProfileSwitchTask { get; private set; } = Task.CompletedTask;
    public bool IsCardView => PreferredViewMode == PreferredViewMode.Card ||
        (PreferredViewMode == PreferredViewMode.Auto && (ProfilePanel?.AllProfiles.Count ?? 0) <= 6);
    public bool IsListView => !IsCardView;
    public IReadOnlyList<SettingItemViewModel> Settings { get; private set; } = Array.Empty<SettingItemViewModel>();
    public IReadOnlyList<SettingGroupViewModel> SettingGroups { get; private set; } = Array.Empty<SettingGroupViewModel>();
    public bool IsCableDisconnected => SelectedAdapter is not null && !SelectedAdapter.IsConnected &&
        AdapterCategoryClassifier.GetConnectionKind(SelectedAdapter.Snapshot) == AdapterCategoryClassifier.ConnectionKind.Wired;
    public ApplyStatusSeverity ConnectivitySeverity => SelectedAdapter?.IsConnected != true ? ApplyStatusSeverity.None :
        SelectedAdapter.Snapshot.InternetConnectivity switch
        {
            InternetConnectivity.Internet => ApplyStatusSeverity.Success,
            InternetConnectivity.LocalNetwork or InternetConnectivity.None => ApplyStatusSeverity.Warning,
            _ => ApplyStatusSeverity.None
        };
    public string ConnectivityGlyph => ConnectivitySeverity switch
    {
        ApplyStatusSeverity.Success => "\uE73E",
        ApplyStatusSeverity.Warning => "\uE7BA",
        _ => IsCableDisconnected ? "\uE711" : "\uE946"
    };
    private AdapterViewModel? ConnectedWireless => Adapters.FirstOrDefault(adapter => adapter.IsConnected &&
        AdapterCategoryClassifier.GetConnectionKind(adapter.Snapshot) == AdapterCategoryClassifier.ConnectionKind.Wireless);
    public bool HasWirelessShortcut => IsCableDisconnected && ConnectedWireless is not null;
    public string StatusHeadline
    {
        get
        {
            if (IsCableDisconnected)
            {
                return Strings.ConnectionStatusCableDisconnected;
            }
            if (SelectedAdapter is null)
            {
                return Strings.StatusNoSelectedAdapter;
            }
            NetworkAdapterSnapshot snapshot = SelectedAdapter.Snapshot;
            string name = snapshot.Mode == NetworkConfigurationMode.Dhcp ? Strings.ModeDhcp :
                new ActiveSettingMatcher().Find(snapshot, ProfilePanel?.AllProfiles ?? [])?.Name ??
                Strings.SafeSwitchManualSetting;
            return Strings.FormatCurrentNetwork(name);
        }
    }
    public string StatusDescription => IsCableDisconnected ? Strings.CableDisconnectedGuidance :
        SelectedAdapter is null ? Strings.ConnectionStatusNoConnection :
        Strings.FormatConnectionSummary(AdapterDisplayFormatter.FormatConnectionKind(SelectedAdapter.Snapshot),
            SelectedAdapter.IsConnected && SelectedAdapter.Snapshot.InternetConnectivity == InternetConnectivity.Unknown
                ? Strings.ConnectionStatusUnknown : SelectedAdapter.ConnectionState);

    [RelayCommand]
    private void OpenDetails()
    {
        if (SelectedAdapter is not null && ConfirmEditorExit())
        {
            CloseEditor();
            CloseDetails();
            SelectedAdapter.Draft.GetCurrentValues();
            DetailsPage = new(SelectedAdapter, Actions,
                new AsyncRelayCommand<ManualSettingsRequest>(ApplyDetailsAsync));
            DetailsPage.BackRequested += OnEditorBackRequested;
            DetailsPage.SaveAsSettingRequested += OnDetailsSaveRequested;
            DetailsPage.PropertyChanged += OnDetailsPropertyChanged;
            CurrentPage = MainWindowPage.Details;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        if (!ConfirmEditorExit()) return;
        CloseEditor();
        CloseDetails();
        CurrentPage = MainWindowPage.Home;
    }

    [RelayCommand]
    private void NewSetting() => OpenEditor(null);

    [RelayCommand]
    private void EditSetting(ProfileListItemViewModel? item)
    {
        if (item is not null) OpenEditor(item.Profile);
    }

    private void OpenEditor(NetworkProfile? profile)
    {
        if (_editorFactory is null || !ConfirmEditorExit()) return;
        CloseEditor();
        CloseDetails();
        _editorAdapterId = SelectedAdapter?.Id;
        ProfileEditor = _editorFactory(profile, SelectedAdapter?.Snapshot);
        UpdateEditorAvailability();
        ProfileEditor.BackRequested += OnEditorBackRequested;
        ProfileEditor.SaveRequested += OnEditorSaved;
        ProfileEditor.SaveAndSwitchRequested += OnEditorSavedAndSwitch;
        CurrentPage = MainWindowPage.ProfileEditor;
    }

    /// <summary>Shared guard for editor navigation and window/tray exit.</summary>
    public bool ConfirmEditorExit(bool allowActiveSwitch = false)
    {
        if (allowActiveSwitch && DetailsPage?.IsBusy == true) return true;
        if (ProfileEditor?.IsBusy == true || DetailsPage?.IsBusy == true) return false;
        if (ProfileEditor?.IsDirty == true)
            return _confirmationService?.Confirm(new UserConfirmationRequest(
                Strings.ProfileEditorDiscardTitle, Strings.ProfileEditorDiscardMessage)) == true;
        return !HasUnappliedChanges || _confirmationService?.Confirm(new UserConfirmationRequest(
            Strings.ProfileEditorDiscardTitle, Strings.DetailsDiscardMessage)) == true;
    }

    private async Task ApplyDetailsAsync(ManualSettingsRequest? request)
    {
        AdapterDetailsPageViewModel? page = DetailsPage;
        if (page is null || request is null || SelectedAdapter?.Id != request.AdapterId || IsBusy) return;
        NetworkAdapterSnapshot snapshot = page.Adapter.Snapshot;
        string previousName = snapshot.Mode == NetworkConfigurationMode.Dhcp ? Strings.SafeSwitchAutomatic :
            new ActiveSettingMatcher().Find(snapshot, ProfilePanel?.AllProfiles ?? [])?.Name ?? Strings.SafeSwitchManualSetting;
        string settingName = request.Mode == NetworkConfigurationMode.Dhcp ? Strings.SafeSwitchAutomatic : Strings.SafeSwitchManualSetting;
        page.IsAutomatic = request.Mode == NetworkConfigurationMode.Dhcp;
        if (request.Mode == NetworkConfigurationMode.Dhcp)
            await Actions.ApplyDhcpCommand.ExecuteAsync(null);
        else
        {
            AdapterDraftViewModel values = page.Adapter.Draft;
            values.Ipv4Address = request.Values.Ipv4Address;
            values.SubnetMask = request.Values.SubnetMask;
            values.Gateway = request.Values.Gateway;
            values.PrimaryDns = request.Values.PrimaryDns;
            values.SecondaryDns = request.Values.SecondaryDns;
            await Actions.ApplyStaticCommand.ExecuteAsync(null);
        }
        if (!ReferenceEquals(page, DetailsPage)) return;
        if (Actions.LastSwitchOutcome?.Status == SafeSwitchStatus.ApplyFailed)
        {
            if (Actions.LastStaticApplyResult is { } staticResult)
                page.PresentResult(staticResult, request, settingName, previousName);
            else if (Actions.LastDhcpApplyResult is { } dhcpResult)
                page.PresentResult(dhcpResult, request, settingName, previousName);
            else
                page.ShowFailure(new(Strings.FormatDetailsFailureTitle(settingName), Actions.StatusMessage,
                    Strings.DetailsUncertain, Actions.LastSwitchOutcome.ToString()), request);
        }
        else if (Actions.LastSwitchOutcome?.Status == SafeSwitchStatus.RevertFailed)
            page.ShowFailure(new(Strings.FormatDetailsFailureTitle(settingName), Actions.StatusMessage,
                Strings.DetailsUncertain, Actions.LastSwitchOutcome.ToString()), request);
        else if (Actions.LastSwitchOutcome is { Status: SafeSwitchStatus.Kept, Unverified: true })
        {
            // Kept, but the apply result was never verified: keep the band honest about it.
            DetailsFailureMessage? message = Actions.LastStaticApplyResult is { } staticResult
                ? DetailsFailureMessageFormatter.Describe(staticResult, settingName, previousName)
                : Actions.LastDhcpApplyResult is { } dhcpResult
                    ? DetailsFailureMessageFormatter.Describe(dhcpResult, settingName, previousName)
                    : null;
            page.ShowFailure((message ?? new(string.Empty, Actions.StatusMessage, Strings.DetailsUncertain,
                Actions.LastSwitchOutcome.ToString())) with { Title = Strings.FormatDetailsUnverifiedTitle(settingName) },
                request);
        }
        else if (Actions.LastSwitchOutcome?.Status is SafeSwitchStatus.Kept or SafeSwitchStatus.NoChange)
        {
            if (Actions.LastStaticApplyResult is { } staticResult)
                page.PresentResult(staticResult, request, settingName, previousName);
            else if (Actions.LastDhcpApplyResult is { } dhcpResult)
                page.PresentResult(dhcpResult, request, settingName, previousName);
        }
        else if (Actions.LastSwitchOutcome?.Status == SafeSwitchStatus.Reverted)
            page.DismissFailureCommand.Execute(null);
    }

    private void OnDetailsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AdapterDetailsPageViewModel.HasUnappliedChanges))
            OnPropertyChanged(nameof(HasUnappliedChanges));
    }

    private void OnDetailsSaveRequested(object? sender, ManualSettingsRequest request)
    {
        if (_editorFactory is null || DetailsPage?.IsBusy != false) return;
        CloseDetails();
        OpenEditor(null);
        ProfileEditor?.Prefill(request.Mode, request.Values);
    }

    private void CloseDetails()
    {
        if (DetailsPage is null) return;
        DetailsPage.BackRequested -= OnEditorBackRequested;
        DetailsPage.SaveAsSettingRequested -= OnDetailsSaveRequested;
        DetailsPage.PropertyChanged -= OnDetailsPropertyChanged;
        DetailsPage.Dispose();
        DetailsPage.Adapter.Draft.GetCurrentValues();
        DetailsPage = null;
    }

    private void OnEditorBackRequested(object? sender, EventArgs e) => GoBack();

    private void OnEditorSaved(object? sender, ProfileEditorSavedEventArgs e)
    {
        CloseEditor();
        if (ProfilePanel is not null) ProfilePanel.SearchText = string.Empty;
        CurrentPage = MainWindowPage.Home;
        UpdateHome();
    }

    private void OnEditorSavedAndSwitch(object? sender, ProfileEditorSavedEventArgs e)
    {
        NetworkAdapterId? adapterId = _editorAdapterId;
        OnEditorSaved(sender, e);
        if (SelectedAdapter?.Id == adapterId && Actions.ApplyProfileCommand.CanExecute(e.Profile))
        {
            ProfileSwitchTask = Actions.ApplyProfileCommand.ExecuteAsync(e.Profile);
        }
    }

    private void CloseEditor()
    {
        if (ProfileEditor is null) return;
        ProfileEditor.BackRequested -= OnEditorBackRequested;
        ProfileEditor.SaveRequested -= OnEditorSaved;
        ProfileEditor.SaveAndSwitchRequested -= OnEditorSavedAndSwitch;
        ProfileEditor.Dispose();
        ProfileEditor = null;
        _editorAdapterId = null;
    }

    private void UpdateEditorAvailability()
    {
        if (ProfileEditor is not null)
        {
            ProfileEditor.IsAdapterBusy = IsBusy || SelectedAdapter is null || SelectedAdapter.Id != _editorAdapterId;
        }
    }

    [RelayCommand]
    private void SwitchToWireless()
    {
        if (!IsBusy && ConnectedWireless is { } wireless)
        {
            SelectAdapter(wireless.Id.Value);
        }
    }

    [RelayCommand]
    private void ChooseCardView() => ChooseViewMode(PreferredViewMode.Card);

    [RelayCommand]
    private void ChooseListView() => ChooseViewMode(PreferredViewMode.List);

    private void ChooseViewMode(PreferredViewMode mode)
    {
        PreferredViewMode = mode;
        ViewModeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnProfilesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProfilePanelViewModel.Profiles))
        {
            UpdateHome();
        }
    }

    private void UpdateHome()
    {
        Actions.SetAvailableSettings(ProfilePanel?.AllProfiles ?? []);
        NetworkAdapterSnapshot? snapshot = SelectedAdapter?.Snapshot;
        string? activeId = snapshot is null ? null : new ActiveSettingMatcher()
            .Find(snapshot, ProfilePanel?.AllProfiles ?? [])?.ProfileId;
        bool connected = snapshot?.IsConnected == true;
        List<SettingItemViewModel> settings = [new(this, null, connected && snapshot?.Mode == NetworkConfigurationMode.Dhcp)];
        settings.AddRange((ProfilePanel?.Profiles ?? []).Select(profile =>
            new SettingItemViewModel(this, profile, connected && profile.Id == activeId)));
        Settings = settings;
        List<SettingGroupViewModel> groups = new();
        SettingItemViewModel[] favorites = settings.Skip(1).Where(item => item.IsFavorite).ToArray();
        SettingItemViewModel[] others = settings.Skip(1).Where(item => !item.IsFavorite).ToArray();
        if (favorites.Length > 0)
        {
            groups.Add(new(Strings.ProfileGroupFavorites, favorites));
        }
        if (others.Length > 0)
        {
            groups.Add(new(Strings.ProfileGroupOthers, others));
        }
        SettingGroups = groups;
        foreach (string property in new[] { nameof(Settings), nameof(SettingGroups), nameof(IsCardView),
            nameof(IsListView), nameof(ConnectivitySeverity), nameof(ConnectivityGlyph), nameof(StatusHeadline), nameof(StatusDescription), nameof(IsCableDisconnected), nameof(HasWirelessShortcut) })
        {
            OnPropertyChanged(property);
        }
    }
}

public enum MainWindowPage
{
    Home,
    Details,
    ProfileEditor
}
