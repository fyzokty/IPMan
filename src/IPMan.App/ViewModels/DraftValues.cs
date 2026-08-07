using IPMan.Domain.Networking;

namespace IPMan.App.ViewModels;

/// <summary>
/// The five editable values as plain text. Used as the comparison baseline for
/// dirty state; absent snapshot values become empty strings, never placeholders.
/// </summary>
public sealed record DraftValues(
    string Ipv4Address,
    string SubnetMask,
    string Gateway,
    string PrimaryDns,
    string SecondaryDns)
{
    public static DraftValues Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);

    public static DraftValues FromSnapshot(NetworkAdapterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new DraftValues(
            snapshot.Ipv4Address ?? string.Empty,
            snapshot.SubnetMask ?? string.Empty,
            snapshot.Gateway ?? string.Empty,
            snapshot.PrimaryDns ?? string.Empty,
            snapshot.SecondaryDns ?? string.Empty);
    }
}
