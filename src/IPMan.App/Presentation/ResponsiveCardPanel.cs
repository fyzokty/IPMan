using System.Windows;
using System.Windows.Controls;
using Size = System.Windows.Size;

namespace IPMan.App.Presentation;

/// <summary>Fits up to three equal-width setting cards without horizontal scrolling.</summary>
public sealed class ResponsiveCardPanel : Panel
{
    private int _columns = 1;
    private double _rowHeight;

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = double.IsFinite(availableSize.Width) ? availableSize.Width : 900;
        _columns = Math.Clamp((int)(width / 300), 1, 3);
        _rowHeight = 0;
        foreach (UIElement child in InternalChildren)
        {
            child.Measure(new Size(width / _columns, double.PositiveInfinity));
            _rowHeight = Math.Max(_rowHeight, child.DesiredSize.Height);
        }
        return new Size(width, Math.Ceiling((double)InternalChildren.Count / _columns) * _rowHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double width = finalSize.Width / _columns;
        for (int index = 0; index < InternalChildren.Count; index++)
        {
            InternalChildren[index].Arrange(new Rect(index % _columns * width,
                index / _columns * _rowHeight, width, _rowHeight));
        }
        return finalSize;
    }
}
