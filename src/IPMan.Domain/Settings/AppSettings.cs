namespace IPMan.Domain.Settings;

/// <summary>Persistent application preferences supported by the current release.</summary>
public sealed record AppSettings(
    int SchemaVersion,
    AppTheme Theme,
    bool ApplyProfileOnSelection,
    bool NotificationsEnabled)
{
    /// <summary>The settings schema version understood by this release.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Gets the safe preferences used when no valid settings document exists.</summary>
    public static AppSettings Default { get; } =
        new(CurrentSchemaVersion, AppTheme.System, ApplyProfileOnSelection: false, NotificationsEnabled: true);
}
