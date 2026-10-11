using IPMan.App.Presentation;

namespace IPMan.Tests.Fakes;

/// <summary>
/// Advances the clock and dispatches one tick, also simulating a delayed UI dispatcher.
/// Subscriptions may be disposed from any thread, including from inside a tick.
/// </summary>
public sealed class FakeTimerService(FakeClock clock) : ITimerService
{
    private readonly object _gate = new();
    private readonly List<Subscription> _subscriptions = [];

    public int ActiveSubscriptions
    {
        get
        {
            lock (_gate)
            {
                return _subscriptions.Count;
            }
        }
    }

    public IDisposable Start(TimeSpan interval, Action tick)
    {
        ArgumentNullException.ThrowIfNull(tick);
        Subscription subscription = new(this, tick);
        lock (_gate)
        {
            _subscriptions.Add(subscription);
        }
        return subscription;
    }

    public void Advance(TimeSpan duration)
    {
        Subscription[] snapshot;
        lock (_gate)
        {
            clock.Advance(duration);
            snapshot = _subscriptions.ToArray();
        }
        foreach (Subscription subscription in snapshot)
        {
            // A previous tick may have disposed a later subscription.
            if (IsActive(subscription))
            {
                subscription.Tick();
            }
        }
    }

    private bool IsActive(Subscription subscription)
    {
        lock (_gate)
        {
            return _subscriptions.Contains(subscription);
        }
    }

    private void Remove(Subscription subscription)
    {
        lock (_gate)
        {
            _subscriptions.Remove(subscription);
        }
    }

    private sealed class Subscription(FakeTimerService owner, Action tick) : IDisposable
    {
        public void Tick() => tick();
        public void Dispose() => owner.Remove(this);
    }
}
