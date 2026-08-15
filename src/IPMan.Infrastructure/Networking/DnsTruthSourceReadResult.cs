namespace IPMan.Infrastructure.Networking;

internal enum DnsTruthSourceReadStatus
{
    Success = 0,
    ProviderReadFailed = 1,
    AccessDenied = 2,
    InvalidData = 3,
    IdentityMismatch = 4,
    UnsupportedAddressFamily = 5,
    TargetMissing = 6,
    AmbiguousTarget = 7,
    NativeCallFailed = 8,
    NativeLibraryUnavailable = 9,
    NativeEntryPointUnavailable = 10,
    UnsupportedArchitecture = 11
}

internal sealed record DnsClientServerAddressFamilyReadResult(
    DnsTruthSourceReadStatus Status,
    uint? TechnicalCode,
    uint InterfaceIndex,
    string? InterfaceAlias,
    ushort AddressFamily,
    IReadOnlyList<string> Servers)
{
    public bool ReadSuccess => Status == DnsTruthSourceReadStatus.Success;
}

internal sealed record DnsClientServerAddressReadResult(
    DnsClientServerAddressFamilyReadResult Ipv4,
    DnsClientServerAddressFamilyReadResult Ipv6);

internal sealed record AdaptersAddressesDnsFamilyReadResult(
    DnsTruthSourceReadStatus Status,
    uint? NativeResult,
    bool ExactIdentityMatches,
    ushort AddressFamily,
    IReadOnlyList<string> Servers)
{
    public bool ReadSuccess => Status == DnsTruthSourceReadStatus.Success;
}

internal sealed record AdaptersAddressesDnsReadResult(
    AdaptersAddressesDnsFamilyReadResult Ipv4,
    AdaptersAddressesDnsFamilyReadResult Ipv6);
