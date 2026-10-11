using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.Views;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Presentation;

public sealed partial class WindowXamlSmokeTests
{
    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void DetailsPage_LoadsBothThemesAndKeepsFooterOutsideScroll(string theme) =>
        WpfTestRunner.Run(() => VerifyDetailsPage(theme, null), $"{theme} details layout timed out.");

    [UiRenderFact]
    public void DetailsPage_RendersBothThemesWithoutShowing()
    {
        string directory = Path.GetFullPath(Environment.GetEnvironmentVariable("UI_RENDER_DIR")!);
        Directory.CreateDirectory(directory);
        foreach (string theme in new[] { "Light", "Dark" })
            WpfTestRunner.Run(() => VerifyDetailsPage(theme, directory), $"{theme} details render timed out.");
    }

    private static void RenderDetails(Window host, string directory, string fileName)
    {
        // A hidden Window has no presentation source to invalidate cached visuals between renders.
        FrameworkElement content = Assert.IsAssignableFrom<FrameworkElement>(host.Content);
        foreach (UIElement element in Descendants(content).OfType<UIElement>()) element.InvalidateVisual();
        content.InvalidateVisual();
        content.UpdateLayout();
        RenderWindow(host, directory, fileName);
    }

    private static FrameworkElement LayoutDetails(Window host)
    {
        AssertNeverShown(host);
        Size size = new(host.Width, host.Height);
        host.Measure(size);
        host.Arrange(new Rect(size));
        FrameworkElement content = Assert.IsAssignableFrom<FrameworkElement>(host.Content);
        content.Measure(size);
        content.Arrange(new Rect(size));
        content.UpdateLayout();
        AssertTemplates(content);
        return content;
    }

    private static void VerifyDetailsPage(string theme, string? directory)
    {
        using DetailsPageFixture fixture = new(TestData.Snapshot(mode: NetworkConfigurationMode.Static) with { InternetConnectivity = InternetConnectivity.Internet });
        AdapterDetailsPage page = new() { DataContext = fixture.Page };
        Window host = new() { Width = 900, Height = 600 };
        FluentMotion.SetIsEnabled(host, false);
        AttachResources(host, theme);
        host.Content = page;
        FluentMotion.SetIsEnabled(page, false);
        fixture.Adapter.Draft.Ipv4Address = "192.168.1.60";
        FrameworkElement content = LayoutDetails(host);
        Expander advanced = Assert.Single(Descendants(content).OfType<Expander>());
        Assert.False(advanced.IsExpanded);
        Assert.Equal(Visibility.Collapsed, ((ContentPresenter)advanced.Template.FindName("Body", advanced)).Visibility);
        Assert.Equal(new Size(), ((ContentPresenter)advanced.Template.FindName("Body", advanced)).DesiredSize);
        Assert.Contains(Descendants(content).OfType<TextBlock>(), block => block.Text == Strings.DetailsChanged);
        Button apply = Assert.Single(Descendants(content).OfType<Button>(), button => ReferenceEquals(button.Command, fixture.Page.ApplyCommand));
        Assert.True(apply.IsEnabled);
        Point footer = Position(apply, content);
        if (directory is not null) RenderDetails(host, directory, $"details-core-{theme.ToLowerInvariant()}.png");
        advanced.IsExpanded = true;
        LayoutDetails(host);
        Assert.Equal(5, Descendants(content).OfType<TextBox>().Count(box => box.DataContext is IPMan.App.ViewModels.ManualSettingsFieldViewModel));
        Button ping = Assert.Single(Descendants(content).OfType<Button>(), button => ReferenceEquals(button.Command, fixture.Actions.PingGatewayCommand));
        Assert.True(ToolTipService.GetShowOnDisabled(ping));
        Assert.Equal(fixture.Actions.PingGatewayDisabledReason, ping.ToolTip);
        ScrollViewer scroll = Assert.Single(Descendants(content).OfType<ScrollViewer>(), item => item.Content is StackPanel);
        Assert.True(scroll.ScrollableHeight > 0);
        scroll.ScrollToBottom();
        LayoutDetails(host);
        Assert.Equal(footer, Position(apply, content));
        if (directory is not null) RenderDetails(host, directory, $"details-advanced-{theme.ToLowerInvariant()}.png");
        fixture.Adapter.Draft.Ipv4Address = "invalid";
        LayoutDetails(host);
        Assert.False(apply.IsEnabled);
        TextBox address = Assert.Single(Descendants(content).OfType<TextBox>(), box => ReferenceEquals(box.DataContext, fixture.Page.Fields[0]));
        Assert.Equal(((SolidColorBrush)host.Resources["ErrorBorderBrush"]).Color, ((SolidColorBrush)address.BorderBrush).Color);
        scroll.ScrollToTop();
        LayoutDetails(host);
        if (directory is not null) RenderDetails(host, directory, $"details-invalid-{theme.ToLowerInvariant()}.png");
        fixture.Page.ShowFailure(DetailsFailureMessageFormatter.Describe(new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.VerificationFailed), "Saha PLC", "Ofis")!, fixture.Page.CreateRequest());
        LayoutDetails(host);
        Assert.Contains(Descendants(content).OfType<TextBlock>(), block => block.Text == Strings.DetailsUncertain);
        if (directory is not null) RenderDetails(host, directory, $"details-failure-{theme.ToLowerInvariant()}.png");
        fixture.Page.DismissFailureCommand.Execute(null);
        fixture.Page.IsAutomatic = true;
        LayoutDetails(host);
        Assert.True(address.IsReadOnly);
        Assert.True(apply.IsEnabled);
        Assert.DoesNotContain(Descendants(content).OfType<TextBlock>(), block => block.IsVisible && block.Text.Contains("Taslak", StringComparison.Ordinal));
        if (directory is not null) RenderDetails(host, directory, $"details-automatic-{theme.ToLowerInvariant()}.png");
        AssertNeverShown(host);
    }
}
