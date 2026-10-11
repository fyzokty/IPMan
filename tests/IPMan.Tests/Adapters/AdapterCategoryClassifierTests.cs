using System.Net.NetworkInformation;
using IPMan.Domain.Adapters;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Adapters;

public sealed class AdapterCategoryClassifierTests
{
    [Theory]
    [InlineData(AdapterMedium.Wired, "Bluetooth Wireless WWAN", AdapterCategoryClassifier.ConnectionKind.Wired)]
    [InlineData(AdapterMedium.Wireless, "Bluetooth Ethernet", AdapterCategoryClassifier.ConnectionKind.Wireless)]
    [InlineData(AdapterMedium.Bluetooth, "Ethernet Wireless WWAN", AdapterCategoryClassifier.ConnectionKind.Bluetooth)]
    [InlineData(AdapterMedium.Cellular, "Bluetooth Wireless Ethernet", AdapterCategoryClassifier.ConnectionKind.Cellular)]
    public void GetConnectionKind_PhysicalMediumTakesPrecedenceOverName(
        AdapterMedium medium, string name, AdapterCategoryClassifier.ConnectionKind expected)
    {
        var adapter = TestData.Snapshot(name: name) with { Medium = medium };

        Assert.Equal(expected, AdapterCategoryClassifier.GetConnectionKind(adapter));
    }

    [Theory]
    [InlineData(AdapterMedium.Unknown, "Bluetooth Device (Personal Area Network)", AdapterCategoryClassifier.ConnectionKind.Bluetooth)]
    [InlineData(AdapterMedium.Other, "BLUETOOTH Device (Personal Area Network)", AdapterCategoryClassifier.ConnectionKind.Bluetooth)]
    [InlineData(AdapterMedium.Unknown, "Mobile Broadband Adapter", AdapterCategoryClassifier.ConnectionKind.Cellular)]
    [InlineData(AdapterMedium.Other, "Mobile Broadband Adapter", AdapterCategoryClassifier.ConnectionKind.Cellular)]
    [InlineData(AdapterMedium.Unknown, "CELLULAR Adapter", AdapterCategoryClassifier.ConnectionKind.Cellular)]
    [InlineData(AdapterMedium.Other, "Cellular Adapter", AdapterCategoryClassifier.ConnectionKind.Cellular)]
    [InlineData(AdapterMedium.Unknown, "WWAN Wireless Adapter", AdapterCategoryClassifier.ConnectionKind.Cellular)]
    [InlineData(AdapterMedium.Other, "WWAN Wireless Adapter", AdapterCategoryClassifier.ConnectionKind.Cellular)]
    [InlineData(AdapterMedium.Unknown, "Contoso WLAN", AdapterCategoryClassifier.ConnectionKind.Wireless)]
    [InlineData(AdapterMedium.Other, "Contoso Wi-Fi", AdapterCategoryClassifier.ConnectionKind.Wireless)]
    [InlineData(AdapterMedium.Unknown, "Contoso Adapter", AdapterCategoryClassifier.ConnectionKind.Wired)]
    [InlineData(AdapterMedium.Other, "Contoso Adapter", AdapterCategoryClassifier.ConnectionKind.Wired)]
    public void GetConnectionKind_WhenMediumIsUnknownOrOther_UsesDescriptionHeuristics(
        AdapterMedium medium, string description, AdapterCategoryClassifier.ConnectionKind expected)
    {
        var adapter = TestData.Snapshot(description: description) with { Medium = medium };

        Assert.Equal(expected, AdapterCategoryClassifier.GetConnectionKind(adapter));
    }

    [Theory]
    [InlineData(0u, AdapterCategoryClassifier.ConnectionKind.Bluetooth)]
    [InlineData(10u, AdapterCategoryClassifier.ConnectionKind.Bluetooth)]
    [InlineData(14u, AdapterCategoryClassifier.ConnectionKind.Wired)]
    public void GetConnectionKind_BluetoothPanWithEthernetInterfaceType_RespectsMediumAndIsVisible(
        uint medium, AdapterCategoryClassifier.ConnectionKind expected)
    {
        var adapter = NetworkAdapterMapper.Map(TestData.Adapter(
            description: "Bluetooth Device (Personal Area Network)",
            interfaceType: NetworkInterfaceType.Ethernet) with { PhysicalMediumType = medium });

        Assert.Equal(expected, AdapterCategoryClassifier.GetConnectionKind(adapter));
        Assert.Equal(AdapterCategoryClassifier.AdapterKind.Physical, AdapterCategoryClassifier.GetAdapterKind(adapter));
        Assert.False(AdapterCategoryClassifier.IsVirtual(adapter));
        Assert.True(AdapterCategoryClassifier.IsVisible(adapter, showVirtualAdapters: false));
    }

    [Fact]
    public void IsVisible_WhenVirtualAdaptersAreHidden_HidesVirtualAdapter()
    {
        var adapter = TestData.Snapshot(description: "Hyper-V Virtual Ethernet Adapter");

        Assert.True(AdapterCategoryClassifier.IsVirtual(adapter));
        Assert.Equal(AdapterCategoryClassifier.AdapterKind.Virtual, AdapterCategoryClassifier.GetAdapterKind(adapter));
        Assert.False(AdapterCategoryClassifier.IsVisible(adapter, showVirtualAdapters: false));
    }

    [Fact]
    public void IsVisible_WhenAdapterTypeIsUnknown_KeepsAdapterVisible()
    {
        var adapter = TestData.Snapshot(description: "Contoso Wired Network Adapter");

        Assert.False(AdapterCategoryClassifier.IsVirtual(adapter));
        Assert.Equal(AdapterCategoryClassifier.AdapterKind.Physical, AdapterCategoryClassifier.GetAdapterKind(adapter));
        Assert.Equal(AdapterCategoryClassifier.ConnectionKind.Wired, AdapterCategoryClassifier.GetConnectionKind(adapter));
        Assert.True(AdapterCategoryClassifier.IsVisible(adapter, showVirtualAdapters: false));
    }

    [Fact]
    public void GetConnectionKind_WhenAdapterIsWireless_ReturnsWireless()
    {
        var adapter = TestData.Snapshot(description: "Contoso Wi-Fi 6 Wireless Adapter");

        Assert.Equal(AdapterCategoryClassifier.ConnectionKind.Wireless, AdapterCategoryClassifier.GetConnectionKind(adapter));
    }
}
