namespace IPMan.Domain.Networking;

/// <summary>Field-level differences between current Windows state and a desired static configuration.</summary>
[Flags]
public enum NetworkConfigurationDifference
{
    None = 0,
    Ipv4Address = 1 << 0,
    SubnetMask = 1 << 1,
    Gateway = 1 << 2,
    PrimaryDns = 1 << 3,
    SecondaryDns = 1 << 4,
    Mode = 1 << 5
}
