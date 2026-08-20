using IPMan.Domain.Profiles;
using Xunit;

namespace IPMan.Tests.Profiles;

public sealed class ProfileNameResolverTests
{
    [Fact]
    public void ResolveUnique_WhenDesiredNameIsEmpty_ReturnsItUnchanged()
    {
        string result = ProfileNameResolver.ResolveUnique(string.Empty, ["PLC"]);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ResolveUnique_WhenDesiredNameIsTaken_ReturnsFirstNumberedVariant()
    {
        string result = ProfileNameResolver.ResolveUnique("PLC", ["PLC"]);

        Assert.Equal("PLC (1)", result);
    }

    [Fact]
    public void ResolveUnique_WhenFirstNumberedVariantIsTaken_ReturnsNextVariant()
    {
        string result = ProfileNameResolver.ResolveUnique("PLC", ["PLC", "PLC (1)"]);

        Assert.Equal("PLC (2)", result);
    }

    [Fact]
    public void ResolveUnique_WhenExistingNameDiffersOnlyByCase_ReturnsNumberedVariant()
    {
        string result = ProfileNameResolver.ResolveUnique("PLC", ["plc"]);

        Assert.Equal("PLC (1)", result);
    }

    [Fact]
    public void ResolveUnique_WhenExistingNamesAreEmpty_ReturnsDesiredName()
    {
        string result = ProfileNameResolver.ResolveUnique("PLC", []);

        Assert.Equal("PLC", result);
    }
}
