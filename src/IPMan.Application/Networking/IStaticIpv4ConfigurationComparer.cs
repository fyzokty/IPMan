using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public interface IStaticIpv4ConfigurationComparer
{
    NetworkConfigurationComparisonResult Compare(
        NetworkAdapterSnapshot current,
        StaticIpv4Configuration normalizedDesired);
}
