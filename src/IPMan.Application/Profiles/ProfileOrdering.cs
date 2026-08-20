using IPMan.Domain.Profiles;

namespace IPMan.Application.Profiles;

/// <summary>Provides the display ordering for network profiles.</summary>
public static class ProfileOrdering
{
    /// <summary>
    /// Orders favorites first and sorts profiles alphabetically by name within each group.
    /// </summary>
    public static IReadOnlyList<NetworkProfile> Order(IEnumerable<NetworkProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        return profiles
            .OrderByDescending(profile => profile.IsFavorite)
            .ThenBy(profile => profile.Name, StringComparer.CurrentCulture)
            .ThenBy(profile => profile.ProfileId, StringComparer.Ordinal)
            .ToArray();
    }
}
