using CommunityToolkit.Mvvm.ComponentModel;
using IPMan.App.Presentation;
using IPMan.App.Resources;

namespace IPMan.App.ViewModels;

public sealed partial class InternetLossConfirmationViewModel(SafeSwitchRequest request) : ObservableObject
{
    [ObservableProperty]
    private bool _doNotAskAgain;

    public string Title => Strings.FormatSafeSwitchConfirmationTitle(request.SettingName);
    public string OldAddress => request.Adapter.Ipv4Address ?? Strings.SafeSwitchEmptyValue;
    public string NewAddress => request.Target?.Ipv4Address ?? Strings.SafeSwitchAutomatic;
    public string ConnectionName => request.Adapter.Name;
    public string SubnetMask => request.Target?.SubnetMask ?? Strings.SafeSwitchEmptyValue;
    public string Gateway => request.Target?.Gateway ?? Strings.SafeSwitchEmptyValue;
    public string Dns => string.Join(Strings.SafeSwitchListSeparator,
        new[] { request.Target?.PrimaryDns, request.Target?.SecondaryDns }.Where(value => !string.IsNullOrWhiteSpace(value))) is { Length: > 0 } dns
            ? dns : Strings.SafeSwitchEmptyValue;
}
