using System.Net.NetworkInformation;

namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Explicit rule for which Windows interfaces IPMan surfaces.
/// <para>
/// Release 1.0 manages every practical Windows adapter: physical Ethernet,
/// Wi-Fi, USB Ethernet, virtual and VPN adapters. Only software loopback is
/// hidden, because it is not a user-configurable adapter.
/// </para>
/// <para>
/// Tunnel and virtual adapters are deliberately <em>not</em> hidden: an adapter
/// that later turns out not to be configurable is communicated as such by the
/// UI rather than being made invisible. Display-name keyword heuristics are
/// never used to hide adapters.
/// </para>
/// </summary>
public static class AdapterDiscoveryFilter
{
    public static bool IsDiscoverable(NetworkInterfaceType interfaceType) =>
        interfaceType is not NetworkInterfaceType.Loopback;

    public static bool IsDiscoverable(AdapterReadModel adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        return IsDiscoverable(adapter.InterfaceType);
    }
}
