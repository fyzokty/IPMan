using IPMan.Application.Common;

namespace IPMan.Tests.Fakes;

/// <summary>
/// Wall and monotonic time advance together; assigning <see cref="UtcNow"/> simulates a
/// wall-clock jump that leaves monotonic time untouched.
/// </summary>
public sealed class FakeClock : IClock, IMonotonicClock
{
    public FakeClock(DateTimeOffset utcNow) => UtcNow = utcNow;

    public DateTimeOffset UtcNow { get; set; }

    public TimeSpan Elapsed { get; private set; }

    public void Advance(TimeSpan duration)
    {
        UtcNow += duration;
        Elapsed += duration;
    }
}
