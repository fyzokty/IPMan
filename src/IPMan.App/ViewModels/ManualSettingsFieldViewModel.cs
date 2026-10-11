using CommunityToolkit.Mvvm.ComponentModel;
using IPMan.App.Resources;

namespace IPMan.App.ViewModels;

/// <summary>One editable field with its live baseline and existing touched validation.</summary>
public sealed class ManualSettingsFieldViewModel : ObservableObject
{
    private readonly AdapterDetailsPageViewModel _page;
    public ManualSettingsFieldViewModel(AdapterDetailsPageViewModel page, DraftField field, string label)
    {
        _page = page;
        Field = field;
        Label = label;
    }

    public DraftField Field { get; }
    public string Label { get; }
    public string Value
    {
        get => _page.IsAutomatic ? CurrentValue : _page.Adapter.Draft.GetFieldText(Field);
        set
        {
            AdapterDraftViewModel draft = _page.Adapter.Draft;
            switch (Field)
            {
                case DraftField.Ipv4Address: draft.Ipv4Address = value; break;
                case DraftField.SubnetMask: draft.SubnetMask = value; break;
                case DraftField.Gateway: draft.Gateway = value; break;
                case DraftField.PrimaryDns: draft.PrimaryDns = value; break;
                case DraftField.SecondaryDns: draft.SecondaryDns = value; break;
            }
        }
    }
    public string CurrentValue => Field switch
    {
        DraftField.Ipv4Address => _page.Adapter.Snapshot.Ipv4Address ?? string.Empty,
        DraftField.SubnetMask => _page.Adapter.Snapshot.SubnetMask ?? string.Empty,
        DraftField.Gateway => _page.Adapter.Snapshot.Gateway ?? string.Empty,
        DraftField.PrimaryDns => _page.Adapter.Snapshot.PrimaryDns ?? string.Empty,
        DraftField.SecondaryDns => _page.Adapter.Snapshot.SecondaryDns ?? string.Empty,
        _ => string.Empty
    };
    public string CurrentText => Strings.FormatDetailsCurrent(
        CurrentValue.Length == 0 ? Strings.DetailsEmpty : CurrentValue);
    public bool IsChanged => !_page.IsAutomatic && Value != CurrentValue;
    public bool IsReadOnly => _page.IsAutomatic || _page.IsBusy;
    private string VisibleError => _page.IsAutomatic ? string.Empty : Field switch
    {
        DraftField.Ipv4Address => _page.Adapter.Draft.Ipv4AddressError,
        DraftField.SubnetMask => _page.Adapter.Draft.SubnetMaskError,
        DraftField.Gateway => _page.Adapter.Draft.GatewayError,
        DraftField.PrimaryDns => _page.Adapter.Draft.PrimaryDnsError,
        DraftField.SecondaryDns => _page.Adapter.Draft.SecondaryDnsError,
        _ => string.Empty
    };
    public string Error => VisibleError.Length == 0 ? string.Empty : string.Concat(
        VisibleError.Replace(Field switch
        {
            DraftField.Ipv4Address => Strings.FieldIpv4Address,
            DraftField.SubnetMask => Strings.FieldSubnetMask,
            DraftField.Gateway => Strings.FieldGateway,
            DraftField.PrimaryDns => Strings.FieldPrimaryDns,
            DraftField.SecondaryDns => Strings.FieldSecondaryDns,
            _ => Label
        }, Label, StringComparison.Ordinal), " ", Field == DraftField.SubnetMask ? Strings.DetailsMaskExample : Strings.DetailsValidationExample);
    public bool HasError => Error.Length > 0;
    internal void Refresh()
    {
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(CurrentValue));
        OnPropertyChanged(nameof(CurrentText));
        OnPropertyChanged(nameof(IsChanged));
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(Error));
        OnPropertyChanged(nameof(HasError));
    }
}
