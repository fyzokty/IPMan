using IPMan.App.Presentation;
using IPMan.Application.Common;
using IPMan.Application.Diagnostics;

namespace IPMan.Tests.Fakes;

public sealed class FakeDiagnosticsSources : ISystemDiagnosticsProvider, IApplicationVersionProvider, IElevationStateProvider, IClock
{
    public HashSet<string> FailingSources { get; } = new(StringComparer.Ordinal);

    public WindowsVersionInfo WindowsVersion { get; set; } = new("Windows 11", "23H2", "22631.1234", "X64");

    public string VersionValue { get; set; } = "1.2.3-preview+abcdef";

    public bool Elevated { get; set; } = true;

    public int AdapterCount { get; set; } = 3;

    public ProfileDirectoryStatus DirectoryStatus { get; set; } = ProfileDirectoryStatus.Exists;

    public bool LogExists { get; set; } = true;

    public DateTimeOffset Now { get; set; } = new(2026, 10, 10, 12, 34, 56, TimeSpan.Zero);

    public string Version => "1.2.3-preview";

    public string InformationalVersion => Read(nameof(InformationalVersion), () => VersionValue);

    public bool IsElevated => Read(nameof(IsElevated), () => Elevated);

    public string ProfilesDirectory => Read(nameof(ProfilesDirectory), () => @"C:\diagnostics-test\Profiles");

    public string CriticalLogFile => Read(nameof(CriticalLogFile), () => @"C:\diagnostics-test\logs\ipman-critical.log");

    public DateTimeOffset UtcNow => Read(nameof(UtcNow), () => Now);

    public WindowsVersionInfo ReadWindowsVersion() => Read(nameof(ReadWindowsVersion), () => WindowsVersion);

    public ProfileDirectoryStatus ReadProfileDirectoryStatus() => Read(nameof(ReadProfileDirectoryStatus), () => DirectoryStatus);

    public bool CriticalLogExists() => Read(nameof(CriticalLogExists), () => LogExists);

    public int ReadAdapterCount() => Read(nameof(ReadAdapterCount), () => AdapterCount);

    private T Read<T>(string source, Func<T> read) => FailingSources.Contains(source)
        ? throw new InvalidOperationException("Diagnostic source failed.")
        : read();
}
