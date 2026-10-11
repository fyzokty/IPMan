using System.Globalization;
using System.Reflection;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Diagnostics;
using IPMan.Domain.Profiles;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.ViewModels;

public sealed class DiagnosticsViewModelTests
{
    public static TheoryData<string, string> SourceFailures => new()
    {
        { nameof(FakeDiagnosticsSources.ReadWindowsVersion), Strings.DiagnosticsWindowsVersion },
        { nameof(FakeDiagnosticsSources.InformationalVersion), Strings.DiagnosticsApplicationVersion },
        { nameof(FakeDiagnosticsSources.IsElevated), Strings.DiagnosticsAdministrator },
        { nameof(FakeDiagnosticsSources.ReadAdapterCount), Strings.DiagnosticsAdapterCount },
        { nameof(FakeDiagnosticsSources.ProfilesDirectory), Strings.DiagnosticsProfilesDirectory },
        { nameof(FakeDiagnosticsSources.ReadProfileDirectoryStatus), Strings.DiagnosticsProfilesDirectoryStatus },
        { nameof(FakeDiagnosticsSources.CriticalLogFile), Strings.DiagnosticsCriticalLog },
        { nameof(FakeDiagnosticsSources.CriticalLogExists), Strings.DiagnosticsCriticalLog },
        { nameof(FakeDiagnosticsSources.UtcNow), Strings.DiagnosticsCreatedAt }
    };

    [Fact]
    public void Refresh_FillsEveryFieldAndCopyPreservesThePlainTextReport()
    {
        FakeDiagnosticsSources sources = new();
        using FakeProfileCatalog profiles = CreateProfiles();
        FakeClipboardService clipboard = new();
        DiagnosticsViewModel viewModel = Create(sources, profiles, clipboard);

        viewModel.RefreshCommand.Execute(null);
        viewModel.CopyCommand.Execute(null);

        string[] expected =
        [
            $"{Strings.DiagnosticsWindowsVersion}: Windows 11 — 23H2, derleme 22631.1234, X64",
            $"{Strings.DiagnosticsApplicationVersion}: 1.2.3-preview+abcdef",
            $"{Strings.DiagnosticsAdministrator}: {Strings.DiagnosticsAdministratorYes}",
            $"{Strings.DiagnosticsAdapterCount}: 3",
            $"{Strings.DiagnosticsProfilesDirectory}: C:\\diagnostics-test\\Profiles",
            $"{Strings.DiagnosticsProfilesDirectoryStatus}: {Strings.DiagnosticsDirectoryExists}",
            $"{Strings.DiagnosticsProfileCount}: 2",
            $"{Strings.DiagnosticsProblemCount}: 1",
            $"{Strings.DiagnosticsCriticalLog}: C:\\diagnostics-test\\logs\\ipman-critical.log",
            $"{Strings.DiagnosticsCreatedAt}: {sources.Now.ToLocalTime().ToString("G", CultureInfo.CurrentCulture)}"
        ];
        Assert.Equal(string.Join(Environment.NewLine, expected), viewModel.ReportText);
        Assert.Equal(viewModel.ReportText, Assert.Single(clipboard.CopiedTexts));
        Assert.Equal(Strings.DiagnosticsCopySucceeded, viewModel.CopyStatus);
    }

    [Theory]
    [MemberData(nameof(SourceFailures))]
    public void Refresh_WhenOneSourceThrows_OnlyThatFieldIsUnreadable(string source, string label)
    {
        FakeDiagnosticsSources sources = new();
        using FakeProfileCatalog profiles = CreateProfiles();
        DiagnosticsViewModel viewModel = Create(sources, profiles);
        viewModel.RefreshCommand.Execute(null);
        string[] original = viewModel.ReportText.Split(Environment.NewLine);
        sources.FailingSources.Add(source);

        viewModel.RefreshCommand.Execute(null);

        string[] expected = original.Select(line => line.StartsWith(label + ":", StringComparison.Ordinal)
            ? $"{label}: {Strings.DiagnosticsUnreadable}" : line).ToArray();
        Assert.Equal(string.Join(Environment.NewLine, expected), viewModel.ReportText);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Refresh_WhenOneCatalogCountThrows_TheOtherCountRemainsAvailable(bool failProfiles)
    {
        using FakeProfileCatalog profiles = CreateProfiles();
        profiles.FailProfiles = failProfiles;
        profiles.FailProblems = !failProfiles;
        DiagnosticsViewModel viewModel = Create(new FakeDiagnosticsSources(), profiles);

        viewModel.RefreshCommand.Execute(null);

        Assert.Contains($"{Strings.DiagnosticsProfileCount}: {(failProfiles ? Strings.DiagnosticsUnreadable : "2")}", viewModel.ReportText, StringComparison.Ordinal);
        Assert.Contains($"{Strings.DiagnosticsProblemCount}: {(failProfiles ? "1" : Strings.DiagnosticsUnreadable)}", viewModel.ReportText, StringComparison.Ordinal);
        Assert.Contains($"{Strings.DiagnosticsAdapterCount}: 3", viewModel.ReportText, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ProfileDirectoryStatus.Exists)]
    [InlineData(ProfileDirectoryStatus.NotCreated)]
    [InlineData(ProfileDirectoryStatus.Inaccessible)]
    public void Refresh_MapsDirectoryStatusToLocalizedText(ProfileDirectoryStatus status)
    {
        FakeDiagnosticsSources sources = new() { DirectoryStatus = status };
        using FakeProfileCatalog profiles = new();
        DiagnosticsViewModel viewModel = Create(sources, profiles);
        string expected = status switch
        {
            ProfileDirectoryStatus.Exists => Strings.DiagnosticsDirectoryExists,
            ProfileDirectoryStatus.NotCreated => Strings.DiagnosticsDirectoryNotCreated,
            _ => Strings.DiagnosticsDirectoryInaccessible
        };

        viewModel.RefreshCommand.Execute(null);

        Assert.Contains($"{Strings.DiagnosticsProfilesDirectoryStatus}: {expected}", viewModel.ReportText, StringComparison.Ordinal);
        Assert.Contains($"{Strings.DiagnosticsProfilesDirectory}: {sources.ProfilesDirectory}", viewModel.ReportText, StringComparison.Ordinal);
    }

    [Fact]
    public void Refresh_WhenLogDoesNotExist_IncludesTheExpectedPathAndMissingSuffix()
    {
        FakeDiagnosticsSources sources = new() { LogExists = false, Elevated = false };
        using FakeProfileCatalog profiles = new();
        DiagnosticsViewModel viewModel = Create(sources, profiles);

        viewModel.RefreshCommand.Execute(null);

        Assert.Contains($"{Strings.DiagnosticsCriticalLog}: {sources.CriticalLogFile} (henüz oluşturulmadı)", viewModel.ReportText, StringComparison.Ordinal);
        Assert.Contains($"{Strings.DiagnosticsAdministrator}: {Strings.DiagnosticsAdministratorNo}", viewModel.ReportText, StringComparison.Ordinal);
        Assert.Contains($"{Strings.DiagnosticsProfileCount}: 0", viewModel.ReportText, StringComparison.Ordinal);
        Assert.Contains($"{Strings.DiagnosticsProblemCount}: 0", viewModel.ReportText, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Copy_WhenClipboardFails_ShowsInlineFailureAndKeepsTheReport(bool throws)
    {
        using FakeProfileCatalog profiles = new();
        FakeClipboardService clipboard = new();
        DiagnosticsViewModel viewModel = Create(new FakeDiagnosticsSources(), profiles, clipboard);
        viewModel.RefreshCommand.Execute(null);
        viewModel.CopyCommand.Execute(null);
        Assert.Equal(Strings.DiagnosticsCopySucceeded, viewModel.CopyStatus);
        string report = viewModel.ReportText;
        clipboard.Succeeds = false;
        clipboard.Failure = throws ? new InvalidOperationException("Clipboard unavailable.") : null;

        viewModel.CopyCommand.Execute(null);

        Assert.Equal(Strings.DiagnosticsCopyFailed, viewModel.CopyStatus);
        Assert.False(viewModel.CopySucceeded);
        Assert.Equal(report, viewModel.ReportText);
        clipboard.Succeeds = true;
        clipboard.Failure = null;
        viewModel.CopyCommand.Execute(null);
        Assert.Equal(Strings.DiagnosticsCopySucceeded, viewModel.CopyStatus);
        Assert.True(viewModel.CopySucceeded);
    }

    [Fact]
    public void Refresh_RecollectsSourcesUpdatesTimestampAndClearsCopyStatus()
    {
        FakeDiagnosticsSources sources = new();
        using FakeProfileCatalog profiles = CreateProfiles();
        FakeClipboardService clipboard = new();
        DiagnosticsViewModel viewModel = Create(sources, profiles, clipboard);
        viewModel.RefreshCommand.Execute(null);
        viewModel.CopyCommand.Execute(null);
        string firstReport = viewModel.ReportText;
        sources.FailingSources.Add(nameof(FakeDiagnosticsSources.ReadWindowsVersion));
        sources.AdapterCount = 7;
        sources.DirectoryStatus = ProfileDirectoryStatus.NotCreated;
        sources.VersionValue = "2.0.0+next";
        sources.Elevated = false;
        sources.LogExists = false;
        sources.Now = sources.Now.AddMinutes(1);
        profiles.ProfileItems = [];
        profiles.ProblemItems = [];

        viewModel.RefreshCommand.Execute(null);

        Assert.NotEqual(firstReport, viewModel.ReportText);
        Assert.Empty(viewModel.CopyStatus);
        Assert.Contains($"{Strings.DiagnosticsWindowsVersion}: {Strings.DiagnosticsUnreadable}", viewModel.ReportText, StringComparison.Ordinal);
        Assert.Contains($"{Strings.DiagnosticsAdapterCount}: 7", viewModel.ReportText, StringComparison.Ordinal);
        Assert.Contains($"{Strings.DiagnosticsApplicationVersion}: 2.0.0+next", viewModel.ReportText, StringComparison.Ordinal);
        Assert.Contains($"{Strings.DiagnosticsAdministrator}: {Strings.DiagnosticsAdministratorNo}", viewModel.ReportText, StringComparison.Ordinal);
        Assert.Contains($"{Strings.DiagnosticsProfilesDirectoryStatus}: {Strings.DiagnosticsDirectoryNotCreated}", viewModel.ReportText, StringComparison.Ordinal);
        Assert.Contains($"{Strings.DiagnosticsProfileCount}: 0", viewModel.ReportText, StringComparison.Ordinal);
        Assert.Contains($"{Strings.DiagnosticsProblemCount}: 0", viewModel.ReportText, StringComparison.Ordinal);
        Assert.Contains($"{Strings.DiagnosticsCreatedAt}: {sources.Now.ToLocalTime().ToString("G", CultureInfo.CurrentCulture)}", viewModel.ReportText, StringComparison.Ordinal);
        viewModel.CopyCommand.Execute(null);
        Assert.Equal(viewModel.ReportText, clipboard.LastCopiedText);
        sources.FailingSources.Clear();
        viewModel.RefreshCommand.Execute(null);
        Assert.DoesNotContain(Strings.DiagnosticsUnreadable, viewModel.ReportText, StringComparison.Ordinal);
    }

    [Fact]
    public void AssemblyVersionProvider_PreservesTheCompleteInformationalVersion()
    {
        AssemblyApplicationVersionProvider provider = new();
        string? expected = typeof(AssemblyApplicationVersionProvider).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        Assert.NotNull(expected);
        Assert.Equal(expected, provider.InformationalVersion);
    }

    [Fact]
    public void ReportRows_PreserveValuesAndTheOriginalPlainTextClipboardFormat()
    {
        FakeDiagnosticsSources sources = new() { VersionValue = "version: details" + Environment.NewLine + "second line" };
        using FakeProfileCatalog profiles = CreateProfiles();
        FakeClipboardService clipboard = new();
        DiagnosticsViewModel viewModel = Create(sources, profiles, clipboard);

        viewModel.RefreshCommand.Execute(null);
        viewModel.CopyCommand.Execute(null);

        Assert.Equal(10, viewModel.ReportRows.Count);
        Assert.Equal(sources.VersionValue, viewModel.ReportRows.Single(row => row.Name == Strings.DiagnosticsApplicationVersion).Value);
        Assert.Equal(string.Join(Environment.NewLine, viewModel.ReportRows.Select(row => $"{row.Name}: {row.Value}")), viewModel.ReportText);
        Assert.Equal(viewModel.ReportText, clipboard.LastCopiedText);
    }

    [Fact]
    public void ReportRows_RefreshFailedSourcesIndependentlyAndNotifyTheView()
    {
        FakeDiagnosticsSources sources = new();
        using FakeProfileCatalog profiles = new();
        DiagnosticsViewModel viewModel = Create(sources, profiles);
        viewModel.RefreshCommand.Execute(null);
        IReadOnlyList<DiagnosticsReportRow> firstRows = viewModel.ReportRows;
        List<string?> changes = new();
        viewModel.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
        sources.FailingSources.Add(nameof(FakeDiagnosticsSources.ReadWindowsVersion));
        sources.AdapterCount = 7;

        viewModel.RefreshCommand.Execute(null);

        Assert.NotSame(firstRows, viewModel.ReportRows);
        Assert.Contains(nameof(DiagnosticsViewModel.ReportRows), changes);
        Assert.Equal(Strings.DiagnosticsUnreadable, viewModel.ReportRows.Single(row => row.Name == Strings.DiagnosticsWindowsVersion).Value);
        Assert.Equal("7", viewModel.ReportRows.Single(row => row.Name == Strings.DiagnosticsAdapterCount).Value);
    }

    private static FakeProfileCatalog CreateProfiles() => new()
    {
        ProfileItems = [TestData.Profile(profileId: "one"), TestData.Profile(profileId: "two")],
        ProblemItems = [new NetworkProfileProblem("invalid.json", ProfileLoadFailureKind.MalformedJson)]
    };

    private static DiagnosticsViewModel Create(
        FakeDiagnosticsSources sources,
        FakeProfileCatalog profiles,
        FakeClipboardService? clipboard = null) => new(
            sources, sources, sources, sources.ReadAdapterCount, profiles, sources, clipboard ?? new FakeClipboardService());
}
