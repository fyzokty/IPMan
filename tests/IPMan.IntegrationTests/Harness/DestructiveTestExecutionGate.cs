namespace IPMan.IntegrationTests.Harness;

public static class DestructiveTestExecutionGate
{
    public const string NotExecutedPrefix = "NOT EXECUTED — ";

    public static string? GetSkipReason(DestructiveNetworkTestSettingsResult settingsResult)
    {
        ArgumentNullException.ThrowIfNull(settingsResult);

        return settingsResult.CanRun
            ? null
            : NotExecutedPrefix +
                "destructive Sprint 08 opt-in gate was not satisfied. " +
                string.Join(" ", settingsResult.RefusalReasons);
    }
}
