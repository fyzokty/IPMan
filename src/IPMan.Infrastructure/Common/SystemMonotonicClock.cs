using System.Diagnostics;
using IPMan.Application.Common;

namespace IPMan.Infrastructure.Common;

/// <summary>Reads the high-resolution performance counter, which ignores wall-clock changes.</summary>
public sealed class SystemMonotonicClock : IMonotonicClock
{
    private readonly long _origin = Stopwatch.GetTimestamp();

    public TimeSpan Elapsed => Stopwatch.GetElapsedTime(_origin);
}
