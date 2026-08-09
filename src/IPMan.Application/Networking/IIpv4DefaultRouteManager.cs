using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public interface IIpv4DefaultRouteManager
{
    Ipv4DefaultRouteClearResult Clear(NetworkAdapterId adapterId);
}
