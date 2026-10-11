using System.Windows.Threading;

namespace IPMan.App.Presentation;

/// <summary>Runs countdown callbacks on the WPF dispatcher.</summary>
public sealed class DispatcherTimerService(Dispatcher dispatcher) : ITimerService
{
    public IDisposable Start(TimeSpan interval, Action tick)
    {
        ArgumentNullException.ThrowIfNull(tick);
        DispatcherTimer timer = new(DispatcherPriority.Normal, dispatcher) { Interval = interval };
        EventHandler handler = (_, _) => tick();
        timer.Tick += handler;
        timer.Start();
        return new Subscription(timer, handler);
    }

    private sealed class Subscription(DispatcherTimer timer, EventHandler handler) : IDisposable
    {
        public void Dispose()
        {
            timer.Stop();
            timer.Tick -= handler;
        }
    }
}
