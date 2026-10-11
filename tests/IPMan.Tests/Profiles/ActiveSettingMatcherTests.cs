using IPMan.Application.Profiles;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;
using Xunit;

namespace IPMan.Tests.Profiles;

public sealed class ActiveSettingMatcherTests
{
    [Fact]
    public void Find_MatchesAllStaticValuesAndNormalizesWhitespace()
    {
        NetworkProfile profile = TestData.Profile(primaryDns: " 192.168.1.1 ", secondaryDns: " ", gateway: " 192.168.1.1 ");
        Assert.Same(profile, new ActiveSettingMatcher().Find(TestData.Snapshot(mode: NetworkConfigurationMode.Static), [profile]));
    }

    [Theory]
    [InlineData("address")]
    [InlineData("mask")]
    [InlineData("gateway")]
    [InlineData("dns1")]
    [InlineData("dns2")]
    public void Find_RejectsAnyDifferentField(string field)
    {
        NetworkProfile profile = TestData.Profile(primaryDns: "192.168.1.1", secondaryDns: null);
        profile = field switch
        {
            "address" => profile with { Ipv4Address = "192.168.1.51" },
            "mask" => profile with { SubnetMask = "255.255.0.0" },
            "gateway" => profile with { Gateway = null },
            "dns1" => profile with { PrimaryDns = "1.1.1.1" },
            _ => profile with { SecondaryDns = "8.8.8.8" }
        };
        Assert.Null(new ActiveSettingMatcher().Find(TestData.Snapshot(mode: NetworkConfigurationMode.Static), [profile]));
    }

    [Fact]
    public void Find_DhcpNeverMatchesStoredSettingsAndTiesAreDeterministic()
    {
        NetworkProfile first = TestData.Profile(profileId: "a", name: "Office", primaryDns: "192.168.1.1", secondaryDns: null);
        NetworkProfile second = first with { ProfileId = "b", IsFavorite = true };
        ActiveSettingMatcher matcher = new();
        Assert.Null(matcher.Find(TestData.Snapshot(), [first, second, first with { Mode = NetworkConfigurationMode.Dhcp }]));
        Assert.Same(first, matcher.Find(TestData.Snapshot(mode: NetworkConfigurationMode.Static), [second, first]));
        Assert.Same(first, matcher.Find(TestData.Snapshot(mode: NetworkConfigurationMode.Static), [first, second]));
    }
}
