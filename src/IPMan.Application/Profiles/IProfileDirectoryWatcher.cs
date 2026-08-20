namespace IPMan.Application.Profiles;

/// <summary>Observes changes to the profile document directory.</summary>
public interface IProfileDirectoryWatcher : IDisposable
{
    /// <summary>Raised when one or more profile directory changes are observed.</summary>
    event EventHandler? Changed;

    /// <summary>Starts observing profile directory changes.</summary>
    void StartWatching();

    /// <summary>Stops observing profile directory changes.</summary>
    void StopWatching();
}
