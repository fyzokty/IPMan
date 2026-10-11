using CommunityToolkit.Mvvm.Input;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Tests.Fakes;

internal sealed class DetailsPageFixture : IDisposable
{
    public DetailsPageFixture(NetworkAdapterSnapshot? snapshot = null, IAsyncRelayCommand<ManualSettingsRequest>? boundary = null)
    {
        Adapter = new AdapterViewModel(snapshot ?? TestData.Snapshot(mode: NetworkConfigurationMode.Static),
            new FakeClipboardService(), new StaticIpv4ConfigurationValidator());
        Actions = new AdapterActionsViewModel(StaticApply, DhcpApply, new FakeRecoveryRestoreService(),
            new FakeUserConfirmationService { Answer = true }, new FakeAdapterRefreshCoordinator());
        Actions.Attach(Adapter);
        Page = new AdapterDetailsPageViewModel(Adapter, Actions, boundary);
    }
    public FakeStaticIpv4ApplyService StaticApply { get; } = new();
    public FakeDhcpApplyService DhcpApply { get; } = new();
    public AdapterViewModel Adapter { get; }
    public AdapterActionsViewModel Actions { get; }
    public AdapterDetailsPageViewModel Page { get; }
    public void Dispose()
    {
        Page.Dispose();
        Actions.Dispose();
    }
}
