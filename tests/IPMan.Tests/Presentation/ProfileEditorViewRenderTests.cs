using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.App.Views;
using IPMan.Application.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Presentation;

[Collection(WpfTestCollectionDefinition.Name)]
public sealed class ProfileEditorViewRenderTests
{
    [Theory]
    [InlineData("Light", "static")]
    [InlineData("Dark", "static")]
    [InlineData("Light", "invalid")]
    [InlineData("Dark", "invalid")]
    [InlineData("Light", "automatic")]
    [InlineData("Dark", "automatic")]
    [InlineData("Light", "edit")]
    [InlineData("Dark", "edit")]
    public void Editor_LoadsBamlAndRendersWithoutWindowOrNetwork(string theme, string state) =>
        WpfTestRunner.Run(() => Render(theme, state), "Profile editor rendering timed out.");

    private static void Render(string theme, string state)
    {
        SequenceIpv4ConflictProbe probe = new();
        probe.Results.Enqueue(state == "invalid" ? Ipv4ConflictProbeStatus.Unavailable : Ipv4ConflictProbeStatus.NoResponse);
        using ProfileEditorViewModel editor = new(new FakeProfileEditorCatalog(),
            new StaticIpv4ConfigurationValidator(), new(probe), new ImmediateDelayProvider(),
            state == "edit" ? TestData.Profile() : null);
        editor.Name = "Saha PLC";
        editor.DeviceAddress = "192.168.1.10";
        // A network address is invalid as a host but still allows an exact subnet comparison.
        editor.Ipv4Address = state == "invalid" ? "192.168.1.0" : "192.168.1.50";
        editor.SubnetMask = "255.255.255.0";
        editor.Gateway = "10.0.0.1";
        editor.IsAutomatic = state == "automatic";

        // Seed resources before BAML initialization, as in FakeWindowServices; no Application/HWND.
        ProfileEditorView view = (ProfileEditorView)RuntimeHelpers.GetUninitializedObject(typeof(ProfileEditorView));
        typeof(UserControl).GetConstructor(Type.EmptyTypes)!.Invoke(view, BindingFlags.Default, null, [], null);
        foreach (string name in new[] { theme, "Controls" })
        {
            view.Resources.MergedDictionaries.Add((ResourceDictionary)System.Windows.Application.LoadComponent(
                new Uri($"/IPMan;component/Themes/{name}.xaml", UriKind.Relative)));
        }
        typeof(ProfileEditorView).GetConstructor(Type.EmptyTypes)!.Invoke(view, BindingFlags.Default, null, [], null);
        view.DataContext = editor;
        FluentMotion.SetIsEnabled(view, false);
        Layout(view, 680);
        Assert.Equal(editor.Title, Assert.Single(Descendants(view).OfType<TextBlock>(), block => block.Text == editor.Title).Text);
        Assert.Equal(2, Descendants(view).OfType<RadioButton>().Count());
        TextBox address = Assert.IsType<TextBox>(view.FindName("Ipv4AddressInput"));
        Assert.Equal(editor.Ipv4Address, address.Text);
        Assert.Equal(state == "invalid", FluentMotion.GetIsInvalid(address));
        Border underline = Assert.IsType<Border>(address.Template.FindName("Underline", address));
        Assert.Equal(((SolidColorBrush)view.FindResource(state == "invalid" ? "ErrorBorderBrush" : "SecondaryTextBrush")).Color,
            ((SolidColorBrush)underline.Background).Color);
        Assert.All(Descendants(view).OfType<Label>(), label =>
            Assert.Equal(((SolidColorBrush)view.FindResource("PrimaryTextBrush")).Color, ((SolidColorBrush)label.Foreground).Color));
        Button save = Assert.Single(Descendants(view).OfType<Button>(), button => ReferenceEquals(button.Command, editor.SaveOnlyCommand));
        Assert.Equal(state != "invalid", save.IsEnabled);
        Assert.Equal(editor.IsAutomatic, Assert.Single(Descendants(view).OfType<RadioButton>(), button =>
            button.IsChecked == true).Content is StackPanel card && card.Children.OfType<TextBlock>().First().Text == Strings.ProfileEditorAutomatic);
        if (state != "automatic")
        {
            Assert.Equal(Strings.ProfileEditorGatewayWarning, editor.GatewayWarning);
            Expander advanced = Assert.Single(Descendants(view).OfType<Expander>());
            Assert.Equal(((SolidColorBrush)view.FindResource("SurfaceBrush")).Color, ((SolidColorBrush)advanced.Background).Color);
            advanced.IsExpanded = true;
        }
        Layout(view, 1100);
        Assert.NotNull(address.Template);
        Assert.True(view.IsMeasureValid);
        Assert.True(view.IsArrangeValid);
        RenderTargetBitmap bitmap = new(1000, 1100, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(view);
        byte[] pixels = new byte[1000 * 1100 * 4];
        bitmap.CopyPixels(pixels, 4000, 0);
        Assert.Contains(pixels, value => value != 0);
        Assert.Contains(Enumerable.Range(1, 1000 * 1100 - 1), index => pixels[index * 4] != pixels[0]);
        string? directory = Environment.GetEnvironmentVariable("UI_RENDER_DIR");
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using FileStream output = File.Create(Path.Combine(directory, $"profile-editor-{state}-{theme.ToLowerInvariant()}.png"));
            encoder.Save(output);
        }
    }

    private static void Layout(FrameworkElement view, double height)
    {
        view.Measure(new Size(1000, height));
        view.Arrange(new Rect(0, 0, 1000, height));
        view.UpdateLayout();
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (DependencyObject descendant in Descendants(child)) yield return descendant;
        }
    }
}
