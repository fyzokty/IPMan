using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class InternetLossRiskRuleTests
{
    public static TheoryData<InternetConnectivity, string?, bool, bool> Matrix
    {
        get
        {
            TheoryData<InternetConnectivity, string?, bool, bool> data = [];
            foreach (InternetConnectivity connectivity in Enum.GetValues<InternetConnectivity>())
            {
                foreach (string? gateway in new[] { null, "192.168.0.1", "192.168.1.1" })
                {
                    foreach (bool dhcp in new[] { false, true })
                    {
                        data.Add(connectivity, gateway, dhcp,
                            connectivity == InternetConnectivity.Internet && !dhcp && gateway != "192.168.0.1");
                    }
                }
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public void IsRisky_ConnectivityGatewayAndModeMatrix_MatchesRule(
        InternetConnectivity connectivity, string? gateway, bool dhcp, bool expected)
    {
        StaticIpv4Configuration? target = dhcp ? null : new("192.168.0.50", "255.255.255.0", gateway, null, null);
        Assert.Equal(expected, InternetLossRiskRule.IsRisky(connectivity, target));
    }

    [Theory]
    [InlineData("255.255.0.0", "192.168.1.1", false)]
    [InlineData("255.255.255.128", "192.168.0.200", true)]
    [InlineData("255.255.255.0", " ", true)]
    public void IsRisky_TargetMaskAndBlankGateway_AreUsed(string mask, string gateway, bool expected) =>
        Assert.Equal(expected, InternetLossRiskRule.IsRisky(InternetConnectivity.Internet,
            new("192.168.0.50", mask, gateway, null, null)));
}
