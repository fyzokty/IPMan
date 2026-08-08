using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using IPMan.IntegrationTests.Harness;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace IPMan.IntegrationTests;

public sealed class ReadOnlyRecoveryDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public ReadOnlyRecoveryDiagnosticTests(ITestOutputHelper output) => _output = output;

    [ReadOnlyRecoveryDiagnosticFact]
    [Trait("Category", "ReadOnlyRecoveryDiagnostic")]
    public async Task ReadExactAdapterRecoveryDiagnostic_WithoutMutation()
    {
        ReadOnlyRecoveryDiagnosticSettingsResult parsed =
            ReadOnlyRecoveryDiagnosticSettingsParser.Parse(
                ReadOnlyRecoveryDiagnosticFactAttribute.ReadEnvironment());

        if (!parsed.CanRun)
        {
            throw new InvalidOperationException(
                "Read-only diagnostic opt-in changed after discovery; refusing the live read.");
        }

        NetworkAdapterId adapterId = parsed.Settings!.AdapterId;
        INetworkAdapterReader adapterReader = new NetworkAdapterReader(
            new SystemNetworkInterfaceProbe(
                NullLogger<SystemNetworkInterfaceProbe>.Instance),
            NullLogger<NetworkAdapterReader>.Instance);
        WmiNetworkAdapterRecoveryReader recoveryReader = new();
        ManagedAdapterDiagnosticRead managed = await RecoveryDiagnosticCapture
            .ReadManagedAdapterAsync(adapterReader, adapterId, CancellationToken.None);
        NetworkAdapterRecoveryDiagnosticReadResult recovery = await recoveryReader
            .ReadDiagnosticAsync(adapterId, CancellationToken.None);
        RecoveryDiagnosticReport report = RecoveryDiagnosticReport.Create(
            adapterId,
            managed,
            recovery);
        IReadOnlyList<string> lines = report.FormatSanitizedLines();

        foreach (string line in lines)
        {
            _output.WriteLine(line);
        }

        Assert.DoesNotContain(lines, line =>
            line.Contains(adapterId.Value, StringComparison.OrdinalIgnoreCase));
    }
}
