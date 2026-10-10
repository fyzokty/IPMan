using IPMan.Domain.Adapters;
using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>The shared adapter list policy, including retention of a selected tab.</summary>
public static class AdapterVisibilityFilter
{
    public static IReadOnlyList<NetworkAdapterSnapshot> SelectVisible(
        IReadOnlyList<NetworkAdapterSnapshot> adapters,
        bool showVirtualAdapters,
        NetworkAdapterId? selectedAdapterId = null)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        return adapters
            .Where(adapter => AdapterCategoryClassifier.IsVisible(adapter, showVirtualAdapters) ||
                adapter.Id == selectedAdapterId)
            .ToArray();
    }
}
