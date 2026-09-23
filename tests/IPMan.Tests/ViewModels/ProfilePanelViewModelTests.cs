using System.IO;
using IPMan.App.Presentation;
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

    private static ProfilePanelViewModel Create(
        FakeProfileCatalog catalog,
        FakeUserConfirmationService? confirmation = null) => new(
            catalog,
            new FakeProfileFileDialogService(),
            new FakeUserTextInputService(),
            confirmation ?? new FakeUserConfirmationService(),
            new FakeUiDispatcher());

    private sealed class FakeProfileCatalog : IProfileCatalog
    {
        public IReadOnlyList<NetworkProfile> Profiles { get; set; } = [];

        public IReadOnlyList<NetworkProfileProblem> Problems { get; set; } = [];

        public event EventHandler? Changed;

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void StartWatching()
        {
        }

        public void StopWatching()
        {
        }

        public Task<ProfileSaveResult> SaveAsync(NetworkProfile profile, CancellationToken cancellationToken) =>
            Task.FromResult(ProfileSaveResult.Success(profile));

        public Task<ProfileDeleteResult> DeleteAsync(string profileId, CancellationToken cancellationToken) =>
            Task.FromResult(ProfileDeleteResult.Success());

        public Task<ProfileImportResult> ImportAsync(Stream source, CancellationToken cancellationToken) =>
            Task.FromResult(ProfileImportResult.Failed(ProfileImportStatus.InvalidContent));

        public Task<ProfileExportResult> ExportAsync(
            string profileId,
            Stream destination,
            CancellationToken cancellationToken) => Task.FromResult(ProfileExportResult.Success());

        public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

        public void Dispose()
        {
        }
    }

    private sealed class FakeProfileFileDialogService : IProfileFileDialogService
    {
        public Stream? OpenProfile() => null;

        public Stream? CreateProfile(string suggestedFileName) => null;
    }

    private sealed class FakeUserTextInputService : IUserTextInputService
    {
        public string? Request(string title, string label, string initialValue) => null;
    }
}
