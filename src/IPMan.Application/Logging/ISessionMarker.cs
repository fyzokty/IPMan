namespace IPMan.Application.Logging;

/// <summary>Tracks whether the previous interactive session closed cleanly.</summary>
public interface ISessionMarker
{
    /// <summary>Consumes a marker from a previous session when it exists.</summary>
    bool TryConsumeStale();

    /// <summary>Creates the current session marker.</summary>
    void Create();

    /// <summary>Deletes the current session marker.</summary>
    void Delete();
}
