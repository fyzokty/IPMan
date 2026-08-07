namespace IPMan.Domain.Networking;

/// <summary>
/// A user-requested static IPv4 configuration. Text may still require
/// validation and normalization; validated consumers receive a canonical copy.
/// </summary>
public sealed record StaticIpv4Configuration(
    string Ipv4Address,
    string SubnetMask,
    string? Gateway,
    string? PrimaryDns,
    string? SecondaryDns);
