using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;

namespace IPMan.Application.Profiles;

/// <summary>Matches saved static settings to Windows values, independently of UI selection.</summary>
public sealed class ActiveSettingMatcher
{
    private readonly StaticIpv4ConfigurationComparer _comparer = new();

    public NetworkProfile? Find(NetworkAdapterSnapshot snapshot, IEnumerable<NetworkProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(profiles);
        if (snapshot.Mode != NetworkConfigurationMode.Static)
        {
            return null;
        }

        // Identical settings resolve consistently, independent of search and favorite ordering.
        return profiles.Where(profile => profile.Mode == NetworkConfigurationMode.Static)
            .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(profile => profile.ProfileId, StringComparer.Ordinal)
            .FirstOrDefault(profile => _comparer.Compare(snapshot, profile.ToStaticConfiguration()).IsEquivalent);
    }
}
