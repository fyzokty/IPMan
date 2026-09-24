namespace IPMan.Application.Logging;

/// <summary>Records failures that require diagnostic information.</summary>
public interface ICriticalLogger
{
    /// <summary>Raised when a record cannot be written.</summary>
    event EventHandler? WriteFailed;

    /// <summary>Writes a critical diagnostic record.</summary>
    void Log(CriticalLogEntry entry);
}

/// <summary>Classifies a critical diagnostic record.</summary>
public enum CriticalLogCategory
{
    Json,
    SettingsWrite,
    ProfileWrite,
    NetworkApi,
    Unhandled,
    UnexpectedShutdown
}

/// <summary>Contains data written to the critical diagnostic log.</summary>
public sealed record CriticalLogEntry(
    CriticalLogCategory Category,
    string Message,
    int? ErrorCode = null,
    Exception? Exception = null);

/// <summary>Provides a no-op logger for callers that do not require diagnostics.</summary>
public sealed class NullCriticalLogger : ICriticalLogger
{
    /// <inheritdoc />
    public event EventHandler? WriteFailed
    {
        add { }
        remove { }
    }

    /// <inheritdoc />
    public void Log(CriticalLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
    }
}
