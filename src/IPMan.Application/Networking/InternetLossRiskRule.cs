using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>Evaluates the internet-loss advisory without performing network operations.</summary>
public static class InternetLossRiskRule
{
    /// <summary>A null target represents DHCP; malformed targets remain the validator's responsibility.</summary>
    public static bool IsRisky(InternetConnectivity connectivity, StaticIpv4Configuration? target)
    {
        if (connectivity != InternetConnectivity.Internet || target is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(target.Gateway))
        {
            return true;
        }

        return Ipv4Value.TryParse(target.Ipv4Address, out Ipv4Value address) &&
            Ipv4Value.TryParse(target.SubnetMask, out Ipv4Value mask) &&
            Ipv4Value.TryParse(target.Gateway, out Ipv4Value gateway) &&
            (address.Bits & mask.Bits) != (gateway.Bits & mask.Bits);
    }
}
