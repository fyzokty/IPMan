using IPMan.Domain.Networking;
using IPMan.IntegrationTests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace IPMan.IntegrationTests;

public sealed class DnsTruthDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public DnsTruthDiagnosticTests(ITestOutputHelper output) => _output = output;

    [DnsTruthDiagnosticFact]
    [Trait("Category", "DnsTruthDiagnostic")]
    public void ReadExactAdapterDnsTruthSources_WithoutMutation()
    {
        DnsTruthDiagnosticSettingsResult parsed =
            DnsTruthDiagnosticSettingsParser.Parse(
                DnsTruthDiagnosticFactAttribute.ReadEnvironment());

        if (!parsed.CanRun)
        {
            throw new InvalidOperationException(
                "DNS truth diagnostic opt-in changed after discovery; refusing the live read.");
        }

        NetworkAdapterId adapterId = parsed.Settings!.AdapterId;
        DnsTruthDiagnosticReport report = new DnsTruthDiagnosticCapture().Capture(adapterId);

        foreach (string line in report.FormatLines())
        {
            _output.WriteLine(line);
        }

        Assert.False(DnsTruthDiagnosticReport.MutationPerformed);
        Assert.True(
            report.AllSourcesReadSuccessfully,
            "One or more exact-adapter DNS truth sources failed closed. Review diagnostic output.");
    }
}
