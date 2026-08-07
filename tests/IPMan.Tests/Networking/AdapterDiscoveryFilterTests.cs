using System.Net.NetworkInformation;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class AdapterDiscoveryFilterTests
{
    [Theory]
    [InlineData(NetworkInterfaceType.Ethernet)]
    [InlineData(NetworkInterfaceType.Wireless80211)]
    [InlineData(NetworkInterfaceType.GigabitEthernet)]
    [InlineData(NetworkInterfaceType.Ppp)]
    public void IsDiscoverable_ForPhysicalAndWirelessAdapters_ReturnsTrue(NetworkInterfaceType type)
    {
        Assert.True(AdapterDiscoveryFilter.IsDiscoverable(type));
    }

    [Theory]
    [InlineData(NetworkInterfaceType.Tunnel)]
    [InlineData(NetworkInterfaceType.Unknown)]
    [InlineData(NetworkInterfaceType.Ethernet3Megabit)]
    public void IsDiscoverable_ForTunnelAndVirtualAdapters_ReturnsTrue(NetworkInterfaceType type)
    {
        // Release 1.0 manages virtual and VPN adapters too; an adapter that turns
        // out not to be configurable is reported as such, never hidden.
        Assert.True(AdapterDiscoveryFilter.IsDiscoverable(type));
    }

    [Fact]
    public void IsDiscoverable_ForSoftwareLoopback_ReturnsFalse()
    {
        Assert.False(AdapterDiscoveryFilter.IsDiscoverable(NetworkInterfaceType.Loopback));
    }

    [Fact]
    public void IsDiscoverable_ForAdapter_UsesItsInterfaceType()
    {
        Assert.False(AdapterDiscoveryFilter.IsDiscoverable(
            TestData.Adapter(interfaceType: NetworkInterfaceType.Loopback)));

        Assert.True(AdapterDiscoveryFilter.IsDiscoverable(
            TestData.Adapter(interfaceType: NetworkInterfaceType.Ethernet)));
    }

    [Fact]
    public void IsDiscoverable_IgnoresDisplayName()
    {
        // No keyword heuristics: naming must never decide visibility.
        Assert.True(AdapterDiscoveryFilter.IsDiscoverable(
            TestData.Adapter(name: "VPN - Contoso", description: "Virtual Adapter")));
    }
}
