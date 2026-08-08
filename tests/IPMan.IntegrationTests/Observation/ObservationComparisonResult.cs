namespace IPMan.IntegrationTests.Observation;

public sealed record ObservationComparisonResult(
    bool IsMatch,
    IReadOnlyList<string> Differences);
