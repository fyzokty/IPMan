namespace IPMan.Application.Networking;

public enum StaticIpv4SafetyBlock
{
    None = 0,
    MultipleIpv4Addresses = 1,
    MultipleIpv4Gateways = 2,
    TooManyIpv4DnsServers = 3
}
