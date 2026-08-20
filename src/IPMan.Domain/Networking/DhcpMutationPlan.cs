namespace IPMan.Domain.Networking;

/// <summary>
/// Describes the intent of a transition to DHCP while retaining the source mode
/// needed by recovery and diagnostics.
/// </summary>
/// <param name="PreviousMode">The adapter mode observed immediately before mutation.</param>
/// <param name="ReturnDnsToAutomatic">
/// Whether the mutation must return DNS server selection to Windows automatic configuration.
/// </param>
public sealed record DhcpMutationPlan(
    NetworkConfigurationMode PreviousMode,
    bool ReturnDnsToAutomatic);
