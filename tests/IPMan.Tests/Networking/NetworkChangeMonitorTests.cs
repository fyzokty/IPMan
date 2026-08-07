using IPMan.Application.Networking;
using IPMan.Infrastructure.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class NetworkChangeMonitorTests
{
    [Fact]
    public void StartMonitoring_SubscribesOnce()
    {
        FakeNetworkChangeEventSource source = new();
        using NetworkChangeMonitor monitor = new(source);

        monitor.StartMonitoring();

        Assert.Equal(1, source.SubscribeCount);
        Assert.Equal(1, source.ActiveSubscriptions);
    }

    [Fact]
    public void StartMonitoring_WhenCalledRepeatedly_DoesNotSubscribeTwice()
    {
        FakeNetworkChangeEventSource source = new();
        using NetworkChangeMonitor monitor = new(source);

        monitor.StartMonitoring();
        monitor.StartMonitoring();
        monitor.StartMonitoring();

        Assert.Equal(1, source.SubscribeCount);
        Assert.Equal(1, source.ActiveSubscriptions);
    }

    [Fact]
    public void StopMonitoring_Unsubscribes()
    {
        FakeNetworkChangeEventSource source = new();
        using NetworkChangeMonitor monitor = new(source);

        monitor.StartMonitoring();
        monitor.StopMonitoring();

        Assert.Equal(1, source.UnsubscribeCount);
        Assert.Equal(0, source.ActiveSubscriptions);
    }

    [Fact]
    public void StopMonitoring_WhenNeverStarted_IsSafe()
    {
        FakeNetworkChangeEventSource source = new();
        using NetworkChangeMonitor monitor = new(source);

        monitor.StopMonitoring();
        monitor.StopMonitoring();

        Assert.Equal(0, source.UnsubscribeCount);
    }

    [Fact]
    public void StopMonitoring_WhenCalledRepeatedly_UnsubscribesOnce()
    {
        FakeNetworkChangeEventSource source = new();
        using NetworkChangeMonitor monitor = new(source);

        monitor.StartMonitoring();
        monitor.StopMonitoring();
        monitor.StopMonitoring();

        Assert.Equal(1, source.UnsubscribeCount);
    }

    [Fact]
    public void StartMonitoring_AfterStop_SubscribesAgain()
    {
        FakeNetworkChangeEventSource source = new();
        using NetworkChangeMonitor monitor = new(source);

        monitor.StartMonitoring();
        monitor.StopMonitoring();
        monitor.StartMonitoring();

        Assert.Equal(2, source.SubscribeCount);
        Assert.Equal(1, source.ActiveSubscriptions);
    }

    [Fact]
    public void Dispose_LeavesNoActiveSubscription()
    {
        FakeNetworkChangeEventSource source = new();
        NetworkChangeMonitor monitor = new(source);

        monitor.StartMonitoring();
        monitor.Dispose();

        Assert.Equal(0, source.ActiveSubscriptions);
    }

    [Fact]
    public void Dispose_WhenCalledRepeatedly_IsSafe()
    {
        FakeNetworkChangeEventSource source = new();
        NetworkChangeMonitor monitor = new(source);

        monitor.StartMonitoring();
        monitor.Dispose();
        monitor.Dispose();

        Assert.Equal(1, source.UnsubscribeCount);
    }

    [Fact]
    public void StartMonitoring_AfterDispose_Throws()
    {
        FakeNetworkChangeEventSource source = new();
        NetworkChangeMonitor monitor = new(source);
        monitor.Dispose();

        Assert.Throws<ObjectDisposedException>(monitor.StartMonitoring);
    }

    [Fact]
    public void OperatingSystemNetworkEvent_IsTranslatedIntoChangedEvent()
    {
        FakeNetworkChangeEventSource source = new();
        using NetworkChangeMonitor monitor = new(source);

        List<string> reasons = new();
        monitor.Changed += (_, e) => reasons.Add(e.Reason);

        monitor.StartMonitoring();
        source.RaiseNetworkChanged(NetworkChangeReason.NetworkAddressChanged);
        source.RaiseNetworkChanged(NetworkChangeReason.NetworkAvailabilityChanged);

        Assert.Equal(
            new[]
            {
                NetworkChangeReason.NetworkAddressChanged,
                NetworkChangeReason.NetworkAvailabilityChanged
            },
            reasons);
    }

    [Fact]
    public void AfterStopMonitoring_OperatingSystemEventsAreNoLongerObserved()
    {
        FakeNetworkChangeEventSource source = new();
        using NetworkChangeMonitor monitor = new(source);

        int raised = 0;
        monitor.Changed += (_, _) => raised++;

        monitor.StartMonitoring();
        monitor.StopMonitoring();
        source.RaiseNetworkChanged(NetworkChangeReason.NetworkAddressChanged);

        Assert.Equal(0, raised);
    }
}
