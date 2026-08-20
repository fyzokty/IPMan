using System.Globalization;
using IPMan.Application.Profiles;
using IPMan.Domain.Profiles;
using Xunit;

namespace IPMan.Tests.Profiles;

public sealed class ProfileSearchTests
{
    [Fact]
    public void Matches_WhenQueryMatchesName_ReturnsTrue()
    {
        NetworkProfile profile = TestData.Profile(name: "Factory PLC");

        bool result = ProfileSearch.Matches(profile, "PLC");

        Assert.True(result);
    }

    [Fact]
    public void Matches_WhenQueryMatchesDescription_ReturnsTrue()
    {
        NetworkProfile profile = TestData.Profile(description: "Packaging line controller");

        bool result = ProfileSearch.Matches(profile, "line controller");

        Assert.True(result);
    }

    [Fact]
    public void Matches_WhenQueryMatchesAnyIpValue_ReturnsTrue()
    {
        NetworkProfile profile = TestData.Profile(
            ipv4Address: "10.20.30.40",
            subnetMask: "255.255.254.0",
            gateway: "10.20.30.1",
            primaryDns: "1.1.1.1",
            secondaryDns: "8.8.4.4");

        Assert.True(ProfileSearch.Matches(profile, "30.40"));
        Assert.True(ProfileSearch.Matches(profile, "254.0"));
        Assert.True(ProfileSearch.Matches(profile, "30.1"));
        Assert.True(ProfileSearch.Matches(profile, "1.1.1.1"));
        Assert.True(ProfileSearch.Matches(profile, "8.8.4.4"));
    }

    [Fact]
    public void Matches_WhenQueryIsEmptyOrWhitespace_ReturnsTrue()
    {
        NetworkProfile profile = TestData.Profile();

        Assert.True(ProfileSearch.Matches(profile, null));
        Assert.True(ProfileSearch.Matches(profile, string.Empty));
        Assert.True(ProfileSearch.Matches(profile, "   "));
    }

    [Fact]
    public void Matches_WhenNameUsesTurkishCasing_IsCaseInsensitiveInCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            NetworkProfile profile = TestData.Profile(name: "İstanbul Ofisi");

            bool result = ProfileSearch.Matches(profile, "istanbul");

            Assert.True(result);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Matches_WhenNoFieldContainsQuery_ReturnsFalse()
    {
        NetworkProfile profile = TestData.Profile();

        bool result = ProfileSearch.Matches(profile, "unrelated");

        Assert.False(result);
    }
}
