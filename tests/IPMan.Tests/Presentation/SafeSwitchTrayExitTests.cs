using IPMan.App.Presentation;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Presentation;

public sealed class SafeSwitchTrayExitTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequestExitAsync_GuardDecision_PublishesOnlyApprovedExit(bool approved)
    {
        TrayIconManager tray = FakeWindowServices.CreateTray();
        tray.ExitGuard = () => Task.FromResult(approved);
        int exits = 0;
        tray.ExitRequested += (_, _) => exits++;
        await tray.RequestExitAsync();
        Assert.Equal(approved ? 1 : 0, exits);
    }

    [Fact]
    public async Task RequestExitAsync_RestoreStillPending_DoesNotPublishExitUntilGuardCompletes()
    {
        TrayIconManager tray = FakeWindowServices.CreateTray();
        TaskCompletionSource<bool> guard = new(TaskCreationOptions.RunContinuationsAsynchronously);
        tray.ExitGuard = () => guard.Task;
        int exits = 0;
        tray.ExitRequested += (_, _) => exits++;
        Task exit = tray.RequestExitAsync();
        Assert.Equal(0, exits);
        Assert.False(exit.IsCompleted);
        guard.SetResult(true);
        await exit;
        Assert.Equal(1, exits);
    }
}
