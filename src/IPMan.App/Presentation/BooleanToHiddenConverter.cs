using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IPMan.App.Presentation;

/// <summary>Reserves layout space for conditional labels, validation and busy state.</summary>
public sealed class BooleanToHiddenConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Hidden;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}
