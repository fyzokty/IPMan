using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>
/// Result of a completed adapter discovery pass, including the difference
/// against the previously published result.
/// </summary>
public sealed class AdapterRefreshedEventArgs : EventArgs
{
    public AdapterRefreshedEventArgs(
        string reason,
        IReadOnlyList<NetworkAdapterSnapshot> adapters,
        IReadOnlyList<NetworkAdapterSnapshot> added,
        IReadOnlyList<NetworkAdapterSnapshot> removed,
        IReadOnlyList<NetworkAdapterSnapshot> changed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentNullException.ThrowIfNull(adapters);
        ArgumentNullException.ThrowIfNull(added);
        ArgumentNullException.ThrowIfNull(removed);
        ArgumentNullException.ThrowIfNull(changed);

        Reason = reason;
        Adapters = adapters;
        Added = added;
        Removed = removed;
        Changed = changed;
    }

    /// <summary>Technical reason identifier; see <see cref="NetworkChangeReason"/>.</summary>
    public string Reason { get; }

    /// <summary>Full adapter set observed by this pass.</summary>
    public IReadOnlyList<NetworkAdapterSnapshot> Adapters { get; }

    /// <summary>Adapters present now that were absent in the previous pass.</summary>
    public IReadOnlyList<NetworkAdapterSnapshot> Added { get; }

    /// <summary>Adapters present in the previous pass that are absent now.</summary>
    public IReadOnlyList<NetworkAdapterSnapshot> Removed { get; }

    /// <summary>Adapters present in both passes whose observed state differs.</summary>
    public IReadOnlyList<NetworkAdapterSnapshot> Changed { get; }
}
