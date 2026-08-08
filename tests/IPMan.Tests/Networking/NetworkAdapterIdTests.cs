using IPMan.Domain.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class NetworkAdapterIdTests
{
    private const string CanonicalGuid = "{827A2938-BB14-4D18-B67F-94E9C4F818BA}";
    private const string LowercaseBracedGuid = "{827a2938-bb14-4d18-b67f-94e9c4f818ba}";
    private const string UnbracedGuid = "827a2938-bb14-4d18-b67f-94e9c4f818ba";

    [Fact]
    public void Constructor_WhenValueIsNullOrWhitespace_PreservesExistingRefusal()
    {
        Assert.Throws<ArgumentNullException>(() => new NetworkAdapterId(null!));
        Assert.Throws<ArgumentException>(() => new NetworkAdapterId(" "));
    }

    [Fact]
    public void Equality_WhenBracedGuidCaseDiffers_TreatsValuesAsSameIdentity()
    {
        NetworkAdapterId uppercase = new(CanonicalGuid);
        NetworkAdapterId lowercase = new(LowercaseBracedGuid);

        Assert.Equal(uppercase, lowercase);
    }

    [Fact]
    public void Equality_WhenGuidFormattingDiffers_TreatsValuesAsSameIdentity()
    {
        NetworkAdapterId braced = new(CanonicalGuid);
        NetworkAdapterId unbraced = new(UnbracedGuid);

        Assert.Equal(braced, unbraced);
    }

    [Fact]
    public void Constructor_WhenGuidIsParseable_ExposesStableCanonicalValueAndString()
    {
        NetworkAdapterId id = new(UnbracedGuid);

        Assert.Equal(CanonicalGuid, id.Value);
        Assert.Equal(CanonicalGuid, id.ToString());
    }

    [Fact]
    public void GetHashCode_WhenGuidFormsAreEquivalent_ReturnsSameValue()
    {
        NetworkAdapterId uppercase = new(CanonicalGuid);
        NetworkAdapterId lowercaseUnbraced = new(UnbracedGuid);

        Assert.Equal(uppercase.GetHashCode(), lowercaseUnbraced.GetHashCode());
    }

    [Fact]
    public void DictionaryLookup_WhenGuidFormsAreEquivalent_FindsExistingIdentity()
    {
        Dictionary<NetworkAdapterId, string> adapters = new()
        {
            [new NetworkAdapterId(CanonicalGuid)] = "target"
        };

        Assert.Equal("target", adapters[new NetworkAdapterId(UnbracedGuid)]);
    }

    [Fact]
    public void Equality_WhenGuidsDiffer_TreatsValuesAsDifferentIdentities()
    {
        NetworkAdapterId first = new(CanonicalGuid);
        NetworkAdapterId second = new("{927A2938-BB14-4D18-B67F-94E9C4F818BA}");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Constructor_WhenValueIsNotGuid_PreservesOriginalCaseSensitiveSemantics()
    {
        NetworkAdapterId original = new("{TEST-ADAPTER}");
        NetworkAdapterId same = new("{TEST-ADAPTER}");
        NetworkAdapterId differentCase = new("{test-adapter}");

        Assert.Equal("{TEST-ADAPTER}", original.Value);
        Assert.Equal(original, same);
        Assert.NotEqual(original, differentCase);
    }
}
