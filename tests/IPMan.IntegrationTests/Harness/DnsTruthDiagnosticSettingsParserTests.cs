using Xunit;

namespace IPMan.IntegrationTests.Harness;

public sealed class DnsTruthDiagnosticSettingsParserTests
{
    private const string AdapterGuid = "{11111111-2222-3333-4444-555555555555}";

    [Fact]
    public void Parse_WithoutExplicitOptIn_RefusesDiagnostic()
    {
        DnsTruthDiagnosticSettingsResult result = Parse(null, AdapterGuid);

        Assert.False(result.CanRun);
        Assert.Contains(result.RefusalReasons, reason =>
            reason.Contains(DnsTruthDiagnosticSettingsParser.EnableVariable, StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_WithoutExactAdapterId_RefusesDiagnostic()
    {
        DnsTruthDiagnosticSettingsResult result = Parse("1", null);

        Assert.False(result.CanRun);
        Assert.Contains(result.RefusalReasons, reason =>
            reason.Contains(DnsTruthDiagnosticSettingsParser.AdapterVariable, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("{00000000-0000-0000-0000-000000000000}")]
    public void Parse_WithInvalidExactAdapterId_RefusesDiagnostic(string adapterId)
    {
        DnsTruthDiagnosticSettingsResult result = Parse("1", adapterId);

        Assert.False(result.CanRun);
    }

    [Fact]
    public void Parse_WithExplicitOptInAndExactGuid_AllowsReadOnlyDiagnostic()
    {
        DnsTruthDiagnosticSettingsResult result = Parse("1", AdapterGuid.ToLowerInvariant());

        Assert.True(result.CanRun);
        Assert.Equal(AdapterGuid.ToUpperInvariant(), result.Settings!.AdapterId.Value);
    }

    private static DnsTruthDiagnosticSettingsResult Parse(
        string? enabled,
        string? adapterId) =>
        DnsTruthDiagnosticSettingsParser.Parse(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [DnsTruthDiagnosticSettingsParser.EnableVariable] = enabled,
                [DnsTruthDiagnosticSettingsParser.AdapterVariable] = adapterId
            });
}
