using System.Reflection;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.IntegrationTests.Evidence;
using IPMan.IntegrationTests.Harness;
using IPMan.IntegrationTests.Observation;
using IPMan.Infrastructure.Networking;
using Xunit;
using Xunit.Abstractions;

namespace IPMan.IntegrationTests;

public sealed class DestructiveStaticMutationTests
{
    private readonly ITestOutputHelper _output;

    public DestructiveStaticMutationTests(ITestOutputHelper output) => _output = output;

    [DestructiveNetworkFact]
    [Trait("Category", "DestructiveNetwork")]
    public async Task RunOneExplicitScenario_WhenEverySafetyGateIsSatisfied()
    {
        string repositoryRoot = FindRepositoryRoot();
        DestructiveNetworkTestSettingsResult parsed = DestructiveNetworkTestSettingsParser.Parse(
            DestructiveNetworkTestEnvironment.Read(),
            repositoryRoot);

        if (!parsed.CanRun)
        {
            throw new InvalidOperationException(
                "Destructive opt-in state changed after test discovery; refusing mutation.");
        }

        DestructiveNetworkTestSettings settings = parsed.Settings!;
        _output.WriteLine("DESTRUCTIVE NETWORK TEST ENABLED FOR ONE EXPLICIT SCENARIO.");
        _output.WriteLine($"Exact target adapter: {settings.AdapterId.Value}");
        _output.WriteLine($"Scenario: {settings.Scenario}");
        _output.WriteLine(
            "WARNING: this mutation can disconnect the machine; use local/VM console and never the remote-management path.");

        using ProductionHarnessContext production = ProductionHarnessFactory.Create();
        ManagedAdapterDiagnosticRead managedBefore = await RecoveryDiagnosticCapture
            .ReadManagedAdapterAsync(
                production.AdapterReader,
                settings.AdapterId,
                CancellationToken.None);
        NetworkAdapterRecoveryDiagnosticReadResult recoveryDiagnostic =
            await production.RecoveryReader.ReadDiagnosticAsync(
                settings.AdapterId,
                CancellationToken.None);
        NetworkAdapterRecoveryReadResult recoveryRead = recoveryDiagnostic.RecoveryRead;
        RecoveryDiagnosticReport diagnosticReport = RecoveryDiagnosticReport.Create(
            settings.AdapterId,
            managedBefore,
            recoveryDiagnostic);

        if (!diagnosticReport.CanProceed)
        {
            Assert.Fail(
                "NOT EXECUTED — exact adapter recovery state was unavailable or not restore-capable." +
                Environment.NewLine +
                string.Join(Environment.NewLine, diagnosticReport.FormatSanitizedLines()));
        }

        NetworkAdapterRecoverySnapshot preconditionRecoverySnapshot = recoveryRead.Snapshot!;

        ScenarioPreconditionResult preconditions = ScenarioPreconditionValidator.Validate(
            settings.Scenario,
            preconditionRecoverySnapshot,
            settings.DesiredConfiguration);

        if (!preconditions.IsAllowed)
        {
            Assert.Fail(
                "NOT EXECUTED — the exact adapter did not satisfy scenario preconditions. " +
                string.Join(" ", preconditions.RefusalReasons));
        }

        NetworkObservation beforeObservation = ExactNetworkObserver.Read(settings.AdapterId);
        DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow;
        Guid runId = Guid.NewGuid();
        string runDirectory = EvidencePathBuilder.BuildRunDirectory(
            settings.EvidenceRoot,
            startedAtUtc,
            runId);
        EvidenceWriter evidenceWriter = new(runDirectory);
        Sprint08BeforeEvidence beforeEvidence = new(
            runId.ToString("N"),
            startedAtUtc,
            Environment.OSVersion.VersionString,
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
            settings.Scenario,
            preconditions.RequestedDimensions,
            settings.AdapterId,
            settings.DesiredConfiguration,
            beforeObservation,
            preconditionRecoverySnapshot);
        await evidenceWriter.WriteBeforeAsync(beforeEvidence, CancellationToken.None);
        _output.WriteLine($"Evidence directory: {evidenceWriter.DirectoryPath}");

        StaticIpv4ApplyResult? applyResult = null;
        NetworkObservation? afterObservation = null;
        NetworkAdapterRecoveryReadResult? afterRecovery = null;
        bool recoveryMatches = false;
        NetworkAdapterRecoveryReadResult? applyRecoverySourceRead = null;
        IReadOnlyList<string> differences = Array.Empty<string>();
        string? failure = null;

        try
        {
            production.ApplyRecoveryReader.BeginApplyCapture();
            try
            {
                applyResult = await production.ApplyService.ApplyAsync(
                        new StaticIpv4ApplyRequest(
                            settings.AdapterId,
                            settings.DesiredConfiguration,
                            ConfirmPotentialConflict: true,
                            ContinueAfterIndeterminateProbe: true),
                        CancellationToken.None);
            }
            finally
            {
                applyRecoverySourceRead = production.ApplyRecoveryReader.EndApplyCapture();
            }

            afterObservation = ExactNetworkObserver.Read(settings.AdapterId);
            afterRecovery = await production.RecoveryReader
                .ReadAsync(settings.AdapterId, CancellationToken.None);
            NetworkAdapterRecoverySnapshot? applyRecoverySource =
                applyRecoverySourceRead?.Status == NetworkAdapterRecoveryReadStatus.Success
                    ? applyRecoverySourceRead.Snapshot
                    : null;
            recoveryMatches = applyRecoverySource is not null &&
                await RecoverySnapshotVerifier
                    .MatchesAsync(
                        applyResult.Recovery,
                        applyRecoverySource,
                        CancellationToken.None)
                    .ConfigureAwait(false);
            ObservationComparisonResult nonInterference = ObservationComparer.CompareNonInterference(
                beforeObservation,
                afterObservation,
                preconditions.RequestedDimensions);
            differences = ScenarioOutcomeVerifier.Verify(
                settings,
                applyResult,
                preconditionRecoverySnapshot,
                afterRecovery,
                preconditions.RequestedDimensions,
                nonInterference,
                recoveryMatches);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            failure = $"{exception.GetType().Name}: {exception.Message}";
            differences = new[] { "The destructive scenario raised an exception; inspect local raw evidence." };
        }

        bool passed = failure is null && differences.Count == 0;
        Sprint08ResultEvidence resultEvidence = new(
            beforeEvidence,
            DateTimeOffset.UtcNow,
            applyResult,
            afterObservation,
            afterRecovery,
            recoveryMatches,
            passed,
            differences,
            failure);
        await evidenceWriter.WriteResultAsync(resultEvidence, CancellationToken.None);

        Assert.True(
            passed,
            $"Sprint 08 scenario failed. Evidence: {evidenceWriter.DirectoryPath}. " +
            string.Join(" ", differences));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "IPMan.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the IPMan repository root.");
    }
}
