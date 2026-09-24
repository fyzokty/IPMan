using IPMan.Domain.Adapters;
using Xunit;

namespace IPMan.Tests.Adapters;

public sealed class AdapterCategoryClassifierTests
{
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
