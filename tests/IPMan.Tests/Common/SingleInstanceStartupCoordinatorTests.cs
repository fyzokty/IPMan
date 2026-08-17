using IPMan.App;
using IPMan.Infrastructure.Common;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Common;

public sealed class SingleInstanceStartupCoordinatorTests
{
    [Fact]
    public async Task CoordinateAsync_WhenGuardIsAcquired_StartsPrimaryInstance()
    {
        FakeSingleInstanceGuard guard = new();
        FakeActivationChannelClient client = new();
        int foregroundPermissionCount = 0;
        SingleInstanceStartupCoordinator coordinator = new(
            guard,
            client,
            () => foregroundPermissionCount++);

        SingleInstanceStartupOutcome outcome = await coordinator.CoordinateAsync(
            CancellationToken.None);

        Assert.Equal(SingleInstanceStartupOutcome.PrimaryInstance, outcome);
        Assert.Equal(1, guard.AcquireCount);
        Assert.Equal(0, client.SendCount);
        Assert.Equal(0, foregroundPermissionCount);
    }

    [Fact]
    public async Task CoordinateAsync_WhenGuardIsUnavailable_AllowsForegroundBeforeSendingSignal()
    {
        List<string> events = new();
        FakeSingleInstanceGuard guard = new()
        {
            Acquisition = SingleInstanceAcquisition.Unavailable
        };
        FakeActivationChannelClient client = new()
        {
            OnSend = () => events.Add("signal")
        };
        SingleInstanceStartupCoordinator coordinator = new(
            guard,
            client,
            () => events.Add("allow-foreground"));

        SingleInstanceStartupOutcome outcome = await coordinator.CoordinateAsync(
            CancellationToken.None);

        Assert.Equal(SingleInstanceStartupOutcome.ExistingInstanceActivated, outcome);
        Assert.Collection(
            events,
            item => Assert.Equal("allow-foreground", item),
            item => Assert.Equal("signal", item));
    }

    [Fact]
    public async Task CoordinateAsync_WhenActivationCannotBeSent_ReportsFailure()
    {
        FakeSingleInstanceGuard guard = new()
        {
            Acquisition = SingleInstanceAcquisition.Unavailable
        };
        FakeActivationChannelClient client = new() { SendResult = false };
        SingleInstanceStartupCoordinator coordinator = new(guard, client, () => { });

        SingleInstanceStartupOutcome outcome = await coordinator.CoordinateAsync(
            CancellationToken.None);

        Assert.Equal(SingleInstanceStartupOutcome.ActivationFailed, outcome);
        Assert.Equal(1, client.SendCount);
    }

    [Fact]
    public async Task CoordinateAsync_WhenMutexWasAbandoned_StartsPrimaryInstance()
    {
        FakeSingleInstanceGuard guard = new()
        {
            Acquisition = SingleInstanceAcquisition.AcquiredAfterAbandonment
        };
        FakeActivationChannelClient client = new();
        SingleInstanceStartupCoordinator coordinator = new(guard, client, () => { });

        SingleInstanceStartupOutcome outcome = await coordinator.CoordinateAsync(
            CancellationToken.None);

        Assert.Equal(SingleInstanceStartupOutcome.PrimaryInstance, outcome);
        Assert.Equal(0, client.SendCount);
    }
}
