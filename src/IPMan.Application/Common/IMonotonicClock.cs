namespace IPMan.Application.Common;

/// <summary>
/// Elapsed time from an arbitrary fixed origin that never jumps when the wall clock
/// is changed; safety deadlines must use this instead of <see cref="IClock"/>.
/// </summary>
public interface IMonotonicClock
{
    TimeSpan Elapsed { get; }
}
