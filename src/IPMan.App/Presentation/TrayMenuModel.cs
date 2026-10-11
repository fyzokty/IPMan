namespace IPMan.App.Presentation;

/// <summary>Immutable presentation state for the notification-area menu.</summary>
public sealed record TrayMenuModel(
    string Title,
    string StatusText,
    TrayStatusColor StatusColor,
    IReadOnlyList<TraySettingItem> Settings,
    bool HasMoreSettings,
    bool CanUndo);

public sealed record TraySettingItem(string? ProfileId, string Name, bool IsActive, bool IsEnabled);

public enum TrayStatusColor
{
    Gray,
    Green,
    Amber
}
