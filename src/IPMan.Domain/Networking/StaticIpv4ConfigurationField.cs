namespace IPMan.Domain.Networking;

/// <summary>Identifies an editable field without coupling validation to presentation text.</summary>
public enum StaticIpv4ConfigurationField
{
    Ipv4Address = 0,
    SubnetMask = 1,
    Gateway = 2,
    PrimaryDns = 3,
    SecondaryDns = 4
}
