namespace IPMan.Infrastructure.Profiles;

/// <summary>Configures profile directory change coalescing.</summary>
public sealed class ProfileWatcherOptions
{
    /// <summary>Gets the window used to combine repeated file system events.</summary>
    public TimeSpan DebounceWindow { get; init; } = TimeSpan.FromMilliseconds(250);
}
