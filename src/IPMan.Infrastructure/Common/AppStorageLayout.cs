namespace IPMan.Infrastructure.Common;

/// <summary>Provides the authoritative per-user storage paths used by IPMan.</summary>
public static class AppStorageLayout
{
    private static readonly string LocalDataRoot = ResolveLocalDataRoot();

    /// <summary>Gets the root containing user-authored profiles and settings.</summary>
    public static string UserDataRoot { get; } = ResolveUserDataRoot();

    /// <summary>Gets the directory containing individual profile documents.</summary>
    public static string ProfilesDirectory { get; } = Path.Combine(UserDataRoot, "Profiles");

    /// <summary>Gets the application settings document path.</summary>
    public static string SettingsFilePath { get; } = Path.Combine(UserDataRoot, "settings.json");

    /// <summary>Gets the operational recovery snapshot directory.</summary>
    public static string BackupDirectory { get; } = Path.Combine(LocalDataRoot, "Backup");

    /// <summary>Gets the directory containing critical diagnostic logs.</summary>
    public static string LogsDirectory { get; } = Path.Combine(LocalDataRoot, "logs");

    /// <summary>Gets the rolling critical diagnostic log path.</summary>
    public static string CriticalLogFile { get; } = Path.Combine(LogsDirectory, "ipman-critical.log");

    /// <summary>Gets the marker used to detect an unclean application exit.</summary>
    public static string SessionMarkerFile { get; } = Path.Combine(LocalDataRoot, "session.marker");

    private static string ResolveUserDataRoot()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return string.IsNullOrWhiteSpace(documents)
            ? LocalDataRoot
            : Path.Combine(documents, "IPMan");
    }

    private static string ResolveLocalDataRoot()
    {
        string localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            throw new InvalidOperationException("The local application data folder is unavailable.");
        }

        return Path.Combine(localAppData, "IPMan");
    }
}
