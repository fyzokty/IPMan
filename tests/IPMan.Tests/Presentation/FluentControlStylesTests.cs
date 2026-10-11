using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;

namespace IPMan.Tests.Presentation;

[Collection(WpfTestCollectionDefinition.Name)]
public sealed class FluentControlStylesTests
{
    [Fact]
    public void Templates_LoadWithoutStartingTheApplicationAndFollowThemeReplacement()
    {
        WpfTestRunner.Run(() =>
        {
            ResourceDictionary light = Load("Light");
            ResourceDictionary dark = Load("Dark");
            Assert.Equal(light.Keys.Cast<string>().Order(), dark.Keys.Cast<string>().Order());
            StackPanel host = new();
            host.Resources.MergedDictionaries.Add(light);
            host.Resources.MergedDictionaries.Add(Load("Controls"));
            Button primary = new() { Content = "Test", Style = (Style)host.Resources["PrimaryActionButtonStyle"] };
            host.Children.Add(primary);
            foreach (Control control in new Control[]
            {
                new TextBox { Text = "192.168.1.1" }, new CheckBox { Content = "Test", IsChecked = true },
                new RadioButton { Content = "Test", IsChecked = true }, new ComboBox(),
                new ListBox { Items = { "Test" } }, new Expander { Header = "Test", Content = "Test", IsExpanded = true }
            })
            {
                control.Style = (Style)host.Resources[control.GetType()];
                host.Children.Add(control);
            }

            host.Measure(new Size(800, 1000));
            host.Arrange(new Rect(0, 0, 800, 1000));
            Assert.Equal(Color.FromRgb(15, 108, 189), ((SolidColorBrush)primary.Background).Color);

            host.Resources.MergedDictionaries[0] = dark;
            host.UpdateLayout();

            Assert.Equal(Color.FromRgb(76, 194, 255), ((SolidColorBrush)primary.Background).Color);
            Assert.Equal(Colors.Black, ((SolidColorBrush)primary.Foreground).Color);
        }, "Control style layout timed out.");
    }

    [Fact]
    public void Controls_ShareOneStandardHeightAndKeepTurkishTextInside()
    {
        WpfTestRunner.Run(() =>
        {
            ResourceDictionary controls = Load("Controls");
            double standard = (double)controls["ControlHeight"];
            Assert.Equal(32, standard);
            StackPanel row = new() { Orientation = Orientation.Horizontal };
            row.Resources.MergedDictionaries.Add(Load("Light"));
            row.Resources.MergedDictionaries.Add(controls);
            const string text = "Ğüş İçöĞ Ayrıntılar ve elle ayar →";
            FrameworkElement[] singleLine =
            [
                new Button { Content = text },
                new Button { Content = text, Style = (Style)row.Resources["SecondaryActionButtonStyle"] },
                new Button { Content = text, Style = (Style)row.Resources["PrimaryActionButtonStyle"] },
                new Button { Content = text, Style = (Style)row.Resources["SubtleButtonStyle"] },
                new Button { Content = "★", Style = (Style)row.Resources["SquareButtonStyle"] },
                new Button { Content = "", Style = (Style)row.Resources["IconButtonStyle"] },
                new Button { Content = "Kopyala", Style = (Style)row.Resources["CopyButtonStyle"] },
                new TextBox { Text = "192.168.10.25 Ğİ" },
                new ComboBox(),
                new Border
                {
                    Style = (Style)row.Resources["SegmentedControlStyle"],
                    Child = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new RadioButton { Content = "Kart", Style = (Style)row.Resources["ViewModeSegmentStyle"] },
                            new RadioButton { Content = "Liste", Style = (Style)row.Resources["ViewModeSegmentStyle"] }
                        }
                    }
                }
            ];
            foreach (FrameworkElement element in singleLine)
            {
                row.Children.Add(element);
            }

            row.Measure(new Size(4000, double.PositiveInfinity));
            row.Arrange(new Rect(row.DesiredSize));
            row.UpdateLayout();
            Assert.Equal(standard, row.ActualHeight);
            Assert.All(singleLine, element => Assert.Equal(standard, element.ActualHeight));
            Assert.All(singleLine.OfType<Button>().Where(button => button.Content is not string { Length: > 7 }),
                button => Assert.Equal(standard, button.ActualWidth));
            Assert.All(singleLine.OfType<Button>().Where(button => button.Content is string { Length: > 7 }),
                button => Assert.True(button.ActualWidth > standard));
        }, "Standard control height layout timed out.");
    }

    private static ResourceDictionary Load(string name) =>
        (ResourceDictionary)System.Windows.Application.LoadComponent(
            new Uri($"/IPMan;component/Themes/{name}.xaml", UriKind.Relative));
}
