using Xunit;

namespace IPMan.IntegrationTests.Harness;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class DnsTruthDiagnosticFactAttribute : FactAttribute
{
    public DnsTruthDiagnosticFactAttribute()
    {
        DnsTruthDiagnosticSettingsResult parsed =
            DnsTruthDiagnosticSettingsParser.Parse(ReadEnvironment());

        if (!parsed.CanRun)
        {
            Skip = "NOT EXECUTED — read-only DNS truth diagnostic opt-in was not satisfied. " +
                string.Join(" ", parsed.RefusalReasons);
        }
    }

    internal static Dictionary<string, string?> ReadEnvironment() =>
        new(StringComparer.Ordinal)
        {
            [DnsTruthDiagnosticSettingsParser.EnableVariable] =
                Environment.GetEnvironmentVariable(
                    DnsTruthDiagnosticSettingsParser.EnableVariable),
            [DnsTruthDiagnosticSettingsParser.AdapterVariable] =
                Environment.GetEnvironmentVariable(
                    DnsTruthDiagnosticSettingsParser.AdapterVariable)
        };
}
