namespace IPMan.Infrastructure.Networking;

/// <summary>Sanitized outcome of the Windows DNS recovery-source probe.</summary>
public enum DnsRecoveryProbeStatus
{
    InvalidAdapterGuid = 0,
    NativeLibraryUnavailable = 1,
    NativeEntryPointUnavailable = 2,
    NativeCallFailed = 3,
    ProfileOrPolicyDnsDetected = 4,
    ManualAdapterFlagWithoutUsableIpv4Servers = 5,
    Automatic = 6,
    Manual = 7
}
