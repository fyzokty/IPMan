namespace IPMan.Domain.Networking;

/// <summary>Localization-independent reasons that a static IPv4 field is invalid.</summary>
public enum StaticIpv4ValidationErrorCode
{
    Required = 0,
    InvalidIpv4 = 1,
    AddressNotAllowed = 2,
    NonContiguousSubnetMask = 3,
    NetworkAddressNotAllowed = 4,
    BroadcastAddressNotAllowed = 5,
    GatewayOutsideSubnet = 6,
    PrimaryDnsRequired = 7,
    GatewayMatchesHostAddress = 8
}
