using System.IO;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Application.Profiles;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.ViewModels;

public sealed class ProfilePanelViewModelTests
{
    [Fact]
    public void Constructor_WhenProfilesExist_GroupsFavoritesFirstAndSortsEachGroup()
    {
        FakeProfileCatalog catalog = new()
        {
            Profiles =
            [
                TestData.Profile(profileId: "other-z", name: "Zulu"),
                TestData.Profile(profileId: "favorite-z", name: "Zulu favorite", isFavorite: true),
                TestData.Profile(profileId: "favorite-a", name: "Alpha favorite", isFavorite: true),
                TestData.Profile(profileId: "other-a", name: "Alpha")
            ]
        };
        using ProfilePanelViewModel viewModel = Create(catalog);

        Assert.Equal(2, viewModel.Groups.Count);
        Assert.Equal("Alpha favorite", viewModel.Groups[0].Profiles[0].Name);
        Assert.Equal("Zulu favorite", viewModel.Groups[0].Profiles[1].Name);
        Assert.Equal("Alpha", viewModel.Groups[1].Profiles[0].Name);
        Assert.Equal("Zulu", viewModel.Groups[1].Profiles[1].Name);
    }

    [Theory]
    [InlineData("controller")]
    [InlineData("192.168.1.50")]
    public void SearchText_WhenMatchesDescriptionOrIp_FiltersCaseInsensitively(string query)
    {
        FakeProfileCatalog catalog = new()
        {
            Profiles =
            [
                TestData.Profile(name: "PLC", description: "Factory Controller"),
                TestData.Profile(
                    profileId: "other",
                    name: "Camera",
                    description: "Security camera",
                    ipv4Address: "10.0.0.10")
            ]
        };
        using ProfilePanelViewModel viewModel = Create(catalog);

        viewModel.SearchText = query;

        Assert.Single(viewModel.Groups);
        Assert.Single(viewModel.Groups[0].Profiles);
        Assert.Equal("PLC", viewModel.Groups[0].Profiles[0].Name);
    }

    [Fact]
    public void ApplyOnExplicitSelection_WhenAlreadySelectedProfileIsClicked_DoesNotApply()
    {
        FakeProfileCatalog catalog = new()
        {
            Profiles = [TestData.Profile(profileId: "selected-profile")]
        };
        using ProfilePanelViewModel viewModel = Create(catalog);
        ProfileListItemViewModel profile = viewModel.Groups[0].Profiles[0];
        int requests = 0;
        viewModel.ExplicitProfileApplyRequested += (_, _) => requests++;
        viewModel.SetApplyProfileOnSelection(true);
        viewModel.SelectedProfile = profile;

        viewModel.BeginExplicitProfileSelection();
        viewModel.ApplyOnExplicitSelection(profile);

        Assert.Equal(0, requests);
    }

    [Fact]
    public void ApplyOnExplicitSelection_WhenApplyFails_AllowsSelectingTheSameProfileAgain()
    {
        FakeProfileCatalog catalog = new()
        {
            Profiles =
            [
                TestData.Profile(profileId: "first-profile", name: "First"),
                TestData.Profile(profileId: "retry-profile", name: "Retry")
            ]
        };
        using ProfilePanelViewModel viewModel = Create(catalog);
        ProfileListItemViewModel first = viewModel.Groups[0].Profiles[0];
        ProfileListItemViewModel retry = viewModel.Groups[0].Profiles[1];
        int requests = 0;
        viewModel.ExplicitProfileApplyRequested += (_, _) => requests++;
        viewModel.SetApplyProfileOnSelection(true);
        viewModel.SelectedProfile = first;

        viewModel.BeginExplicitProfileSelection();
        viewModel.SelectedProfile = retry;
        viewModel.ApplyOnExplicitSelection(retry);
        viewModel.CompleteExplicitProfileApply(succeeded: false);

        viewModel.BeginExplicitProfileSelection();
        viewModel.ApplyOnExplicitSelection(retry);

        Assert.Equal(2, requests);
    }

    [Fact]
    public void SelectedProfile_WhenDhcpProfileSelected_ClearsStaticDraftAndShowsGuidance()
    {
        FakeProfileCatalog catalog = new()
        {
            Profiles =
            [TestData.Profile(mode: NetworkConfigurationMode.Dhcp, ipv4Address: "10.0.0.10")]
        };
        FakeUserConfirmationService confirmation = new() { Answer = true };
        using ProfilePanelViewModel viewModel = Create(catalog, confirmation);
        AdapterDraftViewModel draft = new(new FakeClipboardService(), new StaticIpv4ConfigurationValidator());
        draft.UpdateCurrentValues(TestData.Snapshot(mode: NetworkConfigurationMode.Static));
        viewModel.SetDraft(draft);

        viewModel.SelectedProfile = viewModel.Groups[0].Profiles[0];

        Assert.Equal(string.Empty, draft.Ipv4Address);
        Assert.NotEmpty(draft.ProfileGuidance);
    }

    [Fact]
    public void SelectedProfile_WhenDirtyDraftLoadIsDeclined_RestoresPreviousSelection()
    {
        FakeProfileCatalog catalog = new()
        {
            Profiles =
            [
                TestData.Profile(profileId: "first", name: "First", ipv4Address: "10.0.0.10"),
                TestData.Profile(profileId: "second", name: "Second", ipv4Address: "10.0.0.20")
            ]
        };
        FakeUserConfirmationService confirmation = new() { Answer = true };
        using ProfilePanelViewModel viewModel = Create(catalog, confirmation);
        AdapterDraftViewModel draft = new(new FakeClipboardService(), new StaticIpv4ConfigurationValidator());
        viewModel.SetDraft(draft);
        ProfileListItemViewModel first = viewModel.Groups[0].Profiles[0];

        viewModel.SelectedProfile = first;
        draft.Ipv4Address = "10.0.0.99";
        confirmation.Answer = false;
        viewModel.SelectedProfile = viewModel.Groups[0].Profiles[1];

        Assert.Same(first, viewModel.SelectedProfile);
        Assert.Equal("10.0.0.99", draft.Ipv4Address);
    }

    [Fact]
    public void CatalogChanged_WhenProblemsReported_UpdatesInformationalProblemList()
    {
        FakeProfileCatalog catalog = new()
        {
            Problems =
            [new NetworkProfileProblem("C:\\profiles\\broken.json", ProfileLoadFailureKind.MalformedJson)]
        };
        using ProfilePanelViewModel viewModel = Create(catalog);

        catalog.RaiseChanged();

        ProfileProblemListItemViewModel problem = Assert.Single(viewModel.Problems);
        Assert.Equal("broken.json", problem.FileName);
        Assert.Equal(1, viewModel.ProblemCount);
        Assert.True(viewModel.HasProblems);
        Assert.NotEmpty(problem.Reason);
    }

    [Fact]
    public async Task SaveCommand_WhenDraftAndNameAreAvailable_SavesDraftProfileAndSelectsResult()
    {
        FakeProfileCatalog catalog = new();
        using ProfilePanelViewModel viewModel = Create(catalog);
        AdapterDraftViewModel draft = new(new FakeClipboardService(), new StaticIpv4ConfigurationValidator());
        draft.UpdateCurrentValues(TestData.Snapshot(
            ipv4Address: "10.0.0.5",
            subnetMask: "255.255.255.0",
            gateway: "10.0.0.1"));
        viewModel.SetDraft(draft);
        viewModel.NewProfileName = "PLC";
        viewModel.NewProfileDescription = "Hat 1";

        await viewModel.SaveCommand.ExecuteAsync(null);

        NetworkProfile saved = Assert.Single(catalog.SavedProfiles);
        Assert.Equal("PLC", saved.Name);
        Assert.Equal("Hat 1", saved.Description);
        Assert.Equal("10.0.0.5", saved.Ipv4Address);
        Assert.Equal(Strings.ProfileSaveSuccess, viewModel.StatusMessage);
        Assert.Equal(saved.ProfileId, viewModel.SelectedProfile!.Id);
    }

    [Fact]
    public async Task DuplicateCommand_WhenCatalogResolvesNameCollision_UsesNewIdWithoutPromptAndSelectsResolvedName()
    {
        NetworkProfile original = TestData.Profile(profileId: "original", name: "PLC");
        NetworkProfile duplicate = original with { ProfileId = "duplicate", Name = "PLC (1)" };
        FakeProfileCatalog catalog = new()
        {
            Profiles = [original],
            SaveResult = ProfileSaveResult.Success(duplicate)
        };
        FakeUserTextInputService input = new();
        using ProfilePanelViewModel viewModel = Create(catalog, textInput: input);

        await viewModel.DuplicateCommand.ExecuteAsync(viewModel.Groups[0].Profiles[0]);

        NetworkProfile saved = Assert.Single(catalog.SavedProfiles);
        Assert.NotEqual(original.ProfileId, saved.ProfileId);
        Assert.Equal("PLC", saved.Name);
        Assert.Empty(input.Requests);
        Assert.Equal("duplicate", viewModel.SelectedProfile!.Id);
    }

    [Fact]
    public async Task RenameCommand_WhenInputIsCancelled_DoesNotSave()
    {
        FakeProfileCatalog catalog = new()
        {
            Profiles = [TestData.Profile()]
        };
        FakeUserTextInputService input = new() { Response = null };
        using ProfilePanelViewModel viewModel = Create(catalog, textInput: input);

        await viewModel.RenameCommand.ExecuteAsync(viewModel.Groups[0].Profiles[0]);

        Assert.Empty(catalog.SavedProfiles);
        Assert.Single(input.Requests);
    }

    [Fact]
    public async Task RenameCommand_WhenInputIsProvided_SavesRenamedProfile()
    {
        NetworkProfile profile = TestData.Profile(profileId: "rename-me", name: "Eski ad");
        FakeProfileCatalog catalog = new()
        {
            Profiles = [profile]
        };
        FakeUserTextInputService input = new() { Response = "Yeni ad" };
        using ProfilePanelViewModel viewModel = Create(catalog, textInput: input);

        await viewModel.RenameCommand.ExecuteAsync(viewModel.Groups[0].Profiles[0]);

        NetworkProfile saved = Assert.Single(catalog.SavedProfiles);
        Assert.Equal("rename-me", saved.ProfileId);
        Assert.Equal("Yeni ad", saved.Name);
        Assert.Equal("Eski ad", Assert.Single(input.Requests).InitialValue);
        Assert.Equal("rename-me", viewModel.SelectedProfile!.Id);
    }

    [Fact]
    public async Task ToggleFavoriteCommand_WhenProfileIsSelected_SavesOppositeFavoriteState()
    {
        FakeProfileCatalog catalog = new()
        {
            Profiles = [TestData.Profile(isFavorite: false)]
        };
        using ProfilePanelViewModel viewModel = Create(catalog);

        await viewModel.ToggleFavoriteCommand.ExecuteAsync(viewModel.Groups[0].Profiles[0]);

        Assert.True(Assert.Single(catalog.SavedProfiles).IsFavorite);
    }

    [Fact]
    public async Task DeleteCommand_WhenConfirmationIsDeclined_DoesNotDelete()
    {
        FakeProfileCatalog catalog = new()
        {
            Profiles = [TestData.Profile()]
        };
        FakeUserConfirmationService confirmation = new() { Answer = false };
        using ProfilePanelViewModel viewModel = Create(catalog, confirmation);

        await viewModel.DeleteCommand.ExecuteAsync(viewModel.Groups[0].Profiles[0]);

        Assert.Empty(catalog.DeletedProfileIds);
        Assert.Single(confirmation.Requests);
    }

    [Fact]
    public async Task DeleteCommand_WhenConfirmationIsAccepted_DeletesSelectedProfile()
    {
        NetworkProfile profile = TestData.Profile(profileId: "delete-me");
        FakeProfileCatalog catalog = new()
        {
            Profiles = [profile]
        };
        using ProfilePanelViewModel viewModel = Create(catalog, new FakeUserConfirmationService { Answer = true });

        await viewModel.DeleteCommand.ExecuteAsync(viewModel.Groups[0].Profiles[0]);

        Assert.Equal("delete-me", Assert.Single(catalog.DeletedProfileIds));
        Assert.Equal(Strings.ProfileDeleteSuccess, viewModel.StatusMessage);
    }

    [Fact]
    public async Task ImportCommand_WhenDialogIsCancelled_DoesNotCallCatalog()
    {
        FakeProfileCatalog catalog = new();
        using ProfilePanelViewModel viewModel = Create(catalog);

        await viewModel.ImportCommand.ExecuteAsync(null);

        Assert.Empty(catalog.ImportedSources);
        Assert.Empty(viewModel.StatusMessage);
    }

    [Fact]
    public async Task ImportCommand_WhenContentIsInvalid_DisposesSourceAndShowsMappedStatus()
    {
        TrackingMemoryStream source = new([1, 2, 3]);
        FakeProfileCatalog catalog = new()
        {
            ImportResult = ProfileImportResult.Failed(ProfileImportStatus.InvalidContent)
        };
        using ProfilePanelViewModel viewModel = Create(
            catalog,
            fileDialog: new FakeProfileFileDialogService { Source = source });

        await viewModel.ImportCommand.ExecuteAsync(null);

        Assert.Same(source, Assert.Single(catalog.ImportedSources));
        Assert.True(source.IsDisposed);
        Assert.Equal(Strings.ProfileInvalidContent, viewModel.StatusMessage);
        Assert.Null(viewModel.SelectedProfile);
    }

    [Fact]
    public async Task ImportCommand_WhenCatalogImportsResolvedName_DisposesSourceAndNamesImportedProfile()
    {
        NetworkProfile imported = TestData.Profile(profileId: "imported", name: "PLC (1)");
        TrackingMemoryStream source = new([1, 2, 3]);
        FakeProfileCatalog catalog = new()
        {
            ImportResult = ProfileImportResult.Success(imported)
        };
        using ProfilePanelViewModel viewModel = Create(
            catalog,
            fileDialog: new FakeProfileFileDialogService { Source = source });
        viewModel.SearchText = "does-not-match";

        await viewModel.ImportCommand.ExecuteAsync(null);

        Assert.True(source.IsDisposed);
        Assert.Empty(viewModel.SearchText);
        Assert.Equal("imported", viewModel.SelectedProfile!.Id);
        Assert.Equal(Strings.FormatProfileImportSuccess("PLC (1)"), viewModel.StatusMessage);
    }

    [Fact]
    public async Task ExportCommand_WhenDialogIsCancelled_DoesNotCallCatalog()
    {
        FakeProfileCatalog catalog = new()
        {
            Profiles = [TestData.Profile()]
        };
        using ProfilePanelViewModel viewModel = Create(catalog);

        await viewModel.ExportCommand.ExecuteAsync(viewModel.Groups[0].Profiles[0]);

        Assert.Empty(catalog.ExportedProfiles);
    }

    [Fact]
    public async Task ExportCommand_WhenDestinationChosen_DelegatesAndDisposesDestination()
    {
        TrackingMemoryStream destination = new();
        FakeProfileCatalog catalog = new()
        {
            Profiles = [TestData.Profile(profileId: "export-me", name: "PLC")]
        };
        FakeProfileFileDialogService dialog = new() { Destination = destination };
        using ProfilePanelViewModel viewModel = Create(catalog, fileDialog: dialog);

        await viewModel.ExportCommand.ExecuteAsync(viewModel.Groups[0].Profiles[0]);

        (string profileId, Stream stream) = Assert.Single(catalog.ExportedProfiles);
        Assert.Equal("export-me", profileId);
        Assert.Same(destination, stream);
        Assert.Equal("PLC.json", dialog.SuggestedFileName);
        Assert.True(destination.IsDisposed);
        Assert.Equal(Strings.ProfileExportSuccess, viewModel.StatusMessage);
    }

    [Fact]
    public void ProfileResultMessageFormatter_WhenGivenEveryStatus_ReturnsLocalizedText()
    {
        Assert.All(Enum.GetValues<ProfileSaveStatus>(), status =>
            Assert.False(string.IsNullOrWhiteSpace(ProfileResultMessageFormatter.Describe(status))));
        Assert.All(Enum.GetValues<ProfileDeleteStatus>(), status =>
            Assert.False(string.IsNullOrWhiteSpace(ProfileResultMessageFormatter.Describe(status))));
        Assert.All(Enum.GetValues<ProfileImportStatus>(), status =>
            Assert.False(string.IsNullOrWhiteSpace(ProfileResultMessageFormatter.Describe(status))));
        Assert.All(Enum.GetValues<ProfileExportStatus>(), status =>
            Assert.False(string.IsNullOrWhiteSpace(ProfileResultMessageFormatter.Describe(status))));
        Assert.All(Enum.GetValues<ProfileLoadFailureKind>(), status =>
            Assert.False(string.IsNullOrWhiteSpace(ProfileResultMessageFormatter.Describe(status))));
    }

    [Fact]
    public void ProfileResultMessageFormatter_WhenGivenStatuses_UsesExpectedTurkishResources()
    {
        Assert.Equal(Strings.ProfileSaveSuccess, ProfileResultMessageFormatter.Describe(ProfileSaveStatus.Success));
        Assert.Equal(Strings.ProfileInvalidContent, ProfileResultMessageFormatter.Describe(ProfileSaveStatus.InvalidContent));
        Assert.Equal(Strings.ProfileAccessDenied, ProfileResultMessageFormatter.Describe(ProfileSaveStatus.AccessDenied));
        Assert.Equal(Strings.ProfileIoFailure, ProfileResultMessageFormatter.Describe(ProfileSaveStatus.IoFailure));
        Assert.Equal(Strings.ProfileDeleteSuccess, ProfileResultMessageFormatter.Describe(ProfileDeleteStatus.Success));
        Assert.Equal(Strings.ProfileNotFound, ProfileResultMessageFormatter.Describe(ProfileDeleteStatus.NotFound));
        Assert.Equal(Strings.ProfileAccessDenied, ProfileResultMessageFormatter.Describe(ProfileDeleteStatus.AccessDenied));
        Assert.Equal(Strings.ProfileIoFailure, ProfileResultMessageFormatter.Describe(ProfileDeleteStatus.IoFailure));
        Assert.Equal(Strings.ProfileImportSuccess, ProfileResultMessageFormatter.Describe(ProfileImportStatus.Success));
        Assert.Equal(Strings.ProfileInvalidContent, ProfileResultMessageFormatter.Describe(ProfileImportStatus.InvalidContent));
        Assert.Equal(Strings.ProfileAccessDenied, ProfileResultMessageFormatter.Describe(ProfileImportStatus.AccessDenied));
        Assert.Equal(Strings.ProfileIoFailure, ProfileResultMessageFormatter.Describe(ProfileImportStatus.IoFailure));
        Assert.Equal(Strings.ProfileExportSuccess, ProfileResultMessageFormatter.Describe(ProfileExportStatus.Success));
        Assert.Equal(Strings.ProfileNotFound, ProfileResultMessageFormatter.Describe(ProfileExportStatus.NotFound));
        Assert.Equal(Strings.ProfileAccessDenied, ProfileResultMessageFormatter.Describe(ProfileExportStatus.AccessDenied));
        Assert.Equal(Strings.ProfileIoFailure, ProfileResultMessageFormatter.Describe(ProfileExportStatus.IoFailure));
        Assert.Equal(Strings.ProfileProblemMalformedJson, ProfileResultMessageFormatter.Describe(ProfileLoadFailureKind.MalformedJson));
        Assert.Equal(Strings.ProfileProblemUnsupportedSchema, ProfileResultMessageFormatter.Describe(ProfileLoadFailureKind.UnsupportedSchemaVersion));
        Assert.Equal(Strings.ProfileProblemInvalidContent, ProfileResultMessageFormatter.Describe(ProfileLoadFailureKind.InvalidContent));
        Assert.Equal(Strings.ProfileProblemReadFailure, ProfileResultMessageFormatter.Describe(ProfileLoadFailureKind.ReadFailure));
    }

    private static ProfilePanelViewModel Create(
        FakeProfileCatalog catalog,
        FakeUserConfirmationService? confirmation = null,
        FakeProfileFileDialogService? fileDialog = null,
        FakeUserTextInputService? textInput = null) => new(
            catalog,
            fileDialog ?? new FakeProfileFileDialogService(),
            textInput ?? new FakeUserTextInputService(),
            confirmation ?? new FakeUserConfirmationService(),
            new FakeUiDispatcher());

    private sealed class FakeProfileCatalog : IProfileCatalog
    {
        public IReadOnlyList<NetworkProfile> Profiles { get; set; } = [];

        public IReadOnlyList<NetworkProfileProblem> Problems { get; set; } = [];

        public ProfileSaveResult? SaveResult { get; set; }

        public ProfileDeleteResult DeleteResult { get; set; } = ProfileDeleteResult.Success();

        public ProfileImportResult ImportResult { get; set; } =
            ProfileImportResult.Failed(ProfileImportStatus.InvalidContent);

        public ProfileExportResult ExportResult { get; set; } = ProfileExportResult.Success();

        public List<NetworkProfile> SavedProfiles { get; } = new();

        public List<string> DeletedProfileIds { get; } = new();

        public List<Stream> ImportedSources { get; } = new();

        public List<(string ProfileId, Stream Stream)> ExportedProfiles { get; } = new();

        public event EventHandler? Changed;

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void StartWatching()
        {
        }

        public void StopWatching()
        {
        }

        public Task<ProfileSaveResult> SaveAsync(NetworkProfile profile, CancellationToken cancellationToken)
        {
            SavedProfiles.Add(profile);
            ProfileSaveResult result = SaveResult ?? ProfileSaveResult.Success(profile);
            if (result.IsSuccess && result.Profile is not null)
            {
                Profiles = Profiles
                    .Where(existing => existing.ProfileId != result.Profile.ProfileId)
                    .Append(result.Profile)
                    .ToArray();
                RaiseChanged();
            }

            return Task.FromResult(result);
        }

        public Task<ProfileDeleteResult> DeleteAsync(string profileId, CancellationToken cancellationToken)
        {
            DeletedProfileIds.Add(profileId);
            if (DeleteResult.IsSuccess)
            {
                Profiles = Profiles.Where(profile => profile.ProfileId != profileId).ToArray();
                RaiseChanged();
            }

            return Task.FromResult(DeleteResult);
        }

        public Task<ProfileImportResult> ImportAsync(Stream source, CancellationToken cancellationToken)
        {
            ImportedSources.Add(source);
            if (ImportResult.IsSuccess && ImportResult.Profile is not null)
            {
                Profiles = Profiles.Append(ImportResult.Profile).ToArray();
                RaiseChanged();
            }

            return Task.FromResult(ImportResult);
        }

        public Task<ProfileExportResult> ExportAsync(
            string profileId,
            Stream destination,
            CancellationToken cancellationToken)
        {
            ExportedProfiles.Add((profileId, destination));
            return Task.FromResult(ExportResult);
        }

        public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

        public void Dispose()
        {
        }
    }

    private sealed class FakeProfileFileDialogService : IProfileFileDialogService
    {
        public Stream? Source { get; set; }

        public Stream? Destination { get; set; }

        public string? SuggestedFileName { get; private set; }

        public Stream? OpenProfile() => Source;

        public Stream? CreateProfile(string suggestedFileName)
        {
            SuggestedFileName = suggestedFileName;
            return Destination;
        }
    }

    private sealed class FakeUserTextInputService : IUserTextInputService
    {
        public List<(string Title, string Label, string InitialValue)> Requests { get; } = new();

        public string? Response { get; set; }

        public string? Request(string title, string label, string initialValue)
        {
            Requests.Add((title, label, initialValue));
            return Response;
        }
    }

    private sealed class TrackingMemoryStream : MemoryStream
    {
        public TrackingMemoryStream()
        {
        }

        public TrackingMemoryStream(byte[] buffer)
            : base(buffer)
        {
        }

        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
