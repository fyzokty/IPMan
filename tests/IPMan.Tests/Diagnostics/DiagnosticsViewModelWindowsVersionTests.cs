using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Diagnostics;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Diagnostics;

public sealed class DiagnosticsViewModelWindowsVersionTests
{
    [Theory]
    [InlineData("", "Windows 10 Pro — derleme 10240.1234, X64")]
    [InlineData(" \t", "Windows 10 Pro — derleme 10240.1234, X64")]
    [InlineData("1507", "Windows 10 Pro — 1507, derleme 10240.1234, X64")]
    public void Refresh_WhenDisplayVersionIsOptional_PreservesProductBuildAndArchitecture(
        string displayVersion, string expected)
    {
        FakeDiagnosticsSources sources = new()
        {
            WindowsVersion = new WindowsVersionInfo("Windows 10 Pro", displayVersion, "10240.1234", "X64")
        };
        using FakeProfileCatalog profiles = new();
        DiagnosticsViewModel viewModel = new(
            sources, sources, sources, sources.ReadAdapterCount, profiles, sources, new FakeClipboardService());

        viewModel.RefreshCommand.Execute(null);

        Assert.Equal($"{Strings.DiagnosticsWindowsVersion}: {expected}", viewModel.ReportText.Split(Environment.NewLine)[0]);
    }
}
