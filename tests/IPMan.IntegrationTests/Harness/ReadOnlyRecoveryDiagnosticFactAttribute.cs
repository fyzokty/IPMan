using Xunit;

namespace IPMan.IntegrationTests.Harness;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ReadOnlyRecoveryDiagnosticFactAttribute : FactAttribute
{
    public ReadOnlyRecoveryDiagnosticFactAttribute()
    {
        ReadOnlyRecoveryDiagnosticSettingsResult parsed =
            ReadOnlyRecoveryDiagnosticSettingsParser.Parse(ReadEnvironment());

        if (!parsed.CanRun)
        {
            Skip = "NOT EXECUTED — read-only recovery diagnostic opt-in was not satisfied. " +
                string.Join(" ", parsed.RefusalReasons);
        }
    }

    internal static Dictionary<string, string?> ReadEnvironment() =>
        new(StringComparer.Ordinal)
        {
            [ReadOnlyRecoveryDiagnosticSettingsParser.EnableVariable] =
                Environment.GetEnvironmentVariable(
                    ReadOnlyRecoveryDiagnosticSettingsParser.EnableVariable),
            [ReadOnlyRecoveryDiagnosticSettingsParser.AdapterVariable] =
                Environment.GetEnvironmentVariable(
                    ReadOnlyRecoveryDiagnosticSettingsParser.AdapterVariable)
        };
}
