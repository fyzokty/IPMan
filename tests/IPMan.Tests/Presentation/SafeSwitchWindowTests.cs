using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.App.Views;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Presentation;

public sealed partial class WindowXamlSmokeTests
{
    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void SafeSwitchWindows_WhenNeverShown_LoadBothThemesAndBindCountdown(string theme) =>
        WpfTestRunner.Run(() => VerifySafeSwitchWindows(theme, null), "Safety window layout timed out.");

    [UiRenderFact]
    public void SafeSwitchWindows_WhenRenderDirectoryIsSet_RenderBothThemesWithoutShowing()
    {
        string directory = Path.GetFullPath(Environment.GetEnvironmentVariable("UI_RENDER_DIR")!);
        Directory.CreateDirectory(directory);
        foreach (string theme in new[] { "Light", "Dark" })
        {
            WpfTestRunner.Run(() => VerifySafeSwitchWindows(theme, directory), "Safety window rendering timed out.");
        }
    }

    private static void VerifySafeSwitchWindows(string theme, string? directory)
    {
        SafeSwitchRequest request = new(TestData.Snapshot(name: "Kablolu (Ethernet)", ipv4Address: "192.168.10.25"),
            "Saha PLC", "Ofis", new("192.168.0.50", "255.255.255.0", null, "192.168.0.1", "8.8.8.8"));
        void Resources(Window window)
        {
            AttachResources(window, theme);
            FluentMotion.SetIsEnabled(window, false);
        }
        InternetLossConfirmationViewModel confirmationViewModel = new(request);
        InternetLossConfirmationWindow confirmation = FakeWindowServices.CreateWindow<InternetLossConfirmationWindow>(Resources, confirmationViewModel);
        FrameworkElement confirmationContent = Layout(confirmation);
        Assert.Equal(480, confirmation.Width);
        Assert.True(confirmation.Topmost);
        Assert.Equal(WindowStartupLocation.CenterOwner, confirmation.WindowStartupLocation);
        Expander details = Assert.Single(Descendants(confirmationContent).OfType<Expander>());
        Assert.False(details.IsExpanded);
        Assert.Equal(Visibility.Collapsed, Assert.IsType<ContentPresenter>(details.Template.FindName("Body", details)).Visibility);
        CheckBox optOut = Assert.Single(Descendants(confirmationContent).OfType<CheckBox>());
        optOut.IsChecked = true;
        Assert.True(confirmationViewModel.DoNotAskAgain);
        Assert.Contains(Descendants(confirmationContent).OfType<TextBlock>(), block => block.Text == "192.168.10.25");
        Assert.Contains(Descendants(confirmationContent).OfType<TextBlock>(), block => block.Text == "192.168.0.50");
        details.IsExpanded = true;
        Layout(confirmation);
        Assert.Contains(Descendants(confirmationContent).OfType<TextBlock>(), block => block.Text == "255.255.255.0");
        Assert.Contains(Descendants(confirmationContent).OfType<TextBlock>(), block => block.Text == "192.168.0.1, 8.8.8.8");
        details.IsExpanded = false;

        FakeClock clock = new(DateTimeOffset.UnixEpoch);
        FakeTimerService timer = new(clock);
        using SafetyCountdownViewModel countdownViewModel = new(request, clock, timer);
        SafetyCountdownWindow countdown = FakeWindowServices.CreateWindow<SafetyCountdownWindow>(Resources, countdownViewModel);
        FrameworkElement countdownContent = Layout(countdown);
        Assert.Equal(480, countdown.Width);
        Assert.True(countdown.Topmost);
        Button keep = Assert.Single(Descendants(countdownContent).OfType<Button>(), button => button.IsDefault);
        Assert.Equal(Strings.SafeSwitchKeep, keep.Content);
        Assert.Same(countdownViewModel.KeepCommand, keep.Command);
        System.Windows.Shapes.Path ring = Assert.Single(Descendants(countdownContent).OfType<System.Windows.Shapes.Path>());
        Assert.IsType<EllipseGeometry>(ring.Data);
        countdownViewModel.Start();
        timer.Advance(TimeSpan.FromSeconds(3));
        Layout(countdown);
        Assert.Equal(12, countdownViewModel.RemainingSeconds);
        Assert.IsType<PathGeometry>(ring.Data);
        Assert.Contains(Descendants(countdownContent).OfType<TextBlock>(), block => block.Text == "12");

        SafeSwitchExitWindow exit = FakeWindowServices.CreateWindow<SafeSwitchExitWindow>(Resources);
        FrameworkElement exitContent = Layout(exit);
        Assert.Equal(3, Descendants(exitContent).OfType<Button>().Count());
        Assert.Equal(SafeSwitchExitDecision.Cancel, exit.Decision);

        if (directory is not null)
        {
            string suffix = theme.ToLowerInvariant();
            RenderWindow(confirmation, directory, $"safe-switch-confirmation-{suffix}.png");
            details.IsExpanded = true;
            RenderWindow(confirmation, directory, $"safe-switch-details-{suffix}.png");
            RenderWindow(countdown, directory, $"safe-switch-countdown-{suffix}.png");
            RenderWindow(exit, directory, $"safe-switch-exit-{suffix}.png");
        }
        AssertNeverShown(confirmation);
        AssertNeverShown(countdown);
        AssertNeverShown(exit);
    }
}
