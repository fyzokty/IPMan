using IPMan.App.Resources;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;

namespace IPMan.App.ViewModels;

/// <summary>Presents one stored profile in the fixed profile panel.</summary>
public sealed class ProfileListItemViewModel
{
    /// <summary>Creates a profile list item.</summary>
    public ProfileListItemViewModel(NetworkProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Profile = profile;
    }

    /// <summary>The underlying immutable profile.</summary>
    public NetworkProfile Profile { get; }

    public string Id => Profile.ProfileId;

    public string Name => Profile.Name;

    public string Description => Profile.Description ?? string.Empty;

    public bool IsFavorite => Profile.IsFavorite;

    public string GroupName => IsFavorite ? Strings.ProfileGroupFavorites : Strings.ProfileGroupOthers;

    public string Summary => Profile.Mode == NetworkConfigurationMode.Dhcp
        ? Strings.ModeDhcp
        : Profile.Ipv4Address ?? Strings.ValueUnavailable;
}
