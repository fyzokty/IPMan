using IPMan.Domain.Networking;
using Xunit;

namespace IPMan.IntegrationTests.Harness;

public sealed class DestructiveNetworkTestSettingsParserTests
{
    private const string RepositoryRoot = "C:\\repo";
    private const string ExactAdapter = "{11111111-2222-3333-4444-555555555555}";

    [Fact]
    public void Parse_WhenEnableFlagIsAbsent_Refuses()
    {
        Dictionary<string, string?> values = CompleteValues();
        values.Remove(DestructiveNetworkTestSettingsParser.EnableVariable);

        DestructiveNetworkTestSettingsResult result = Parse(values);

        Assert.False(result.CanRun);
        Assert.Contains(result.RefusalReasons, reason =>
            reason.Contains(DestructiveNetworkTestSettingsParser.EnableVariable, StringComparison.Ordinal));
    }

    [Fact]
    public void ExecutionGate_WhenOptInsAreAbsent_ReportsNotExecuted()
    {
        DestructiveNetworkTestSettingsResult parsed = Parse(
            new Dictionary<string, string?>(StringComparer.Ordinal));

        string? reason = DestructiveTestExecutionGate.GetSkipReason(parsed);

        Assert.StartsWith(DestructiveTestExecutionGate.NotExecutedPrefix, reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ExecutionGate_WhenAllOptInsAreValid_AllowsExecution()
    {
        DestructiveNetworkTestSettingsResult parsed = Parse(CompleteValues());

        Assert.Null(DestructiveTestExecutionGate.GetSkipReason(parsed));
    }

    [Fact]
    public void Parse_WhenAdapterIdIsMissing_Refuses()
    {
        Dictionary<string, string?> values = CompleteValues();
        values.Remove(DestructiveNetworkTestSettingsParser.AdapterVariable);

        DestructiveNetworkTestSettingsResult result = Parse(values);

        Assert.False(result.CanRun);
        Assert.Contains(result.RefusalReasons, reason =>
            reason.Contains("exact adapter GUID", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_WhenAdapterIdIsInvalid_Refuses()
    {
        Dictionary<string, string?> values = CompleteValues();
        values[DestructiveNetworkTestSettingsParser.AdapterVariable] = "Ethernet 2";

        DestructiveNetworkTestSettingsResult result = Parse(values);

        Assert.False(result.CanRun);
        Assert.Contains(result.RefusalReasons, reason =>
            reason.Contains("Invalid", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_WhenIsolatedAcknowledgementIsMissing_Refuses()
    {
        Dictionary<string, string?> values = CompleteValues();
        values.Remove(DestructiveNetworkTestSettingsParser.IsolatedVariable);

        DestructiveNetworkTestSettingsResult result = Parse(values);

        Assert.False(result.CanRun);
        Assert.Contains(result.RefusalReasons, reason =>
            reason.Contains(DestructiveNetworkTestSettingsParser.IsolatedVariable, StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_WhenFilterAcknowledgementIsMissing_Refuses()
    {
        Dictionary<string, string?> values = CompleteValues();
        values.Remove(DestructiveNetworkTestSettingsParser.FilterVariable);

        Assert.False(Parse(values).CanRun);
    }

    [Fact]
    public void Parse_WhenRequiredConfigurationIsMissing_Refuses()
    {
        Dictionary<string, string?> values = CompleteValues();
        values.Remove(DestructiveNetworkTestSettingsParser.DnsVariable);

        Assert.False(Parse(values).CanRun);
    }

    [Fact]
    public void Parse_WithEveryOptIn_PreservesExactGuidAndConfiguration()
    {
        DestructiveNetworkTestSettingsResult result = Parse(CompleteValues());

        Assert.True(result.CanRun);
        DestructiveNetworkTestSettings settings = Assert.IsType<DestructiveNetworkTestSettings>(result.Settings);
        Assert.Equal(new NetworkAdapterId(ExactAdapter), settings.AdapterId);
        Assert.Equal(Sprint08Scenario.StaticToStatic, settings.Scenario);
        Assert.Equal("192.0.2.50", settings.DesiredConfiguration.Ipv4Address);
        Assert.Equal("192.0.2.1", settings.DesiredConfiguration.Gateway);
        Assert.Null(settings.DesiredConfiguration.PrimaryDns);
    }

    [Fact]
    public void EvidencePath_UsesOnlyExplicitRootTimestampAndRunId()
    {
        Guid runId = Guid.Parse("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");

        string path = EvidencePathBuilder.BuildRunDirectory(
            "C:\\repo\\artifacts\\integration\\sprint08",
            new DateTimeOffset(2026, 8, 8, 10, 11, 12, 123, TimeSpan.Zero),
            runId);

        Assert.Equal(
            "C:\\repo\\artifacts\\integration\\sprint08\\20260808T101112123Z-aaaaaaaabbbbccccddddeeeeeeeeeeee",
            path);
    }

    [Fact]
    public void ExactIdentityMatcher_WhenExactGuidIsAbsent_DoesNotSelectFirstCandidate()
    {
        Candidate first = new("{AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA}");
        Candidate second = new("{BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB}");

        Candidate? result = ExactIdentityMatcher.FindUnique(
            new[] { first, second },
            candidate => candidate.Id,
            ExactAdapter);

        Assert.Null(result);
    }

    [Fact]
    public void ExactIdentityMatcher_WhenIdentityIsAmbiguous_Refuses()
    {
        Candidate first = new(ExactAdapter);
        Candidate duplicate = new(ExactAdapter.ToLowerInvariant());

        Candidate? result = ExactIdentityMatcher.FindUnique(
            new[] { first, duplicate },
            candidate => candidate.Id,
            ExactAdapter);

        Assert.Null(result);
    }

    private static DestructiveNetworkTestSettingsResult Parse(
        IReadOnlyDictionary<string, string?> values) =>
        DestructiveNetworkTestSettingsParser.Parse(values, RepositoryRoot);

    private static Dictionary<string, string?> CompleteValues() =>
        new(StringComparer.Ordinal)
        {
            [DestructiveNetworkTestSettingsParser.EnableVariable] = "1",
            [DestructiveNetworkTestSettingsParser.AdapterVariable] = ExactAdapter,
            [DestructiveNetworkTestSettingsParser.IsolatedVariable] =
                DestructiveNetworkTestSettingsParser.IsolatedAcknowledgement,
            [DestructiveNetworkTestSettingsParser.FilterVariable] =
                DestructiveNetworkTestSettingsParser.FilterAcknowledgement,
            [DestructiveNetworkTestSettingsParser.ConflictVariable] =
                DestructiveNetworkTestSettingsParser.ConflictAcknowledgement,
            [DestructiveNetworkTestSettingsParser.ScenarioVariable] = nameof(Sprint08Scenario.StaticToStatic),
            [DestructiveNetworkTestSettingsParser.Ipv4Variable] = "192.0.2.50",
            [DestructiveNetworkTestSettingsParser.SubnetVariable] = "255.255.255.0",
            [DestructiveNetworkTestSettingsParser.GatewayVariable] = "192.0.2.1",
            [DestructiveNetworkTestSettingsParser.DnsVariable] = "NONE"
        };

    private sealed record Candidate(string Id);
}
