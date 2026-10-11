using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using IPMan.App.Presentation;
using Xunit;

namespace IPMan.Tests.Presentation;

/// <summary>
/// Disabled controls must look disabled after real-time motion settles, whatever order the
/// enable changes and other motion arrive in. The reference is the same control disabled with
/// motion switched off, so the assertion follows the theme instead of a hard-coded design.
/// </summary>
[Collection(WpfTestCollectionDefinition.Name)]
public sealed class DisabledAppearanceTests
{
    internal const int Settle = 120;
    private const int Gap = 5;

    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void DisabledControls_AfterAnimatedEnableSequences_SettleOnTheDisabledLook(string theme)
    {
        WpfTestRunner.Run(() =>
        {
            Assert.True(SystemParameters.ClientAreaAnimation, "Client area animation is off; motion races cannot be reproduced.");
            Grid host = Host(theme);
            using HwndSource source = Show(host);
            List<string> failures = [];
            var cases = Controls(host).Select(item =>
            {
                Control reference = Add(host, item.Create());
                FluentMotion.SetIsEnabled(reference, false);
                reference.IsEnabled = false;
                Control unloaded = item.Create();
                unloaded.IsEnabled = false;
                return (item.Name, Reference: reference, Enabled: Add(host, item.Create()),
                    Single: Add(host, item.Create()), Flicker: Add(host, item.Create()),
                    Busy: Add(host, item.Create()), Unloaded: unloaded);
            }).ToArray();
            Pump(Settle);
            foreach (var item in cases)
            {
                Assert.All(new[] { item.Reference, item.Enabled, item.Single, item.Flicker, item.Busy },
                    control => Assert.True(control.IsLoaded));
                item.Single.IsEnabled = false;
                item.Flicker.IsEnabled = false;
                FluentMotion.SetBusy(item.Busy, true);
                SetPressed(item.Busy, true);
            }

            // IsBusy disables, the action ends and re-enables, then the DHCP snapshot disables again.
            Pump(Gap);
            foreach (var item in cases)
            {
                item.Flicker.IsEnabled = true;
                item.Busy.IsEnabled = false;
            }

            Pump(Gap);
            foreach (var item in cases)
            {
                item.Flicker.IsEnabled = false;
                FluentMotion.SetBusy(item.Busy, false);
                SetPressed(item.Busy, false);
                Add(host, item.Unloaded);
            }

            Pump(Settle);
            foreach (var item in cases)
            {
                byte[] expected = Pixels(item.Reference);
                Assert.True(Differs(expected, Pixels(item.Enabled)),
                    $"{theme} {item.Name}: the disabled look must differ from the enabled look.");
                foreach ((string scenario, Control control) in new[]
                    { ("single", item.Single), ("flicker", item.Flicker), ("busy/pressed", item.Busy), ("unloaded", item.Unloaded) })
                {
                    Assert.False(control.IsEnabled);
                    if (Differs(expected, Pixels(control)))
                    {
                        failures.Add($"{item.Name} ({scenario}, opacity {control.Opacity}, base {control.GetAnimationBaseValue(UIElement.OpacityProperty)}, expected {item.Reference.Opacity})");
                    }
                }
            }

            Assert.True(failures.Count == 0, $"{theme}: settled on a frame that differs from the disabled look: {string.Join(", ", failures)}");
        }, $"{theme} disabled appearance timed out.");
    }

    private static IEnumerable<(string Name, Func<Control> Create)> Controls(Grid host)
    {
        Style Style(object key) => (Style)host.Resources[key];
        yield return ("primary button", () => new Button { Content = "Uygula", Style = Style("PrimaryActionButtonStyle") });
        yield return ("secondary button", () => new Button { Content = "Otomatik Al (DHCP)", Style = Style("SecondaryActionButtonStyle") });
        yield return ("quick action button", () => new Button
        {
            Content = "IP Yenile", FontSize = 12, HorizontalContentAlignment = HorizontalAlignment.Left,
            Width = 160, Style = Style("SecondaryActionButtonStyle")
        });
        yield return ("subtle button", () => new Button { Content = "Mevcut değerleri al", Style = Style("SubtleButtonStyle") });
        yield return ("icon button", () => new Button
        {
            Content = new TextBlock { Text = "", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets") },
            Style = Style("IconButtonStyle")
        });
        yield return ("copy button", () => new Button { Content = "Kopyala", Style = Style("CopyButtonStyle") });
        yield return ("text box", () => new TextBox { Text = "192.168.1.10", Width = 200, Style = Style(typeof(TextBox)) });
        yield return ("check box", () => new CheckBox { Content = "Seçenek", IsChecked = true, Style = Style(typeof(CheckBox)) });
        yield return ("combo box", () => new ComboBox { Items = { "Ethernet" }, SelectedIndex = 0, Width = 200, Style = Style(typeof(ComboBox)) });
        yield return ("radio button", () => new RadioButton { Content = "Açık", IsChecked = true, Style = Style(typeof(RadioButton)) });
    }

    private static Control Add(Grid host, Control control)
    {
        control.HorizontalAlignment = HorizontalAlignment.Left;
        control.VerticalAlignment = VerticalAlignment.Top;
        host.Children.Add(control);
        return control;
    }

    private static void SetPressed(Control control, bool pressed)
    {
        if (control.Template?.FindName("Chrome", control) is FrameworkElement chrome)
        {
            FluentMotion.SetPressed(chrome, pressed);
        }
    }

    private static Grid Host(string theme)
    {
        Grid host = new() { Width = 400, Height = 120 };
        host.Resources.MergedDictionaries.Add(Load(theme));
        host.Resources.MergedDictionaries.Add(Load("Controls"));
        ShortenMotion(host);
        return host;
    }

    internal static void ShortenMotion(FrameworkElement host)
    {
        foreach (string key in new[] { "Motion.Fast", "Motion.Normal", "Motion.Slow", "Motion.Busy" })
        {
            host.Resources[key] = TimeSpan.FromMilliseconds(40);
        }
    }

    private static ResourceDictionary Load(string name) => (ResourceDictionary)System.Windows.Application.LoadComponent(
        new Uri($"/IPMan;component/Themes/{name}.xaml", UriKind.Relative));

    /// <summary>Connects the tree to a never-shown HWND so elements load and motion clocks tick.</summary>
    internal static HwndSource Show(Visual root) =>
        new(new HwndSourceParameters("IPMan.Tests") { Width = 800, Height = 600, WindowStyle = 0 }) { RootVisual = root };

    /// <summary>Runs the dispatcher, including render and animation ticks, for real elapsed time.</summary>
    internal static void Pump(int milliseconds)
    {
        Stopwatch clock = Stopwatch.StartNew();
        do
        {
            DispatcherFrame frame = new();
            DispatcherTimer timer = new(TimeSpan.FromMilliseconds(1), DispatcherPriority.Background,
                (_, _) => frame.Continue = false, Dispatcher.CurrentDispatcher);
            Dispatcher.PushFrame(frame);
            timer.Stop();
        }
        while (clock.ElapsedMilliseconds < milliseconds);
    }

    internal static byte[] Pixels(FrameworkElement element)
    {
        RenderTargetBitmap bitmap = new((int)Math.Ceiling(element.ActualWidth), (int)Math.Ceiling(element.ActualHeight),
            96, 96, PixelFormats.Pbgra32);
        // A VisualBrush drops the element's offset inside its parent, so only the element itself is captured.
        DrawingVisual capture = new();
        using (DrawingContext drawing = capture.RenderOpen())
        {
            drawing.DrawRectangle(new VisualBrush(element) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top },
                null, new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
        }

        bitmap.Render(capture);
        byte[] pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        return pixels;
    }

    /// <summary>True when more than 2% of the pixels differ visibly; tolerates sub-pixel rounding noise.</summary>
    internal static bool Differs(byte[] expected, byte[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        int changed = 0;
        for (int index = 0; index < expected.Length; index += 4)
        {
            int delta = 0;
            for (int channel = 0; channel < 4; channel++)
            {
                delta = Math.Max(delta, Math.Abs(expected[index + channel] - actual[index + channel]));
            }

            changed += delta > 8 ? 1 : 0;
        }

        return changed > expected.Length / 4 / 50;
    }
}
