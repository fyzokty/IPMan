using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace IPMan.App.Presentation;

/// <summary>Builds a clockwise arc from twelve o'clock with a fixed 100 DIP view box.</summary>
public sealed class CountdownRingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double progress = Math.Clamp(value is double number ? number : 0, 0, 1);
        if (progress <= 0)
        {
            return Geometry.Empty;
        }
        if (progress >= 1)
        {
            return new EllipseGeometry(new Point(50, 50), 42, 42);
        }
        double angle = progress * Math.PI * 2;
        Point end = new(50 + Math.Sin(angle) * 42, 50 - Math.Cos(angle) * 42);
        PathFigure figure = new() { StartPoint = new Point(50, 8) };
        figure.Segments.Add(new ArcSegment(end, new Size(42, 42), 0, progress > 0.5,
            SweepDirection.Clockwise, true));
        return new PathGeometry([figure]);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
