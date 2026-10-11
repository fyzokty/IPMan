using IPMan.Application.Common;

namespace IPMan.Tests.Fakes;

public sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset utcNow) => UtcNow = utcNow;

    public DateTimeOffset UtcNow { get; set; }

    public void Advance(TimeSpan duration) => UtcNow += duration;
}
