using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Domain.Profiles;

namespace IPMan.Tests.Fakes;

internal sealed class HomeFixture : IDisposable
{
    public HomeFixture(IReadOnlyList<NetworkProfile> profiles)
    {
        Catalog.Profiles = profiles;
        FakeWindowProfileDialogs dialogs = new();
        Profiles = new(Catalog, dialogs, dialogs, Confirmation, new FakeUiDispatcher());
        AdapterActionsViewModel actions = new(Static, Dhcp, Restore, Confirmation, Coordinator, Safety);
        Main = new(Coordinator, new FakeUiDispatcher(), new FakeClipboardService(),
            new FakeClock(DateTimeOffset.UtcNow), new FakeElevationStateProvider(true), new FakeApplicationVersionProvider("1.0"),
            actions, new StaticIpv4ConfigurationValidator(), Profiles,
            editorFactory: (profile, adapter) => new(Catalog, new StaticIpv4ConfigurationValidator(),
                new(new SequenceIpv4ConflictProbe()), new ImmediateDelayProvider(), profile, adapter?.Gateway,
                adapter?.Ipv4Address, adapter?.SubnetMask),
            confirmationService: Confirmation);
        Main.Initialize();
        Coordinator.PublishRefresh(TestData.Snapshot());
    }
    public FakeProfileEditorCatalog Catalog { get; } = new();
    public FakeAdapterRefreshCoordinator Coordinator { get; } = new();
    public FakeStaticIpv4ApplyService Static { get; } = new();
    public FakeDhcpApplyService Dhcp { get; } = new();
    public FakeRecoveryRestoreService Restore { get; } = new();
    public FakeSafetyCountdownCoordinator Safety { get; } = new();
    public FakeUserConfirmationService Confirmation { get; } = new();
    public ProfilePanelViewModel Profiles { get; }
    public MainWindowViewModel Main { get; }
    public void Dispose() => Main.Dispose();
}
