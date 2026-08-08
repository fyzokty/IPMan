using IPMan.Domain.Networking;

namespace IPMan.IntegrationTests.Harness;

public static class ReadOnlyRecoveryDiagnosticSettingsParser
{
    public const string EnableVariable = "IPMAN_RECOVERY_DIAGNOSTIC";
    public const string AdapterVariable = DestructiveNetworkTestSettingsParser.AdapterVariable;

    public static ReadOnlyRecoveryDiagnosticSettingsResult Parse(
        IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        List<string> reasons = new();

        if (!values.TryGetValue(EnableVariable, out string? enabled) ||
            !string.Equals(enabled, "1", StringComparison.Ordinal))
        {
            reasons.Add($"{EnableVariable} must exactly equal 1.");
        }

        NetworkAdapterId? adapterId = null;
        values.TryGetValue(AdapterVariable, out string? adapterValue);

        if (string.IsNullOrWhiteSpace(adapterValue))
        {
            reasons.Add($"Missing {AdapterVariable}; an exact adapter GUID is required.");
        }
        else if (!Guid.TryParse(adapterValue, out Guid parsed) || parsed == Guid.Empty)
        {
            reasons.Add($"Invalid {AdapterVariable}; supply one non-empty interface GUID.");
        }
        else
        {
            adapterId = new NetworkAdapterId(parsed.ToString("B"));
        }

        return reasons.Count == 0 && adapterId.HasValue
            ? new ReadOnlyRecoveryDiagnosticSettingsResult(
                new ReadOnlyRecoveryDiagnosticSettings(adapterId.Value),
                Array.Empty<string>())
            : new ReadOnlyRecoveryDiagnosticSettingsResult(null, reasons);
    }
}
