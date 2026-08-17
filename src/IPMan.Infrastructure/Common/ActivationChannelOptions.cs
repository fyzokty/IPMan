namespace IPMan.Infrastructure.Common;

/// <summary>Bounded retry settings for activation delivery.</summary>
internal sealed class ActivationChannelOptions
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(3);

    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromMilliseconds(50);

    public TimeSpan MaximumRetryDelay { get; init; } = TimeSpan.FromMilliseconds(400);
}
