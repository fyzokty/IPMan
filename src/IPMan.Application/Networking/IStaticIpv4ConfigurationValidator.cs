using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public interface IStaticIpv4ConfigurationValidator
{
    StaticIpv4ValidationResult Validate(StaticIpv4Configuration configuration);
}
