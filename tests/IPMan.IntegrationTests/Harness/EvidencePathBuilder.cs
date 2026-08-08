namespace IPMan.IntegrationTests.Harness;

public static class EvidencePathBuilder
{
    public static string BuildRunDirectory(
        string evidenceRoot,
        DateTimeOffset utcTimestamp,
        Guid runId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceRoot);
        string folder = $"{utcTimestamp.ToUniversalTime():yyyyMMddTHHmmssfffZ}-{runId:N}";
        return Path.Combine(Path.GetFullPath(evidenceRoot), folder);
    }
}
