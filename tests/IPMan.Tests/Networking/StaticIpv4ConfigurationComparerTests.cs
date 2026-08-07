using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class StaticIpv4ConfigurationComparerTests
{
    private readonly StaticIpv4ConfigurationComparer _comparer = new();

    [Fact]
    public void Compare_WhenValuesAndModeAreEquivalent_ReturnsNoDifferences()
    {
        NetworkAdapterSnapshot current = TestData.Snapshot(
            mode: NetworkConfigurationMode.Static,
            gateway: " 192.168.001.001 ",
            primaryDns: null,
            secondaryDns: null);
        StaticIpv4Configuration desired = new(
            "192.168.1.50",
            "255.255.255.0",
            "192.168.1.1",
            null,
            null);

        NetworkConfigurationComparisonResult result = _comparer.Compare(current, desired);

        Assert.True(result.IsEquivalent);
        Assert.Equal(NetworkConfigurationDifference.None, result.Differences);
    }

    [Fact]
    public void Compare_WhenOptionalValuesAreAddedOrRemoved_ReportsTheirFields()
    {
        NetworkAdapterSnapshot current = TestData.Snapshot(
            mode: NetworkConfigurationMode.Static,
            gateway: null,
            primaryDns: "8.8.8.8",
            secondaryDns: "1.1.1.1");
        StaticIpv4Configuration desired = new(
            "192.168.1.50",
            "255.255.255.0",
            "192.168.1.1",
            "8.8.8.8",
            null);

        NetworkConfigurationComparisonResult result = _comparer.Compare(current, desired);

        Assert.True(result.HasDifference(NetworkConfigurationDifference.Gateway));
        Assert.True(result.HasDifference(NetworkConfigurationDifference.SecondaryDns));
        Assert.False(result.HasDifference(NetworkConfigurationDifference.PrimaryDns));
    }

    [Fact]
    public void Compare_WhenEveryDimensionDiffers_ReturnsCompleteFieldSet()
    {
        NetworkAdapterSnapshot current = TestData.Snapshot(
            mode: NetworkConfigurationMode.Dhcp,
            ipv4Address: "10.0.0.2",
            subnetMask: "255.255.0.0",
            gateway: "10.0.0.1",
            primaryDns: "10.0.0.1",
            secondaryDns: null);
        StaticIpv4Configuration desired = new(
            "192.168.1.50",
            "255.255.255.0",
            "192.168.1.1",
            "8.8.8.8",
            "1.1.1.1");

        NetworkConfigurationComparisonResult result = _comparer.Compare(current, desired);

        NetworkConfigurationDifference expected =
            NetworkConfigurationDifference.Ipv4Address |
            NetworkConfigurationDifference.SubnetMask |
            NetworkConfigurationDifference.Gateway |
            NetworkConfigurationDifference.PrimaryDns |
            NetworkConfigurationDifference.SecondaryDns |
            NetworkConfigurationDifference.Mode;
        Assert.Equal(expected, result.Differences);
    }

    [Theory]
    [InlineData(NetworkConfigurationMode.Dhcp)]
    [InlineData(NetworkConfigurationMode.Unknown)]
    public void Compare_WhenCurrentModeIsNotStatic_ReportsModeDifference(NetworkConfigurationMode mode)
    {
        NetworkAdapterSnapshot current = TestData.Snapshot(
            mode: mode,
            primaryDns: null,
            secondaryDns: null);
        StaticIpv4Configuration desired = new(
            "192.168.1.50",
            "255.255.255.0",
            "192.168.1.1",
            null,
            null);

        NetworkConfigurationComparisonResult result = _comparer.Compare(current, desired);

        Assert.True(result.HasDifference(NetworkConfigurationDifference.Mode));
    }
}
