using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Application.Common;
using IPMan.Application.Profiles;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;

namespace IPMan.App.ViewModels;

/// <summary>Coordinates profile list, persistence commands and draft loading.</summary>
public sealed partial class ProfilePanelViewModel : ObservableObject, IDisposable
{
    private readonly IProfileCatalog _catalog;
    private readonly IProfileFileDialogService _fileDialogService;
    private readonly IUserTextInputService _textInputService;
    private readonly IUserConfirmationService _confirmationService;
    private readonly IUiDispatcher _uiDispatcher;
    private AdapterDraftViewModel? _draft;
    private bool _isRestoringSelection;
    private bool _isDisposed;
    private ProfileListItemViewModel? _previousSelectedProfile;
    private string? _lastAppliedProfileId;
    private bool _applyProfileOnSelection;

    /// <summary>Raised for an explicit mouse selection when immediate application is enabled.</summary>
    public event EventHandler<NetworkProfile>? ExplicitProfileApplyRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ProfileListItemViewModel? _selectedProfile;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _newProfileName = string.Empty;

    [ObservableProperty]
    private string _newProfileDescription = string.Empty;

    [ObservableProperty]
    private bool _newProfileUsesDhcp;

    /// <summary>Creates the profile panel and begins observing catalog changes.</summary>
    public ProfilePanelViewModel(
        IProfileCatalog catalog,
        IProfileFileDialogService fileDialogService,
        IUserTextInputService textInputService,
        IUserConfirmationService confirmationService,
        IUiDispatcher uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(fileDialogService);
        ArgumentNullException.ThrowIfNull(textInputService);
        ArgumentNullException.ThrowIfNull(confirmationService);
        ArgumentNullException.ThrowIfNull(uiDispatcher);

        _catalog = catalog;
        _fileDialogService = fileDialogService;
        _textInputService = textInputService;
        _confirmationService = confirmationService;
        _uiDispatcher = uiDispatcher;
        _catalog.Changed += OnCatalogChanged;
        RefreshFromCatalog();
    }

    public ObservableCollection<ProfileGroupViewModel> Groups { get; } = new();

    public ObservableCollection<ProfileProblemListItemViewModel> Problems { get; } = new();

    public int ProblemCount => Problems.Count;

    public bool HasProblems => Problems.Count > 0;

    public string ProblemsHeader => Strings.FormatProfileProblemsHeader(ProblemCount);

    public bool HasStatusMessage => StatusMessage.Length > 0;

    /// <summary>Enables immediate profile application for explicit mouse selection only.</summary>
    public void SetApplyProfileOnSelection(bool enabled) => _applyProfileOnSelection = enabled;

    /// <summary>Requests the standard apply flow for an explicitly clicked, changed profile.</summary>
    public void ApplyOnExplicitSelection(ProfileListItemViewModel? item)
    {
        if (!_applyProfileOnSelection || item is null || SelectedProfile?.Id != item.Id ||
            item.Id == _lastAppliedProfileId)
        {
            return;
        }

        _lastAppliedProfileId = item.Id;
        ExplicitProfileApplyRequested?.Invoke(this, item.Profile);
    }

    /// <summary>Associates the selected adapter draft; null disables draft-specific commands.</summary>
    public void SetDraft(AdapterDraftViewModel? draft)
    {
        _draft = draft;
        SaveCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _catalog.Changed -= OnCatalogChanged;
    }

    partial void OnSearchTextChanged(string value) => RefreshFromCatalog();

    partial void OnSelectedProfileChanged(ProfileListItemViewModel? value)
    {
        if (_isRestoringSelection || value is null)
        {
            return;
        }

        if (_draft is null)
        {
            _previousSelectedProfile = value;
            return;
        }

        if (_previousSelectedProfile?.Id == value.Id)
        {
            _previousSelectedProfile = value;
            return;
        }

        if (_draft.IsDirty && !_confirmationService.Confirm(new UserConfirmationRequest(
                Strings.ConfirmProfileLoadTitle,
                Strings.ConfirmProfileLoadMessage)))
        {
            _isRestoringSelection = true;
            try
            {
                SelectedProfile = _previousSelectedProfile;
            }
            finally
            {
                _isRestoringSelection = false;
            }

            StatusMessage = Strings.ProfileLoadCancelled;
            return;
        }

        _draft.LoadProfile(value.Profile);
        _previousSelectedProfile = value;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (_draft is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(NewProfileName))
        {
            StatusMessage = Strings.ProfileNameRequired;
            return;
        }

        NetworkProfile profile = _draft.CreateProfile(NewProfileName, NewProfileDescription, NewProfileUsesDhcp);
        ProfileSaveResult result = await _catalog.SaveAsync(profile, CancellationToken.None);
        StatusMessage = ProfileResultMessageFormatter.Describe(result.Status);
        if (result.IsSuccess && result.Profile is not null)
        {
            ClearFilterAndSelect(result.Profile.ProfileId);
        }
    }

    [RelayCommand]
    private async Task RenameAsync(ProfileListItemViewModel? item)
    {
        NetworkProfile? profile = GetProfile(item);
        if (profile is null)
        {
            return;
        }

        string? name = _textInputService.Request(
            Strings.ProfileRenameTitle,
            Strings.ProfileNameLabel,
            profile.Name);
        if (name is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = Strings.ProfileNameRequired;
            return;
        }

        ProfileSaveResult result = await _catalog.SaveAsync(
            profile with { Name = name, ModifiedAtUtc = DateTimeOffset.UtcNow },
            CancellationToken.None);
        StatusMessage = ProfileResultMessageFormatter.Describe(result.Status);
        if (result.IsSuccess && result.Profile is not null)
        {
            ClearFilterAndSelect(result.Profile.ProfileId);
        }
    }

    [RelayCommand]
    private async Task DuplicateAsync(ProfileListItemViewModel? item)
    {
        NetworkProfile? profile = GetProfile(item);
        if (profile is null)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        ProfileSaveResult result = await _catalog.SaveAsync(profile with
        {
            ProfileId = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = now,
            ModifiedAtUtc = now
        }, CancellationToken.None);
        StatusMessage = ProfileResultMessageFormatter.Describe(result.Status);
        if (result.IsSuccess && result.Profile is not null)
        {
            ClearFilterAndSelect(result.Profile.ProfileId);
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(ProfileListItemViewModel? item)
    {
        NetworkProfile? profile = GetProfile(item);
        if (profile is null)
        {
            return;
        }

        ProfileSaveResult result = await _catalog.SaveAsync(
            profile with { IsFavorite = !profile.IsFavorite, ModifiedAtUtc = DateTimeOffset.UtcNow },
            CancellationToken.None);
        StatusMessage = ProfileResultMessageFormatter.Describe(result.Status);
    }

    [RelayCommand]
    private async Task DeleteAsync(ProfileListItemViewModel? item)
    {
        NetworkProfile? profile = GetProfile(item);
        if (profile is null || !_confirmationService.Confirm(new UserConfirmationRequest(
                Strings.ConfirmProfileDeleteTitle,
                Strings.FormatConfirmProfileDeleteMessage(profile.Name))))
        {
            return;
        }

        ProfileDeleteResult result = await _catalog.DeleteAsync(profile.ProfileId, CancellationToken.None);
        StatusMessage = ProfileResultMessageFormatter.Describe(result.Status);
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        using Stream? source = _fileDialogService.OpenProfile();
        if (source is null)
        {
            return;
        }

        ProfileImportResult result = await _catalog.ImportAsync(source, CancellationToken.None);
        StatusMessage = ProfileResultMessageFormatter.Describe(result);
        if (result.IsSuccess && result.Profile is not null)
        {
            ClearFilterAndSelect(result.Profile.ProfileId);
        }
    }

    [RelayCommand]
    private async Task ExportAsync(ProfileListItemViewModel? item)
    {
        NetworkProfile? profile = GetProfile(item);
        if (profile is null)
        {
            return;
        }

        using Stream? destination = _fileDialogService.CreateProfile(profile.Name + ".json");
        if (destination is null)
        {
            return;
        }

        ProfileExportResult result = await _catalog.ExportAsync(
            profile.ProfileId,
            destination,
            CancellationToken.None);
        StatusMessage = ProfileResultMessageFormatter.Describe(result.Status);
    }

    private bool CanSave() => _draft is not null && !string.IsNullOrWhiteSpace(NewProfileName);

    private NetworkProfile? GetProfile(ProfileListItemViewModel? item) =>
        (item ?? SelectedProfile)?.Profile;

    private void OnCatalogChanged(object? sender, EventArgs e) =>
        _uiDispatcher.Post(RefreshFromCatalog);

    private void ClearFilterAndSelect(string profileId)
    {
        SearchText = string.Empty;
        RefreshFromCatalog(profileId);
    }

    private void RefreshFromCatalog() => RefreshFromCatalog(SelectedProfile?.Id);

    private void RefreshFromCatalog(string? selectedId)
    {
        IEnumerable<ProfileListItemViewModel> profiles = _catalog.Profiles
            .Where(MatchesSearch)
            .OrderBy(profile => profile.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(profile => new ProfileListItemViewModel(profile));

        ProfileListItemViewModel[] orderedProfiles = profiles.ToArray();
        Groups.Clear();
        AddGroup(Strings.ProfileGroupFavorites, orderedProfiles.Where(profile => profile.IsFavorite));
        AddGroup(Strings.ProfileGroupOthers, orderedProfiles.Where(profile => !profile.IsFavorite));

        Problems.Clear();
        foreach (NetworkProfileProblem problem in _catalog.Problems)
        {
            Problems.Add(new ProfileProblemListItemViewModel(problem));
        }

        OnPropertyChanged(nameof(ProblemCount));
        OnPropertyChanged(nameof(HasProblems));
        OnPropertyChanged(nameof(ProblemsHeader));
        SelectedProfile = orderedProfiles.FirstOrDefault(profile => profile.Id == selectedId);
    }

    private bool MatchesSearch(NetworkProfile profile)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        string query = SearchText;
        return Contains(profile.Name, query) ||
            Contains(profile.Description, query) ||
            Contains(profile.Ipv4Address, query) ||
            Contains(profile.SubnetMask, query) ||
            Contains(profile.Gateway, query) ||
            Contains(profile.PrimaryDns, query) ||
            Contains(profile.SecondaryDns, query);
    }

    private static bool Contains(string? value, string query) =>
        value?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true;

    private void AddGroup(string name, IEnumerable<ProfileListItemViewModel> profiles)
    {
        ProfileListItemViewModel[] items = profiles.ToArray();
        if (items.Length > 0)
        {
            Groups.Add(new ProfileGroupViewModel(name, items));
        }
    }
}
