namespace IPMan.IntegrationTests.Harness;

public sealed record ScenarioPreconditionResult(
    IReadOnlyList<string> RefusalReasons,
    IReadOnlyList<string> RequestedDimensions)
{
    public bool IsAllowed => RefusalReasons.Count == 0;
}
