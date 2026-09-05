namespace IPMan.App.ViewModels;

/// <summary>Identifies the visual importance of an inline apply result.</summary>
public enum ApplyStatusSeverity
{
    /// <summary>No result is currently displayed.</summary>
    None = 0,

    /// <summary>The requested operation completed successfully.</summary>
    Success = 1,

    /// <summary>The operation completed with an informational outcome.</summary>
    Information = 2,

    /// <summary>The operation needs attention but did not report a technical failure.</summary>
    Warning = 3,

    /// <summary>The requested operation failed.</summary>
    Error = 4
}
