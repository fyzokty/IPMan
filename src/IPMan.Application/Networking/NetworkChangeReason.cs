namespace IPMan.Application.Networking;

/// <summary>
/// Technical (non user-facing) identifiers describing why a refresh was
/// requested. These values are diagnostics, not localized UI text.
/// </summary>
public static class NetworkChangeReason
{
    public const string InitialDiscovery = "InitialDiscovery";

    public const string NetworkAddressChanged = "NetworkAddressChanged";

    public const string NetworkAvailabilityChanged = "NetworkAvailabilityChanged";

    public const string ManualRefresh = "ManualRefresh";

    /// <summary>A refresh requested by IPMan immediately after it changed configuration.</summary>
    public const string ConfigurationApplied = "ConfigurationApplied";

    /// <summary>Low-frequency fallback pass, not a Windows-reported change.</summary>
    public const string ScheduledReconciliation = "ScheduledReconciliation";
}
