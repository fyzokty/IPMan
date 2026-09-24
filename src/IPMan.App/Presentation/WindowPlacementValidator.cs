using System.Windows;
using IPMan.Domain.Settings;

namespace IPMan.App.Presentation;

/// <summary>Validates persisted window placement against currently available work areas.</summary>
public sealed class WindowPlacementValidator
{
    /// <summary>Returns a safe placement, centering it on the primary work area when needed.</summary>
    public static AppWindowPlacement Validate(
        AppWindowPlacement? placement,
        IReadOnlyCollection<Rect> workAreas,
        Rect primaryWorkArea,
        double minimumWidth,
        double minimumHeight,
        double defaultWidth,
        double defaultHeight)
    {
        if (!IsUsableArea(primaryWorkArea))
        {
            throw new ArgumentException("Primary work area must have positive dimensions.", nameof(primaryWorkArea));
        }

        bool validSize = placement is not null &&
            IsValidSize(placement.Width, minimumWidth) &&
            IsValidSize(placement.Height, minimumHeight);
        double width = validSize ? placement!.Width!.Value : defaultWidth;
        double height = validSize ? placement!.Height!.Value : defaultHeight;

        Rect bounds = placement is not null && placement.Left is double left && placement.Top is double top
            ? new Rect(left, top, width, height)
            : Rect.Empty;
        bool isInsideWorkArea = !bounds.IsEmpty && workAreas.Any(area =>
            IsUsableArea(area) && area.Contains(bounds));

        if (!isInsideWorkArea)
        {
            bounds = Center(primaryWorkArea, width, height);
        }

        return new AppWindowPlacement(
            bounds.Left,
            bounds.Top,
            width,
            height,
            placement?.IsMaximized ?? false);
    }

    private static bool IsValidSize(double? value, double minimum) =>
        value is double number && double.IsFinite(number) && number >= minimum;

    private static bool IsUsableArea(Rect area) =>
        double.IsFinite(area.Left) && double.IsFinite(area.Top) &&
        double.IsFinite(area.Width) && double.IsFinite(area.Height) &&
        area.Width > 0 && area.Height > 0;

    private static Rect Center(Rect workArea, double width, double height) =>
        new(
            workArea.Left + ((workArea.Width - width) / 2),
            workArea.Top + ((workArea.Height - height) / 2),
            width,
            height);
}
