namespace IPMan.Domain.Networking;

/// <summary>
/// A normalized user-requested static IPv4 configuration.
/// Validation/normalization is performed before this model reaches the
/// infrastructure mutation layer.
/// </summary>
public sealed record StaticIpv4Configuration(
    string Ipv4Address,
    string SubnetMask,
    string? Gateway,
    string? PrimaryDns,
    string? SecondaryDns);
