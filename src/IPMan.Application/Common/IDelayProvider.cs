namespace IPMan.Application.Common;

/// <summary>
/// Abstraction over time-based waiting.
/// Exists so coalescing/debounce logic can be exercised deterministically in
/// tests without depending on wall-clock delays.
/// </summary>
public interface IDelayProvider
{
    Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken);
}
