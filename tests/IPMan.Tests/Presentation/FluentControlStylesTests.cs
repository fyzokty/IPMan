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
                new TabControl { Items = { new TabItem { Header = "Test", Content = "Test" } } },
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

    private static ResourceDictionary Load(string name) =>
        (ResourceDictionary)System.Windows.Application.LoadComponent(
            new Uri($"/IPMan;component/Themes/{name}.xaml", UriKind.Relative));
}
