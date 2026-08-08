namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Stable structural metadata for DNS recovery diagnostics. It intentionally
/// excludes raw exception text, DNS values and native pointer contents.
/// </summary>
public sealed record DnsRecoveryProbeDiagnostic(
    DnsRecoveryProbeStatus Status,
    uint? NativeResult,
    bool AdapterManualServerFlag,
    bool ProfileServerFlag,
    int UsableIpv4ServerCount);
