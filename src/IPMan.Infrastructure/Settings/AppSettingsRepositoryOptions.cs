namespace IPMan.Infrastructure.Settings;

/// <summary>Configures application settings storage.</summary>
public sealed class AppSettingsRepositoryOptions
{
    /// <summary>Gets the application settings JSON document path.</summary>
    public required string SettingsFilePath { get; init; }
}
