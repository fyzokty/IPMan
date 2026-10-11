using IPMan.App.Resources;
using IPMan.Application.Profiles;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;

namespace IPMan.App.Presentation;

/// <summary>Builds a bounded menu from snapshots without reading or changing the network.</summary>
public sealed class TrayMenuBuilder
{
    private const int MaximumSettings = 10;
    private readonly ActiveSettingMatcher _matcher = new();

    public TrayMenuModel Build(
        NetworkAdapterSnapshot? snapshot,
        IEnumerable<NetworkProfile> profiles,
        bool canUndo,
        bool isBusy)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        NetworkProfile[] stored = profiles.ToArray();
        string? activeId = snapshot is null ? null : _matcher.Find(snapshot, stored)?.ProfileId;
        bool automaticActive = snapshot?.Mode == NetworkConfigurationMode.Dhcp;
        bool canSwitch = snapshot is not null && !isBusy;
        List<TraySettingItem> settings =
        [
            new(null, Strings.ModeDhcp, automaticActive, canSwitch && !automaticActive)
        ];
        settings.AddRange(stored.OrderByDescending(profile => profile.IsFavorite)
            .ThenBy(profile => profile.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(profile => profile.ProfileId, StringComparer.Ordinal)
            .Take(MaximumSettings - 1)
            .Select(profile => new TraySettingItem(profile.ProfileId, profile.Name,
                profile.ProfileId == activeId, canSwitch && profile.ProfileId != activeId)));

        TrayStatusColor color = snapshot is null || !snapshot.IsConnected
            ? TrayStatusColor.Gray
            : snapshot.InternetConnectivity switch
            {
                InternetConnectivity.Internet => TrayStatusColor.Green,
                InternetConnectivity.LocalNetwork or InternetConnectivity.None => TrayStatusColor.Amber,
                _ => TrayStatusColor.Gray
            };
        string status = snapshot is null ? Strings.ConnectionStatusNoConnection
            : Strings.FormatTrayCurrentStatus(AdapterDisplayFormatter.OrUnavailable(snapshot.Name),
                AdapterDisplayFormatter.FormatConnectionStatus(snapshot));

        return new TrayMenuModel(Strings.ApplicationName, status, color,
            settings.AsReadOnly(), stored.Length >= MaximumSettings, canUndo && !isBusy);
    }
}
