using System.IO;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Application.Profiles;
using IPMan.Domain.Profiles;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    private static readonly DateTimeOffset RefreshedAt = new(2026, 8, 7, 21, 5, 9, TimeSpan.Zero);

    [Fact]
    public void BeforeFirstRefresh_TheViewModelIsInTheLoadingState()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();

        harness.ViewModel.Initialize();

        Assert.True(harness.ViewModel.IsLoading);
        Assert.False(harness.ViewModel.IsContentVisible);
        Assert.False(harness.ViewModel.IsEmptyStateVisible);
        Assert.Equal(Strings.StateLoading, harness.ViewModel.StatusBar.ApplicationState);
    }

    [Fact]
    public void Initialize_StartsTheRefreshCoordinatorExactlyOnce()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();

        harness.ViewModel.Initialize();
        harness.ViewModel.Initialize();

        Assert.Equal(1, harness.Coordinator.StartCount);
    }

    [Fact]
    public void FirstRefresh_SelectsTheFirstAdapterInDiscoveryOrder()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();

        harness.Coordinator.PublishRefresh(
            TestData.Snapshot(id: "{A}", name: "Ethernet"),
            TestData.Snapshot(id: "{B}", name: "Wi-Fi"));

        Assert.Equal(2, harness.ViewModel.Adapters.Count);
        Assert.Equal("{A}", harness.ViewModel.SelectedAdapter?.Id.Value);
        Assert.False(harness.ViewModel.IsLoading);
        Assert.True(harness.ViewModel.IsContentVisible);
    }

    [Fact]
    public void Refresh_PreservesTheSelectedAdapterIdentity()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();
        harness.Coordinator.PublishRefresh(
            TestData.Snapshot(id: "{A}"),
            TestData.Snapshot(id: "{B}"));

        harness.ViewModel.SelectedAdapter = harness.ViewModel.Adapters[1];

        harness.Coordinator.PublishRefresh(
            TestData.Snapshot(id: "{A}"),
            TestData.Snapshot(id: "{B}", isConnected: false));

        Assert.Equal("{B}", harness.ViewModel.SelectedAdapter?.Id.Value);
    }

    [Fact]
    public void Refresh_PreservesSelectionEvenWhenTheAdapterIsRenamedOrReordered()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();
        harness.Coordinator.PublishRefresh(
            TestData.Snapshot(id: "{A}", name: "Ethernet"),
            TestData.Snapshot(id: "{B}", name: "Wi-Fi"));

        harness.ViewModel.SelectedAdapter = harness.ViewModel.Adapters[1];

        harness.Coordinator.PublishRefresh(
            TestData.Snapshot(id: "{B}", name: "Wi-Fi (yeni ad)"),
            TestData.Snapshot(id: "{A}", name: "Ethernet"));

        Assert.Equal("{B}", harness.ViewModel.SelectedAdapter?.Id.Value);
        Assert.Equal("Wi-Fi (yeni ad)", harness.ViewModel.SelectedAdapter?.DisplayName);
        Assert.Equal("{B}", harness.ViewModel.Adapters[0].Id.Value);
    }

    [Fact]
    public void Refresh_ReusesTheExistingTabViewModelForAKnownAdapter()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();
        harness.Coordinator.PublishRefresh(TestData.Snapshot(id: "{A}"));

        AdapterViewModel before = harness.ViewModel.Adapters[0];
        harness.Coordinator.PublishRefresh(TestData.Snapshot(id: "{A}", isConnected: false));

        Assert.Same(before, harness.ViewModel.Adapters[0]);
    }

    [Fact]
    public void Refresh_WhenPersistedAdapterGuidExists_RestoresThatSelection()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.SetLastSelectedAdapterId("827a2938-bb14-4d18-b67f-94e9c4f818ba");
        harness.ViewModel.Initialize();

        harness.Coordinator.PublishRefresh(
            TestData.Snapshot(id: "{927A2938-BB14-4D18-B67F-94E9C4F818BA}", name: "Ethernet"),
            TestData.Snapshot(id: "{827A2938-BB14-4D18-B67F-94E9C4F818BA}", name: "Wi-Fi"));

        Assert.Equal("{827A2938-BB14-4D18-B67F-94E9C4F818BA}", harness.ViewModel.SelectedAdapter?.Id.Value);
    }

    [Fact]
    public void Refresh_WhenPersistedAdapterGuidIsMissing_SelectsFirstAdapter()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.SetLastSelectedAdapterId("827a2938-bb14-4d18-b67f-94e9c4f818ba");
        harness.ViewModel.Initialize();

        harness.Coordinator.PublishRefresh(TestData.Snapshot(
            id: "{927A2938-BB14-4D18-B67F-94E9C4F818BA}",
            name: "Ethernet"));

        Assert.Equal("{927A2938-BB14-4D18-B67F-94E9C4F818BA}", harness.ViewModel.SelectedAdapter?.Id.Value);
        Assert.Equal(harness.ViewModel.SelectedAdapter?.Id.Value, harness.ViewModel.LastSelectedAdapterId);
    }

    [Fact]
    public void Refresh_AddsNewlyDiscoveredAdaptersWithoutRestart()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();
        harness.Coordinator.PublishRefresh(TestData.Snapshot(id: "{A}"));

        harness.Coordinator.PublishRefresh(
            TestData.Snapshot(id: "{A}"),
            TestData.Snapshot(id: "{USB}", name: "USB Ethernet"));

        Assert.Equal(2, harness.ViewModel.Adapters.Count);
        Assert.Equal("{USB}", harness.ViewModel.Adapters[1].Id.Value);
        Assert.Equal("{A}", harness.ViewModel.SelectedAdapter?.Id.Value);
    }

    [Fact]
    public void WhenTheSelectedAdapterDisappears_AValidRemainingAdapterIsSelected()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();
        harness.Coordinator.PublishRefresh(
            TestData.Snapshot(id: "{A}"),
            TestData.Snapshot(id: "{USB}"));

        harness.ViewModel.SelectedAdapter = harness.ViewModel.Adapters[1];
        harness.Coordinator.PublishRefresh(TestData.Snapshot(id: "{A}"));

        Assert.Equal("{A}", harness.ViewModel.SelectedAdapter?.Id.Value);
        Assert.Single(harness.ViewModel.Adapters);
    }

    [Fact]
    public void WhenEveryAdapterDisappears_SelectionIsClearedAndTheEmptyStateIsShown()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();
        harness.Coordinator.PublishRefresh(TestData.Snapshot(id: "{A}"));

        harness.Coordinator.PublishRefresh();

        Assert.Null(harness.ViewModel.SelectedAdapter);
        Assert.Empty(harness.ViewModel.Adapters);
        Assert.True(harness.ViewModel.IsEmptyStateVisible);
        Assert.False(harness.ViewModel.IsContentVisible);
        Assert.Equal(Strings.StatusNoSelectedAdapter, harness.ViewModel.StatusBar.SelectedAdapterState);
    }

    [Fact]
    public void WhenNoAdapterExistsAtStartup_TheEmptyStateIsShownWithoutError()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();

        harness.Coordinator.PublishRefresh();

        Assert.True(harness.ViewModel.IsEmptyStateVisible);
        Assert.False(harness.ViewModel.HasRefreshError);
    }

    [Fact]
    public void DraftsOfDifferentAdaptersStayIndependent()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();
        harness.Coordinator.PublishRefresh(
            TestData.Snapshot(id: "{A}", ipv4Address: "10.0.0.5"),
            TestData.Snapshot(id: "{B}", ipv4Address: "10.0.1.5"));

        AdapterViewModel ethernet = harness.ViewModel.Adapters[0];
        AdapterViewModel wifi = harness.ViewModel.Adapters[1];

        ethernet.Draft.Ipv4Address = "10.0.0.99";

        // Switching tabs must not apply or discard anything.
        harness.ViewModel.SelectedAdapter = wifi;
        harness.ViewModel.SelectedAdapter = ethernet;

        Assert.Equal("10.0.0.99", ethernet.Draft.Ipv4Address);
        Assert.True(ethernet.Draft.IsDirty);
        Assert.Equal("10.0.1.5", wifi.Draft.Ipv4Address);
        Assert.False(wifi.Draft.IsDirty);
    }

    [Fact]
    public void RefreshDoesNotDestroyADirtyDraftButStillUpdatesCurrentValues()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();
        harness.Coordinator.PublishRefresh(TestData.Snapshot(id: "{A}", ipv4Address: "10.0.0.5"));

        AdapterViewModel adapter = harness.ViewModel.Adapters[0];
        adapter.Draft.Ipv4Address = "10.0.0.99";

        harness.Coordinator.PublishRefresh(TestData.Snapshot(id: "{A}", ipv4Address: "10.0.0.6"));

        Assert.Equal("10.0.0.99", adapter.Draft.Ipv4Address);
        Assert.Equal("10.0.0.6", adapter.Details.Ipv4Address);
    }

    [Fact]
    public void CurrentPanel_ShowsTheLatestSnapshotAndMarksMissingValues()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();

        harness.Coordinator.PublishRefresh(TestData.Snapshot(
            id: "{A}",
            name: "Ethernet",
            gateway: null,
            secondaryDns: null,
            linkSpeedBitsPerSecond: null));

        AdapterDetailsViewModel details = harness.ViewModel.Adapters[0].Details;

        Assert.Equal("Ethernet", details.Name);
        Assert.Equal(Strings.ValueUnavailable, details.Gateway);
        Assert.Equal(Strings.ValueUnavailable, details.SecondaryDns);
        Assert.Equal(Strings.ValueUnavailable, details.LinkSpeed);
    }

    [Fact]
    public void CurrentPanel_KeepsAdditionalIpv4AddressesVisible()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();

        harness.Coordinator.PublishRefresh(TestData.Snapshot(
            id: "{A}",
            ipv4Address: "192.168.1.50",
            ipv4Addresses: new IPMan.Domain.Networking.Ipv4AddressCollection(new[]
            {
                new IPMan.Domain.Networking.Ipv4AddressAssignment("192.168.1.50", "255.255.255.0"),
                new IPMan.Domain.Networking.Ipv4AddressAssignment("192.168.1.60", "255.255.255.0")
            })));

        AdapterDetailsViewModel details = harness.ViewModel.Adapters[0].Details;

        Assert.True(details.HasAdditionalIpv4Addresses);
        Assert.Equal("192.168.1.60", Assert.Single(details.AdditionalIpv4AddressList).Split('/')[0]);
    }

    [Fact]
    public void RefreshFailure_IsSurfacedWithoutDiscardingTheLastValidState()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();
        harness.Coordinator.PublishRefresh(TestData.Snapshot(id: "{A}", name: "Ethernet"));

        harness.Coordinator.PublishFailure();

        Assert.True(harness.ViewModel.HasRefreshError);
        Assert.Single(harness.ViewModel.Adapters);
        Assert.Equal("{A}", harness.ViewModel.SelectedAdapter?.Id.Value);
        Assert.Equal(Strings.StateError, harness.ViewModel.StatusBar.ApplicationState);
    }

    [Fact]
    public void RefreshFailureBeforeAnyResult_LeavesLoadingAndShowsTheEmptyState()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();

        harness.Coordinator.PublishFailure();

        Assert.False(harness.ViewModel.IsLoading);
        Assert.True(harness.ViewModel.HasRefreshError);
        Assert.True(harness.ViewModel.IsEmptyStateVisible);
    }

    [Fact]
    public void ASuccessfulRefreshAfterAFailure_ClearsTheErrorState()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();
        harness.Coordinator.PublishFailure();

        harness.Coordinator.PublishRefresh(TestData.Snapshot(id: "{A}"));

        Assert.False(harness.ViewModel.HasRefreshError);
        Assert.Equal(Strings.StateReady, harness.ViewModel.StatusBar.ApplicationState);
    }

    [Fact]
    public void StatusBar_ReportsAdministratorStateAndVersion()
    {
        using MainWindowHarness harness = MainWindowHarness.Create(isElevated: true, version: "1.2.3");

        Assert.Equal(Strings.StatusAdministratorYes, harness.ViewModel.StatusBar.AdministratorState);
        Assert.Contains("1.2.3", harness.ViewModel.StatusBar.Version, StringComparison.Ordinal);
    }

    [Fact]
    public void StatusBar_ReportsMissingAdministratorRights()
    {
        using MainWindowHarness harness = MainWindowHarness.Create(isElevated: false);

        Assert.Equal(Strings.StatusAdministratorNo, harness.ViewModel.StatusBar.AdministratorState);
    }

    [Fact]
    public void StatusBar_ReportsTheSelectedAdapterAndLastSuccessfulRefresh()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();

        Assert.Equal(Strings.StatusLastRefreshNever, harness.ViewModel.StatusBar.LastRefresh);

        harness.Coordinator.PublishRefresh(TestData.Snapshot(id: "{A}", name: "Ethernet"));

        Assert.Contains("Ethernet", harness.ViewModel.StatusBar.SelectedAdapterState, StringComparison.Ordinal);
        Assert.Contains("Bağlı", harness.ViewModel.StatusBar.SelectedAdapterState, StringComparison.Ordinal);
        Assert.NotEqual(Strings.StatusLastRefreshNever, harness.ViewModel.StatusBar.LastRefresh);
    }

    [Fact]
    public void AdapterUpdates_AreMarshalledThroughTheUiDispatcher()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();

        harness.Coordinator.PublishRefresh(TestData.Snapshot(id: "{A}"));
        harness.Coordinator.PublishFailure();

        Assert.Equal(2, harness.Dispatcher.PostCount);
    }

    [Fact]
    public void Dispose_StopsObservingTheCoordinator()
    {
        MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();
        harness.Coordinator.PublishRefresh(TestData.Snapshot(id: "{A}"));

        harness.Dispose();
        harness.Coordinator.PublishRefresh(
            TestData.Snapshot(id: "{A}"),
            TestData.Snapshot(id: "{B}"));

        Assert.Single(harness.ViewModel.Adapters);
    }

    [Fact]
    public void SelectionChange_AttachesActionsToTheSelectedAdapter()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();
        harness.Coordinator.PublishRefresh(
            TestData.Snapshot(id: "{A}"),
            TestData.Snapshot(id: "{B}"));

        harness.ViewModel.SelectedAdapter = harness.ViewModel.Adapters[1];

        Assert.Same(harness.ViewModel.SelectedAdapter, harness.Actions.CurrentAdapter);
    }

    [Fact]
    public void BusyActions_SetApplyingThenReadyApplicationState()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();

        harness.Actions.IsBusy = true;
        Assert.Equal(Strings.StateApplying, harness.ViewModel.StatusBar.ApplicationState);

        harness.Actions.IsBusy = false;
        Assert.Equal(Strings.StateReady, harness.ViewModel.StatusBar.ApplicationState);
    }

    [Fact]
    public void BusyActions_WhenRefreshHasFailed_ReturnToErrorApplicationState()
    {
        using MainWindowHarness harness = MainWindowHarness.Create();
        harness.ViewModel.Initialize();
        harness.Coordinator.PublishFailure();

        harness.Actions.IsBusy = true;
        harness.Actions.IsBusy = false;

        Assert.Equal(Strings.StateError, harness.ViewModel.StatusBar.ApplicationState);
    }

    [Fact]
    public void ProfileSelection_WhenAdapterIsSelected_LoadsThatAdaptersDraft()
    {
        TestProfileCatalog catalog = new()
        {
            Profiles = [TestData.Profile(ipv4Address: "10.0.0.60")]
        };
        using ProfilePanelViewModel panel = CreateProfilePanel(catalog);
        using MainWindowHarness harness = MainWindowHarness.Create(panel);
        harness.ViewModel.Initialize();
        harness.Coordinator.PublishRefresh(TestData.Snapshot(id: "{A}", ipv4Address: "10.0.0.5"));

        harness.ViewModel.ProfilePanel!.SelectedProfile = harness.ViewModel.ProfilePanel.Groups[0].Profiles[0];

        Assert.Equal("10.0.0.60", harness.ViewModel.SelectedAdapter!.Draft.Ipv4Address);
    }

    [Fact]
    public void ProfileSelection_WhenDirtyDraftLoadIsDeclined_PreservesDraftAndPreviousSelection()
    {
        TestProfileCatalog catalog = new()
        {
            Profiles =
            [
                TestData.Profile(profileId: "first", name: "First", ipv4Address: "10.0.0.10"),
                TestData.Profile(profileId: "second", name: "Second", ipv4Address: "10.0.0.20")
            ]
        };
        FakeUserConfirmationService confirmation = new() { Answer = true };
        using ProfilePanelViewModel panel = CreateProfilePanel(catalog, confirmation);
        using MainWindowHarness harness = MainWindowHarness.Create(panel);
        harness.ViewModel.Initialize();
        harness.Coordinator.PublishRefresh(TestData.Snapshot(id: "{A}"));
        ProfileListItemViewModel first = panel.Groups[0].Profiles[0];

        panel.SelectedProfile = first;
        harness.ViewModel.SelectedAdapter!.Draft.Ipv4Address = "10.0.0.99";
        confirmation.Answer = false;
        panel.SelectedProfile = panel.Groups[0].Profiles[1];

        Assert.Same(first, panel.SelectedProfile);
        Assert.Equal("10.0.0.99", harness.ViewModel.SelectedAdapter.Draft.Ipv4Address);
    }

    [Fact]
    public void ProfilePanel_WhenNoAdapterIsSelected_DisablesSaveUntilAnAdapterIsAvailable()
    {
        TestProfileCatalog catalog = new();
        using ProfilePanelViewModel panel = CreateProfilePanel(catalog);
        using MainWindowHarness harness = MainWindowHarness.Create(panel);

        panel.NewProfileName = "PLC";

        Assert.False(panel.SaveCommand.CanExecute(null));

        harness.ViewModel.Initialize();
        harness.Coordinator.PublishRefresh(TestData.Snapshot(id: "{A}"));

        Assert.True(panel.SaveCommand.CanExecute(null));

        harness.Coordinator.PublishRefresh();

        Assert.False(panel.SaveCommand.CanExecute(null));
    }

    private static ProfilePanelViewModel CreateProfilePanel(
        TestProfileCatalog catalog,
        FakeUserConfirmationService? confirmation = null) => new(
            catalog,
            new TestProfileFileDialogService(),
            new TestUserTextInputService(),
            confirmation ?? new FakeUserConfirmationService(),
            new FakeUiDispatcher());

    private sealed class MainWindowHarness : IDisposable
    {
        private MainWindowHarness(
            FakeAdapterRefreshCoordinator coordinator,
            FakeUiDispatcher dispatcher,
            FakeClipboardService clipboard,
            FakeStaticIpv4ApplyService staticApply,
            FakeDhcpApplyService dhcpApply,
            FakeRecoveryRestoreService recoveryRestore,
            FakeUserConfirmationService confirmation,
            AdapterActionsViewModel actions,
            MainWindowViewModel viewModel)
        {
            Coordinator = coordinator;
            Dispatcher = dispatcher;
            Clipboard = clipboard;
            StaticApply = staticApply;
            DhcpApply = dhcpApply;
            RecoveryRestore = recoveryRestore;
            Confirmation = confirmation;
            Actions = actions;
            ViewModel = viewModel;
        }

        public FakeAdapterRefreshCoordinator Coordinator { get; }

        public FakeUiDispatcher Dispatcher { get; }

        public FakeClipboardService Clipboard { get; }

        public FakeStaticIpv4ApplyService StaticApply { get; }

        public FakeDhcpApplyService DhcpApply { get; }

        public FakeRecoveryRestoreService RecoveryRestore { get; }

        public FakeUserConfirmationService Confirmation { get; }

        public AdapterActionsViewModel Actions { get; }

        public MainWindowViewModel ViewModel { get; }

        public static MainWindowHarness Create(
            ProfilePanelViewModel? profilePanel = null,
            bool isElevated = true,
            string version = "1.0.0")
        {
            FakeAdapterRefreshCoordinator coordinator = new();
            FakeUiDispatcher dispatcher = new();
            FakeClipboardService clipboard = new();
            FakeStaticIpv4ApplyService staticApply = new();
            FakeDhcpApplyService dhcpApply = new();
            FakeRecoveryRestoreService recoveryRestore = new();
            FakeUserConfirmationService confirmation = new();
            AdapterActionsViewModel actions = new(
                staticApply,
                dhcpApply,
                recoveryRestore,
                confirmation,
                coordinator);
            StaticIpv4ConfigurationValidator validator = new();

            MainWindowViewModel viewModel = new(
                coordinator,
                dispatcher,
                clipboard,
                new FakeClock(RefreshedAt),
                new FakeElevationStateProvider(isElevated),
                new FakeApplicationVersionProvider(version),
                actions,
                validator,
                profilePanel);

            return new MainWindowHarness(
                coordinator,
                dispatcher,
                clipboard,
                staticApply,
                dhcpApply,
                recoveryRestore,
                confirmation,
                actions,
                viewModel);
        }

        public void Dispose() => ViewModel.Dispose();
    }

    private sealed class TestProfileCatalog : IProfileCatalog
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

    private sealed class TestProfileFileDialogService : IProfileFileDialogService
    {
        public Stream? OpenProfile() => null;

        public Stream? CreateProfile(string suggestedFileName) => null;
    }

    private sealed class TestUserTextInputService : IUserTextInputService
    {
        public string? Request(string title, string label, string initialValue) => null;
    }
}
