namespace IPMan.IntegrationTests.Harness;

public sealed record DestructiveNetworkTestSettingsResult(
    DestructiveNetworkTestSettings? Settings,
    IReadOnlyList<string> RefusalReasons)
{
    public bool CanRun => Settings is not null && RefusalReasons.Count == 0;
}
