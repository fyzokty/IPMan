using System.Windows;

namespace IPMan.App.Presentation;

internal static class DialogWindowPlacement
{
    internal static void Prepare(Window dialog, Window? owner)
    {
        dialog.Topmost = true;
        dialog.Owner = owner is { IsVisible: true } && owner.WindowState != WindowState.Minimized ? owner : null;
        dialog.WindowStartupLocation = dialog.Owner is null
            ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
    }
}
