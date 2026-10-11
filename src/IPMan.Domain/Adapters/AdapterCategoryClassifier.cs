using IPMan.Domain.Networking;

namespace IPMan.Domain.Adapters;

/// <summary>Classifies adapters for the user-controlled virtual-adapter filter.</summary>
public static class AdapterCategoryClassifier
{
    /// <summary>Broad adapter category shown by the settings filter.</summary>
    public enum AdapterKind
    {
        Physical,
        Virtual
    }

    /// <summary>Connection subtype used for category display.</summary>
    public enum ConnectionKind
    {
        Wired,
        Wireless,
        Bluetooth,
        Cellular
    }

    /// <summary>Returns the broad category, treating unrecognized adapters as physical.</summary>
    public static AdapterKind GetAdapterKind(NetworkAdapterSnapshot adapter) =>
        IsVirtual(adapter) ? AdapterKind.Virtual : AdapterKind.Physical;

    /// <summary>Returns the connection subtype, treating unrecognized adapters as wired.</summary>
    public static ConnectionKind GetConnectionKind(NetworkAdapterSnapshot adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);

        switch (adapter.Medium)
        {
            case AdapterMedium.Wired:
                return ConnectionKind.Wired;
            case AdapterMedium.Wireless:
                return ConnectionKind.Wireless;
            case AdapterMedium.Bluetooth:
                return ConnectionKind.Bluetooth;
            case AdapterMedium.Cellular:
                return ConnectionKind.Cellular;
        }

        AdapterMedium namedMedium = GetMediumFromName(adapter.Name, adapter.Description);
        if (namedMedium == AdapterMedium.Bluetooth)
        {
            return ConnectionKind.Bluetooth;
        }

        if (namedMedium == AdapterMedium.Cellular)
        {
            return ConnectionKind.Cellular;
        }

        string value = $"{adapter.Name} {adapter.Description}";
        return value.Contains("wireless", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("wi-fi", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("wifi", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("wlan", StringComparison.OrdinalIgnoreCase)
            ? ConnectionKind.Wireless
            : ConnectionKind.Wired;
    }

    /// <summary>Recognizes Bluetooth and cellular name markers when physical medium is unspecified.</summary>
    public static AdapterMedium GetMediumFromName(string name, string description)
    {
        string value = $"{name} {description}";
        if (value.Contains("bluetooth", StringComparison.OrdinalIgnoreCase))
        {
            return AdapterMedium.Bluetooth;
        }

        return value.Contains("mobile broadband", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("cellular", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("wwan", StringComparison.OrdinalIgnoreCase)
            ? AdapterMedium.Cellular
            : AdapterMedium.Unknown;
    }

    /// <summary>Returns whether an adapter is virtual based on stable Windows description markers.</summary>
    public static bool IsVirtual(NetworkAdapterSnapshot adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);

        string value = $"{adapter.Name} {adapter.Description}";
        return value.Contains("virtual", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("vmware", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("hyper-v", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("loopback", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("tunnel", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Returns true unless a virtual adapter is hidden by the user.</summary>
    public static bool IsVisible(NetworkAdapterSnapshot adapter, bool showVirtualAdapters) =>
        showVirtualAdapters || GetAdapterKind(adapter) != AdapterKind.Virtual;
}
