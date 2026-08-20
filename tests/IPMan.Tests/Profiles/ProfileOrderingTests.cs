using System.Globalization;
using IPMan.Application.Profiles;
using IPMan.Domain.Profiles;
using Xunit;

namespace IPMan.Tests.Profiles;

public sealed class ProfileOrderingTests
{
    [Fact]
    public void Order_WhenFavoritesAndOtherProfilesExist_PutsFavoritesFirst()
    {
        NetworkProfile[] profiles =
        [
            TestData.Profile(profileId: "other-a", name: "A", isFavorite: false),
            TestData.Profile(profileId: "favorite-z", name: "Z", isFavorite: true),
            TestData.Profile(profileId: "other-b", name: "B", isFavorite: false),
            TestData.Profile(profileId: "favorite-y", name: "Y", isFavorite: true)
        ];

        IReadOnlyList<NetworkProfile> result = ProfileOrdering.Order(profiles);

        Assert.Equal(
            ["favorite-y", "favorite-z", "other-a", "other-b"],
            result.Select(profile => profile.ProfileId));
    }

    [Fact]
    public void Order_WhenNamesDifferWithinGroups_SortsEachGroupAlphabetically()
    {
        NetworkProfile[] profiles =
        [
            TestData.Profile(profileId: "favorite-z", name: "Zulu", isFavorite: true),
            TestData.Profile(profileId: "other-z", name: "Zulu", isFavorite: false),
            TestData.Profile(profileId: "favorite-a", name: "Alpha", isFavorite: true),
            TestData.Profile(profileId: "other-a", name: "Alpha", isFavorite: false)
        ];

        IReadOnlyList<NetworkProfile> result = ProfileOrdering.Order(profiles);

        Assert.Equal(
            ["favorite-a", "favorite-z", "other-a", "other-z"],
            result.Select(profile => profile.ProfileId));
    }

    [Fact]
    public void Order_WhenNamesContainTurkishCharacters_UsesTurkishCultureOrder()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            NetworkProfile[] profiles =
            [
                TestData.Profile(profileId: "h", name: "Hale"),
                TestData.Profile(profileId: "g-breve", name: "Ğaye"),
                TestData.Profile(profileId: "d", name: "Deniz"),
                TestData.Profile(profileId: "c-cedilla", name: "Çağla"),
                TestData.Profile(profileId: "c", name: "Can")
            ];

            IReadOnlyList<NetworkProfile> result = ProfileOrdering.Order(profiles);

            Assert.Equal(
                ["c", "c-cedilla", "d", "g-breve", "h"],
                result.Select(profile => profile.ProfileId));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
