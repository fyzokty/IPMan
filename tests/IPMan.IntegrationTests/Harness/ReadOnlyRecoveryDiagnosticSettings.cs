using IPMan.Domain.Networking;

namespace IPMan.IntegrationTests.Harness;

public sealed record ReadOnlyRecoveryDiagnosticSettings(NetworkAdapterId AdapterId);

public sealed record ReadOnlyRecoveryDiagnosticSettingsResult(
    ReadOnlyRecoveryDiagnosticSettings? Settings,
    IReadOnlyList<string> RefusalReasons)
{
    public bool CanRun => Settings is not null && RefusalReasons.Count == 0;
}
