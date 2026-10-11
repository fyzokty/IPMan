using IPMan.App.Presentation;

namespace IPMan.Tests.Fakes;

/// <summary>Advances the clock and dispatches one tick, also simulating a delayed UI dispatcher.</summary>
public sealed class FakeTimerService(FakeClock clock) : ITimerService
{
    private readonly List<Subscription> _subscriptions = [];

    public int ActiveSubscriptions => _subscriptions.Count;

    public IDisposable Start(TimeSpan interval, Action tick)
    {
        ArgumentNullException.ThrowIfNull(tick);
        Subscription subscription = new(_subscriptions, tick);
        _subscriptions.Add(subscription);
        return subscription;
    }

    public void Advance(TimeSpan duration)
    {
        clock.Advance(duration);
        foreach (Subscription subscription in _subscriptions.ToArray())
        {
            subscription.Tick();
        }
    }

    private sealed class Subscription(List<Subscription> subscriptions, Action tick) : IDisposable
    {
        public void Tick() => tick();
        public void Dispose() => subscriptions.Remove(this);
    }
}
