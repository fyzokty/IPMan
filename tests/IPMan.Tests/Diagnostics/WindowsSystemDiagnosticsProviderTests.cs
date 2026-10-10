using System.IO;
using System.Security;
using IPMan.Application.Diagnostics;
using IPMan.Infrastructure.Diagnostics;
using Xunit;

namespace IPMan.Tests.Diagnostics;

public sealed class WindowsSystemDiagnosticsProviderTests
{
    [Theory]
    [InlineData("Windows 10 Pro", 19045, "Windows 10 Pro")]
    [InlineData("Windows 10 Pro", 21999, "Windows 10 Pro")]
    [InlineData("Windows 10", 22000, "Windows 11")]
    [InlineData("Windows 10 Pro", 22000, "Windows 11 Pro")]
    [InlineData("Windows 10 Enterprise", 22631, "Windows 11 Enterprise")]
    [InlineData("Windows 10 Enterprise LTSC", 26100, "Windows 11 Enterprise LTSC")]
    [InlineData("Windows 11 Pro", 22631, "Windows 11 Pro")]
    [InlineData("Windows Server 2022 Standard", 20348, "Windows Server 2022 Standard")]
    [InlineData("Windows Server 2025 Standard", 26100, "Windows Server 2025 Standard")]
    [InlineData("Custom Windows 10 Pro", 22631, "Custom Windows 10 Pro")]
    public void ResolveProductName_FromBuild22000_ReplacesOnlyTheWindows10Prefix(string registryName, int build, string expected) =>
        Assert.Equal(expected, WindowsSystemDiagnosticsProvider.ResolveProductName(registryName, build));

    [Theory]
    [InlineData("22H2", "2009", "22H2")]
    [InlineData("22H2", null, "22H2")]
    [InlineData(null, "1507", "1507")]
    [InlineData("", "1607", "1607")]
    [InlineData(" \t", "1809", "1809")]
    [InlineData(null, null, "")]
    [InlineData("", "", "")]
    [InlineData(" \t", " \t", "")]
    [InlineData(null, "", "")]
    [InlineData("", null, "")]
    public void ResolveDisplayVersion_PrefersDisplayVersionThenReleaseIdOtherwiseEmpty(
        string? displayVersion, string? releaseId, string expected) =>
        Assert.Equal(expected, WindowsSystemDiagnosticsProvider.ResolveDisplayVersion(displayVersion, releaseId));

    [Fact]
    public void ProbeProfileDirectory_WhenReadable_ReturnsExists() =>
        Assert.Equal(ProfileDirectoryStatus.Exists, WindowsSystemDiagnosticsProvider.ProbeProfileDirectory(() => { }));

    public static TheoryData<Exception, ProfileDirectoryStatus> DirectoryFailures => new()
    {
        { new FileNotFoundException(), ProfileDirectoryStatus.NotCreated },
        { new DirectoryNotFoundException(), ProfileDirectoryStatus.NotCreated },
        { new UnauthorizedAccessException(), ProfileDirectoryStatus.Inaccessible },
        { new SecurityException(), ProfileDirectoryStatus.Inaccessible },
        { new IOException(), ProfileDirectoryStatus.Inaccessible }
    };

    [Theory]
    [MemberData(nameof(DirectoryFailures))]
    public void ProbeProfileDirectory_MapsMissingAndAccessFailures(Exception failure, ProfileDirectoryStatus expected) =>
        Assert.Equal(expected, WindowsSystemDiagnosticsProvider.ProbeProfileDirectory(() => throw failure));
}
