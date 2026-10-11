using CommunityToolkit.Mvvm.Input;
using IPMan.App.Resources;

namespace IPMan.App.ViewModels;

/// <summary>One home-screen setting, including the immutable automatic entry.</summary>
public sealed class SettingItemViewModel
{
    public SettingItemViewModel(MainWindowViewModel owner, ProfileListItemViewModel? storedSetting, bool isActive)
    {
        Owner = owner;
        StoredSetting = storedSetting;
        IsActive = isActive;
        SwitchCommand = new AsyncRelayCommand(
            () => owner.Actions.ApplyProfileCommand.ExecuteAsync(storedSetting?.Profile),
            () => !IsActive && !owner.IsBusy && owner.SelectedAdapter is not null);
    }

    public MainWindowViewModel Owner { get; }
    public ProfileListItemViewModel? StoredSetting { get; }
    public bool IsBuiltIn => StoredSetting is null;
    public bool CanManage => !IsBuiltIn;
    public bool IsActive { get; }
    public bool IsFavorite => StoredSetting?.IsFavorite == true;
    public string FavoriteGlyph => IsFavorite ? "★" : "☆";
    public string Name => StoredSetting?.Name ?? Strings.ModeDhcp;
    public string Summary => StoredSetting?.Summary ?? Strings.AutomaticSummary;
    public string Badge => IsActive ? Strings.SettingCurrentlyUsed : IsBuiltIn ? Strings.AutomaticRecommended : string.Empty;
    public bool HasBadge => Badge.Length > 0;
    public string SwitchLabel => IsActive ? Strings.SettingActive : Strings.SwitchNetwork;
    public string ListSwitchLabel => IsActive ? Strings.SettingInUse : Strings.SwitchNetworkCompact;
    public AsyncRelayCommand SwitchCommand { get; }
}

public sealed record SettingGroupViewModel(string Name, IReadOnlyList<SettingItemViewModel> Settings);
