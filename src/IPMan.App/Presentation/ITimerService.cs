namespace IPMan.App.Presentation;

/// <summary>Schedules UI countdown ticks; disposing the subscription stops callbacks.</summary>
public interface ITimerService
{
    IDisposable Start(TimeSpan interval, Action tick);
}
