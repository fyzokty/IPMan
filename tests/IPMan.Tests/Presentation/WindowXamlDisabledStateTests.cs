using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Documents;
using System.Windows.Media;
using IPMan.App.Presentation;
using IPMan.App.ViewModels;
using IPMan.App.Views;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Presentation;

public sealed partial class WindowXamlSmokeTests
{
    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void DhcpButton_WhenAdapterBecomesDhcpWithMotionOn_LooksDisabled(string theme) =>
        WpfTestRunner.Run(() => VerifyDhcpButtonDisables(theme), $"{theme} DHCP button state timed out.");

    private static void VerifyDhcpButtonDisables(string theme)
    {
        Assert.True(SystemParameters.ClientAreaAnimation, "Client area animation is off; motion races cannot be reproduced.");
        NetworkAdapterSnapshot staticAdapter = TestData.Snapshot(mode: NetworkConfigurationMode.Static);
        using WindowFixture fixture = new(theme, adapters: [staticAdapter]);
        MainWindow main = FakeWindowServices.CreateWindow<MainWindow>(window => AttachResources(window, theme),
            fixture.Main, fixture.SettingsRepository, fixture.Confirmation, fixture.Tray,
            fixture.Settings, fixture.Theme, fixture.Notifications, fixture.Dispatcher,
            new FakeWindowSessionMarker(), (Func<DiagnosticsViewModel>)(() => fixture.Diagnostics));
        AssertNeverShown(main);

        // MainWindow's own HWND hooks need the real tray/theme services, so move its client tree into a
        // plain never-shown Window: bindings still find a Window ancestor. A hidden HwndSource loads
        // its client tree without invoking MainWindow's native hooks or showing a window.
        FrameworkElement content = Assert.IsAssignableFrom<FrameworkElement>(main.Content);
        main.Content = null;
        Window host = new() { DataContext = main.DataContext, Width = 1280, Height = 800, ShowActivated = false };
        AttachResources(host, theme);
        DisabledAppearanceTests.ShortenMotion(host);
        host.Content = content;
        using HwndSource source = DisabledAppearanceTests.Show(content);
        try
        {
            LayoutContent(content);
            DisabledAppearanceTests.Pump(DisabledAppearanceTests.Settle);
            Button dhcp = Assert.Single(Descendants(content).OfType<Button>(),
                button => ReferenceEquals(button.Command, fixture.Main.Actions.ApplyDhcpCommand));
            Button renew = Assert.Single(Descendants(content).OfType<Button>(),
                button => ReferenceEquals(button.Command, fixture.Main.Actions.RenewIpCommand));
            Assert.True(dhcp.IsLoaded);
            Assert.True(dhcp.IsEnabled);
            Assert.False(renew.IsEnabled);
            byte[] dhcpEnabled = DisabledAppearanceTests.Pixels(dhcp);
            byte[] renewDisabled = DisabledAppearanceTests.Pixels(renew);

            // Apply DHCP: busy, the action finishes, then the refresh reports the adapter as DHCP.
            fixture.Main.Actions.IsBusy = true;
            DisabledAppearanceTests.Pump(5);
            fixture.Main.Actions.IsBusy = false;
            DisabledAppearanceTests.Pump(5);
            fixture.Coordinator.PublishRefresh(TestData.Snapshot(mode: NetworkConfigurationMode.Dhcp));
            DisabledAppearanceTests.Pump(DisabledAppearanceTests.Settle);

            Assert.False(dhcp.IsEnabled);
            Assert.True(renew.IsEnabled);
            Assert.True(DisabledAppearanceTests.Differs(dhcpEnabled, DisabledAppearanceTests.Pixels(dhcp)),
                $"{theme}: the disabled DHCP button still renders like the enabled one.");
            Assert.True(DisabledAppearanceTests.Differs(renewDisabled, DisabledAppearanceTests.Pixels(renew)),
                $"{theme}: the enabled renew button still renders like the disabled one.");
            AssertDisabledButtonBrushes(dhcp, host, theme);
            AssertNeverShown(host);
        }
        finally
        {
            host.Close();
        }
    }

    private static void AssertDisabledButtonBrushes(Button button, Window host, string theme)
    {
        Border chrome = Assert.IsType<Border>(button.Template.FindName("Chrome", button));
        Assert.Equal(((SolidColorBrush)host.FindResource("ControlDisabledBrush")).Color,
            ((SolidColorBrush)chrome.Background).Color);
        Assert.Equal(((SolidColorBrush)host.FindResource("ControlDisabledBorderBrush")).Color,
            ((SolidColorBrush)chrome.BorderBrush).Color);
        Assert.Equal(((SolidColorBrush)host.FindResource("ControlDisabledTextBrush")).Color,
            ((SolidColorBrush)TextElement.GetForeground(chrome)).Color);
        Assert.Equal(1, button.Opacity);
        Assert.False(button.HasAnimatedProperties, $"{theme}: disabled appearance must not animate the control itself.");
    }
}
