using IPMan.Domain.Profiles;

namespace IPMan.Application.Profiles;

/// <summary>Matches profile display text and IP values against a search query.</summary>
public static class ProfileSearch
{
    /// <summary>Returns whether a profile matches the supplied query.</summary>
    public static bool Matches(NetworkProfile profile, string? query)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        string normalizedQuery = query.Trim();

        return ContainsDisplayText(profile.Name, normalizedQuery) ||
            ContainsDisplayText(profile.Description, normalizedQuery) ||
            ContainsIpValue(profile.Ipv4Address, normalizedQuery) ||
            ContainsIpValue(profile.SubnetMask, normalizedQuery) ||
            ContainsIpValue(profile.Gateway, normalizedQuery) ||
            ContainsIpValue(profile.PrimaryDns, normalizedQuery) ||
            ContainsIpValue(profile.SecondaryDns, normalizedQuery);
    }

    private static bool ContainsDisplayText(string? value, string query) =>
        value?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true;

    private static bool ContainsIpValue(string? value, string query) =>
        value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
}
