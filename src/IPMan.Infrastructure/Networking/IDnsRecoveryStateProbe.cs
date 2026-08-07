using IPMan.Domain.Networking;

namespace IPMan.Infrastructure.Networking;

internal interface IDnsRecoveryStateProbe
{
    DnsRecoveryState ReadIpv4State(string adapterId);
}

internal sealed record DnsRecoveryState(
    DnsConfigurationMode Mode,
    string[] ConfiguredServers);
