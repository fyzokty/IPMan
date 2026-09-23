using System.Collections.ObjectModel;

namespace IPMan.App.ViewModels;

/// <summary>A fixed, ordered display group in the profile panel.</summary>
public sealed class ProfileGroupViewModel
{
    /// <summary>Creates a display group.</summary>
    public ProfileGroupViewModel(string name, IEnumerable<ProfileListItemViewModel> profiles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(profiles);

        Name = name;
        Profiles = new ObservableCollection<ProfileListItemViewModel>(profiles);
    }

    public string Name { get; }

    public ObservableCollection<ProfileListItemViewModel> Profiles { get; }
}
