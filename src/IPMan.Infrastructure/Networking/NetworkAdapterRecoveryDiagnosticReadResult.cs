using IPMan.Application.Networking;

namespace IPMan.Infrastructure.Networking;

/// <summary>Production recovery read plus sanitized Windows-specific diagnostics.</summary>
public sealed record NetworkAdapterRecoveryDiagnosticReadResult(
    NetworkAdapterRecoveryReadResult RecoveryRead,
    DnsRecoveryProbeDiagnostic? DnsProbe);
