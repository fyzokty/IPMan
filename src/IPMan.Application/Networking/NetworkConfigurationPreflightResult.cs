using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>Complete read-only outcome of one fresh adapter preflight.</summary>
public sealed record NetworkConfigurationPreflightResult(
    NetworkConfigurationPreflightStatus Status,
    NetworkAdapterSnapshot? CurrentSnapshot = null,
    StaticIpv4ValidationResult? Validation = null,
    NetworkConfigurationComparisonResult? Comparison = null,
    Ipv4ConflictProbeResult? ConflictProbe = null);
