using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using IPMan.Application.Diagnostics;
using IPMan.Infrastructure.Common;
using Microsoft.Win32;

namespace IPMan.Infrastructure.Diagnostics;

/// <summary>Reads Windows version metadata and the authoritative application storage paths.</summary>
public sealed class WindowsSystemDiagnosticsProvider : ISystemDiagnosticsProvider
{
    public string ProfilesDirectory => AppStorageLayout.ProfilesDirectory;

    public string CriticalLogFile => AppStorageLayout.CriticalLogFile;

    public WindowsVersionInfo ReadWindowsVersion()
    {
        using RegistryKey machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using RegistryKey key = machine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion")
            ?? throw new InvalidOperationException("Windows version metadata is unavailable.");
        string productName = ReadRequiredString(key, "ProductName");
        string displayVersion = ResolveDisplayVersion(
            key.GetValue("DisplayVersion") as string, key.GetValue("ReleaseId") as string);
        string buildNumber = ReadRequiredString(key, "CurrentBuildNumber");
        int build = int.Parse(buildNumber, CultureInfo.InvariantCulture);
        if (key.GetValue("UBR") is int revision)
        {
            buildNumber += "." + revision.ToString(CultureInfo.InvariantCulture);
        }

        return new WindowsVersionInfo(
            ResolveProductName(productName, build),
            displayVersion,
            buildNumber,
            RuntimeInformation.OSArchitecture.ToString());
    }

    public ProfileDirectoryStatus ReadProfileDirectoryStatus() => ProbeProfileDirectory(() =>
    {
        if ((File.GetAttributes(ProfilesDirectory) & FileAttributes.Directory) == 0)
        {
            throw new IOException("The profile path is not a directory.");
        }

        // Exists APIs hide access failures. Enumerating also verifies permission to read the directory.
        using IEnumerator<string> entries = Directory.EnumerateFileSystemEntries(ProfilesDirectory).GetEnumerator();
        _ = entries.MoveNext();
    });

    public bool CriticalLogExists()
    {
        try
        {
            return (File.GetAttributes(CriticalLogFile) & FileAttributes.Directory) == 0;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
    }

    internal static string ResolveProductName(string productName, int buildNumber)
    {
        const string windows10Prefix = "Windows 10";
        return buildNumber >= 22000 && productName.StartsWith(windows10Prefix, StringComparison.Ordinal)
            ? "Windows 11" + productName[windows10Prefix.Length..]
            : productName;
    }

    internal static string ResolveDisplayVersion(string? displayVersion, string? releaseId) =>
        !string.IsNullOrWhiteSpace(displayVersion) ? displayVersion
        : !string.IsNullOrWhiteSpace(releaseId) ? releaseId : string.Empty;

    internal static ProfileDirectoryStatus ProbeProfileDirectory(Action probe)
    {
        ArgumentNullException.ThrowIfNull(probe);
        try
        {
            probe();
            return ProfileDirectoryStatus.Exists;
        }
        catch (FileNotFoundException)
        {
            return ProfileDirectoryStatus.NotCreated;
        }
        catch (DirectoryNotFoundException)
        {
            return ProfileDirectoryStatus.NotCreated;
        }
        catch (UnauthorizedAccessException)
        {
            return ProfileDirectoryStatus.Inaccessible;
        }
        catch (SecurityException)
        {
            return ProfileDirectoryStatus.Inaccessible;
        }
        catch (IOException)
        {
            return ProfileDirectoryStatus.Inaccessible;
        }
    }

    private static string ReadRequiredString(RegistryKey key, string name) =>
        key.GetValue(name) is string value && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException("Windows version metadata is incomplete.");
}
