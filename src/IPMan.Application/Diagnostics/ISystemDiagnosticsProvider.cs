namespace IPMan.Application.Diagnostics;

/// <summary>Reads operating-system and storage facts without changing the system.</summary>
public interface ISystemDiagnosticsProvider
{
    WindowsVersionInfo ReadWindowsVersion();

    string ProfilesDirectory { get; }

    ProfileDirectoryStatus ReadProfileDirectoryStatus();

    string CriticalLogFile { get; }

    bool CriticalLogExists();
}
