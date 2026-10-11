using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

/// <summary>Pure mapping tests only: never instantiate NLM or access COM.</summary>
public sealed class WindowsNetworkListConnectivityServiceTests
{
    [Theory]
    [InlineData(0x0u, false, InternetConnectivity.None)]
    [InlineData(0x0u, true, InternetConnectivity.None)]
    [InlineData(0x1u, false, InternetConnectivity.None)]
    [InlineData(0x1u, true, InternetConnectivity.LocalNetwork)]
    [InlineData(0x10u, true, InternetConnectivity.LocalNetwork)]
    [InlineData(0x20u, true, InternetConnectivity.LocalNetwork)]
    [InlineData(0x31u, true, InternetConnectivity.LocalNetwork)]
    [InlineData(0x40u, true, InternetConnectivity.Internet)]
    [InlineData(0x71u, true, InternetConnectivity.Internet)]
    [InlineData(0x400u, true, InternetConnectivity.None)]
    [InlineData(0x702u, true, InternetConnectivity.None)]
    [InlineData(0x440u, true, InternetConnectivity.Internet)]
    [InlineData(0x420u, true, InternetConnectivity.LocalNetwork)]
    public void MapConnectivity_UsesOnlyIpv4Flags(uint flags, bool connected, InternetConnectivity expected)
    {
        Assert.Equal(expected, WindowsNetworkListConnectivityService.MapConnectivity(flags, connected));
    }

    [Theory]
    [InlineData("abcdef01-2345-6789-abcd-ef0123456789")]
    [InlineData("{ABCDEF01-2345-6789-ABCD-EF0123456789}")]
    [InlineData("abcdef0123456789abcdef0123456789")]
    public void NormalizeAdapterId_MatchesManagedReaderIdentity(string windowsId)
    {
        NetworkAdapterSnapshot mapped = NetworkAdapterMapper.Map(TestData.Adapter(id: windowsId));
        NetworkAdapterId id = WindowsNetworkListConnectivityService.NormalizeAdapterId(Guid.Parse(windowsId));

        Assert.Equal(mapped.Id, id);
        Assert.Equal("{ABCDEF01-2345-6789-ABCD-EF0123456789}", id.Value);
    }
}
