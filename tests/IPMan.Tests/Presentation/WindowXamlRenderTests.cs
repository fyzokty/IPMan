using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.App.Views;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Presentation;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class UiRenderFactAttribute : FactAttribute
{
    public UiRenderFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("UI_RENDER_DIR")))
        {
            Skip = "UI_RENDER_DIR is not set; window PNG rendering is opt-in.";
        }
    }
}

public sealed partial class WindowXamlSmokeTests
{
    [UiRenderFact]
    public void Windows_WhenRenderDirectoryIsSet_RenderBothThemesWithoutShowing()
    {
        string directory = Path.GetFullPath(Environment.GetEnvironmentVariable("UI_RENDER_DIR")!);
        Directory.CreateDirectory(directory);
        foreach (string theme in new[] { "Light", "Dark" })
        {
            WpfTestRunner.Run(() => RenderWindows(theme, directory), $"{theme} window rendering timed out.");
        }
    }

    private static void RenderWindows(string theme, string directory)
    {
        using WindowFixture fixture = new(theme, RenderProfiles(), RenderAdapters());
        void InitializeResources(Window window)
        {
            AttachResources(window, theme);
            FluentMotion.SetIsEnabled(window, false);
        }

        MainWindow main = FakeWindowServices.CreateWindow<MainWindow>(InitializeResources,
            fixture.Main, fixture.SettingsRepository, fixture.Confirmation, fixture.Tray,
            fixture.Settings, fixture.Theme, fixture.Notifications, fixture.Dispatcher,
            new FakeWindowSessionMarker(), (Func<DiagnosticsViewModel>)(() => fixture.Diagnostics));
        main.Width = 1280;
        main.Height = 800;
        Assert.Equal(3, fixture.Main.Adapters.Count);
        ProfileListItemViewModel[] profiles = fixture.Profiles.Groups.SelectMany(group => group.Profiles).ToArray();
        Assert.Equal(5, profiles.Length);
        Assert.Equal(2, profiles.Count(profile => profile.IsFavorite));
        Assert.Single(fixture.Profiles.Problems);
        fixture.Main.SelectAdapter(RenderEthernetId);
        AdapterDraftViewModel draft = Assert.IsType<AdapterViewModel>(fixture.Main.SelectedAdapter).Draft;
        draft.Ipv4Address = "192.168.10.40";
        draft.Gateway = "192.168.10.254";
        Assert.True(draft.IsValid);
        Assert.True(draft.IsIpv4AddressDifferentFromWindows);
        Assert.True(draft.IsGatewayDifferentFromWindows);

        FrameworkElement content = Layout(main);
        FluentMotion.SetIsEnabled(content, false);
        Assert.Single(Descendants(content).OfType<Expander>()).IsExpanded = true;
        string suffix = theme.ToLowerInvariant();
        RenderWindow(main, directory, $"main-{suffix}.png");
        TextBox[] draftFields = Descendants(content).OfType<TextBox>()
            .Where(box => ReferenceEquals(box.DataContext, draft)).ToArray();
        Point[] draftPositions = draftFields.Select(box => Position(box, content)).ToArray();
        Assert.Equal(5, draftFields.Length);
        Assert.All(draftFields, box => Assert.Equal(32, box.ActualHeight));
        Button apply = Assert.Single(Descendants(content).OfType<Button>(),
            button => ReferenceEquals(button.Command, fixture.Main.Actions.ApplyStaticCommand));
        Button restore = Assert.Single(Descendants(content).OfType<Button>(),
            button => ReferenceEquals(button.Command, fixture.Main.Actions.RestoreLastCommand));
        Assert.Equal(Position(apply, content).Y, Position(restore, content).Y);
        ScrollViewer tabScroll = Assert.Single(Descendants(content).OfType<ScrollViewer>(),
            scroll => scroll.Content is Grid grid && grid.ColumnDefinitions.Count == 3);
        Assert.Equal(0, tabScroll.ScrollableHeight);
        File.WriteAllText(Path.Combine(directory, $"layout-{suffix}.txt"),
            $"Main: {content.ActualWidth}x{content.ActualHeight}; tabs y={Position(Assert.Single(Descendants(content).OfType<TabControl>()), content).Y}; " +
            $"draft height={draftFields[0].ActualHeight}; apply y={Position(apply, content).Y}; scroll extent={tabScroll.ScrollableHeight}{Environment.NewLine}");

        draft.Ipv4Address = "192.168.10.999";
        draft.MarkAllFieldsTouched();
        Assert.NotEmpty(draft.Ipv4AddressError);
        Assert.Empty(draft.GatewayError);
        RenderWindow(main, directory, $"main-validation-{suffix}.png");
        Assert.Equal(draftPositions, draftFields.Select(box => Position(box, content)).ToArray());

        draft.Ipv4Address = "192.168.10.40";
        Assert.False(draft.HasErrors);
        fixture.Main.Actions.StatusMessage = Strings.ApplyStaticSuccess;
        fixture.Main.Actions.StatusSeverity = ApplyStatusSeverity.Success;
        Assert.True(fixture.Main.Actions.HasStatusMessage);
        RenderWindow(main, directory, $"main-banner-{suffix}.png");
        Assert.Equal(draftPositions, draftFields.Select(box => Position(box, content)).ToArray());
        Button renew = Assert.Single(Descendants(content).OfType<Button>(),
            button => ReferenceEquals(button.Command, fixture.Main.Actions.RenewIpCommand));
        Border resultBanner = Assert.Single(Descendants(content).OfType<Border>(),
            border => System.Windows.Automation.AutomationProperties.GetLiveSetting(border) ==
                System.Windows.Automation.AutomationLiveSetting.Assertive);
        Assert.True(Position(renew, content).Y + renew.ActualHeight < Position(resultBanner, content).Y,
            "Result banner must not obscure quick actions at 1280x800.");
        File.AppendAllText(Path.Combine(directory, $"layout-{suffix}.txt"),
            $"Main: quick actions bottom={Position(renew, content).Y + renew.ActualHeight}; result banner y={Position(resultBanner, content).Y}{Environment.NewLine}");

        content.Measure(new Size(1100, 700));
        content.Arrange(new Rect(0, 0, 1100, 700));
        content.UpdateLayout();
        Assert.True(tabScroll.ScrollableHeight > 0, "Small windows must scroll the whole tab content.");
        Assert.DoesNotContain(Descendants(tabScroll).OfType<ScrollViewer>(), scroll => scroll.Content is Grid);

        fixture.Main.Actions.ClearStatusCommand.Execute(null);
        fixture.Confirmation.Answers.Enqueue(true);
        fixture.Profiles.SelectedProfile = Assert.Single(profiles, profile => profile.Id == "home");
        Assert.Equal(Strings.ProfileDhcpGuidance, draft.ProfileGuidance);
        RenderWindow(main, directory, $"main-dhcp-profile-{suffix}.png");
        TextBlock profileGuidance = Assert.Single(Descendants(content).OfType<TextBlock>(),
            block => ReferenceEquals(block.DataContext, draft) && block.Text == Strings.ProfileDhcpGuidance);
        Border guidanceBand = Assert.IsType<Border>(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(profileGuidance)));
        Assert.Equal(Visibility.Visible, guidanceBand.Visibility);
        Assert.True(guidanceBand.ActualHeight > 0);
        Assert.Equal(TextWrapping.Wrap, profileGuidance.TextWrapping);
        fixture.Confirmation.Answers.Enqueue(true);
        fixture.Profiles.SelectedProfile = Assert.Single(profiles, profile => profile.Id == "office");
        RenderWindow(main, directory, $"main-static-profile-{suffix}.png");
        Assert.Empty(profileGuidance.Text);
        Assert.Equal(Visibility.Collapsed, guidanceBand.Visibility);
        Assert.Equal(new Size(), guidanceBand.DesiredSize);
        fixture.Main.SelectAdapter(RenderAdapters()[1].Id.Value);
        RenderWindow(main, directory, $"main-dhcp-adapter-{suffix}.png");
        Button dhcp = Assert.Single(Descendants(content).OfType<Button>(),
            button => ReferenceEquals(button.Command, fixture.Main.Actions.ApplyDhcpCommand));
        Button dhcpRenew = Assert.Single(Descendants(content).OfType<Button>(),
            button => ReferenceEquals(button.Command, fixture.Main.Actions.RenewIpCommand));
        Assert.False(dhcp.IsEnabled);
        Assert.True(dhcpRenew.IsEnabled);
        AssertDisabledButtonBrushes(dhcp, main, theme);
        fixture.Coordinator.PublishRefresh();
        fixture.Coordinator.PublishFailure();
        Assert.Empty(fixture.Main.Adapters);
        Assert.True(fixture.Main.IsEmptyStateVisible);
        Assert.True(fixture.Main.HasRefreshError);
        RenderWindow(main, directory, $"main-empty-{suffix}.png");

        SettingsWindow settings = FakeWindowServices.CreateWindow<SettingsWindow>(InitializeResources,
            fixture.Settings, (Func<DiagnosticsViewModel>)(() => fixture.Diagnostics), fixture.Theme);
        RenderWindow(settings, directory, $"settings-{suffix}.png");
        FrameworkElement settingsContent = Assert.IsAssignableFrom<FrameworkElement>(settings.Content);
        Assert.Equal(0, Assert.Single(Descendants(settingsContent).OfType<ScrollViewer>()).ScrollableHeight);
        File.AppendAllText(Path.Combine(directory, $"layout-{suffix}.txt"),
            $"Settings: {settingsContent.ActualWidth}x{settingsContent.ActualHeight}; no scroll{Environment.NewLine}");
        DiagnosticsWindow diagnostics = FakeWindowServices.CreateWindow<DiagnosticsWindow>(InitializeResources, fixture.Diagnostics);
        RenderWindow(diagnostics, directory, $"diagnostics-{suffix}.png");
        FrameworkElement diagnosticsContent = Assert.IsAssignableFrom<FrameworkElement>(diagnostics.Content);
        TextBox firstReportValue = Descendants(diagnosticsContent).OfType<TextBox>().First();
        Point reportPosition = Position(firstReportValue, diagnosticsContent);
        fixture.Diagnostics.CopyCommand.Execute(null);
        Assert.Equal(Strings.DiagnosticsCopySucceeded, fixture.Diagnostics.CopyStatus);
        if (theme == "Dark")
        {
            fixture.Diagnostics.CopyStatus = Strings.DiagnosticsCopyFailed;
            fixture.Diagnostics.CopySucceeded = false;
        }
        RenderWindow(diagnostics, directory, $"diagnostics-{suffix}.png");
        Assert.Equal(reportPosition, Position(firstReportValue, diagnosticsContent));
        PromptWindow confirm = WpfUserConfirmationService.CreatePrompt(
            new UserConfirmationRequest(Strings.ExitConfirmationTitle, Strings.ExitConfirmationMessage,
                ShowDoNotShowAgain: true), out _);
        InitializeResources(confirm);
        RenderWindow(confirm, directory, $"confirm-{suffix}.png");
        PromptWindow rename = WpfUserTextInputService.CreatePrompt(
            Strings.CommandRenameProfile, Strings.CommandRenameProfile, "Ofis Ethernet", out _);
        InitializeResources(rename);
        RenderWindow(rename, directory, $"rename-{suffix}.png");
    }

    private const string RenderEthernetId = "{11111111-2222-3333-4444-555555555555}";

    private static Point Position(FrameworkElement element, FrameworkElement content) =>
        element.TransformToAncestor(content).Transform(new Point());

    private static NetworkAdapterSnapshot[] RenderAdapters() =>
    [
        TestData.Snapshot(id: RenderEthernetId, name: "Ethernet", description: "Intel(R) Ethernet Connection I219-V",
            mode: NetworkConfigurationMode.Static, ipv4Address: "192.168.10.25", gateway: "192.168.10.1",
            primaryDns: "192.168.10.1", secondaryDns: "8.8.8.8"),
        TestData.Snapshot(id: "{22222222-2222-3333-4444-555555555555}", name: "Wi-Fi",
            description: "Intel(R) Wi-Fi 6 AX201 160MHz", macAddress: "00-1A-2B-3C-4D-60",
            linkSpeedBitsPerSecond: 866_000_000, mode: NetworkConfigurationMode.Dhcp,
            ipv4Address: "192.168.20.42", gateway: "192.168.20.1", primaryDns: "192.168.20.1"),
        TestData.Snapshot(id: "{33333333-2222-3333-4444-555555555555}", name: "vEthernet (Default Switch)",
            description: "Hyper-V Virtual Ethernet Adapter", macAddress: "00-15-5D-10-20-30",
            mode: NetworkConfigurationMode.Static, ipv4Address: "172.24.64.1", gateway: null, primaryDns: null)
    ];

    private static NetworkProfile[] RenderProfiles() =>
    [
        TestData.Profile(profileId: "office", name: "Ofis Ethernet", description: "Ofis yerel ağı",
            ipv4Address: "192.168.10.25", gateway: "192.168.10.1", primaryDns: "192.168.10.1", isFavorite: true),
        TestData.Profile(profileId: "plc", name: "PLC Bakım", description: "Üretim hattı kontrolörü",
            ipv4Address: "192.168.10.40", gateway: "192.168.10.254", isFavorite: true),
        TestData.Profile(profileId: "home", name: "Ev Wi-Fi", description: "Otomatik ağ yapılandırması",
            mode: NetworkConfigurationMode.Dhcp, ipv4Address: null, subnetMask: null, gateway: null,
            primaryDns: null, secondaryDns: null, originAdapterName: "Wi-Fi"),
        TestData.Profile(profileId: "lab", name: "Test Laboratuvarı", description: "Cihaz test ağı",
            ipv4Address: "10.10.0.25", gateway: "10.10.0.1", primaryDns: "10.10.0.1"),
        TestData.Profile(profileId: "virtual", name: "Sanal Ağ", description: "Hyper-V geliştirme ağı",
            ipv4Address: "172.24.64.10", gateway: null, primaryDns: null, secondaryDns: null,
            originAdapterName: "vEthernet (Default Switch)")
    ];

    private static void RenderWindow(Window window, string directory, string fileName)
    {
        AssertNeverShown(window);
        FluentMotion.SetIsEnabled(window, false);
        FrameworkElement content = Assert.IsAssignableFrom<FrameworkElement>(window.Content);
        FluentMotion.SetIsEnabled(content, false);
        double width = window.Width;
        double height = window.SizeToContent == SizeToContent.Height ? double.PositiveInfinity : window.Height;
        window.Measure(new Size(width, height));
        // A collapsed, unshown Window does not measure its client content.
        // Keep the Window ancestor so action/banner bindings still resolve.
        content.Measure(new Size(width, height));
        if (double.IsPositiveInfinity(height))
        {
            height = Math.Ceiling(content.DesiredSize.Height);
        }
        Size size = new(width, height);
        window.Arrange(new Rect(size));
        content.Arrange(new Rect(size));
        content.UpdateLayout();
        // SizeToContent can remeasure the Window with native chrome dimensions.
        // PNGs contain only the client visual, including for automatic-height dialogs.
        if (window.SizeToContent == SizeToContent.Height)
        {
            height = Math.Ceiling(content.ActualHeight);
            size = new Size(width, height);
        }
        Assert.True(content.IsMeasureValid);
        Assert.True(content.IsArrangeValid);
        AssertTemplates(content);
        Assert.All(Descendants(content).OfType<FrameworkElement>(), element => Assert.False(FluentMotion.GetIsEnabled(element)));
        Assert.All(Descendants(content).OfType<System.Windows.Controls.Primitives.Popup>(), popup => Assert.False(popup.IsOpen));

        int pixelWidth = (int)Math.Ceiling(width);
        int pixelHeight = (int)Math.Ceiling(height);
        RenderTargetBitmap bitmap = new(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
        // Window chrome needs an HWND; paint only its themed background and client visual.
        DrawingVisual background = new();
        using (DrawingContext drawing = background.RenderOpen())
        {
            drawing.DrawRectangle(window.Background, null, new Rect(size));
        }
        bitmap.Render(background);
        bitmap.Render(content);
        AssertNeverShown(window);

        byte[] pixels = new byte[pixelWidth * pixelHeight * 4];
        bitmap.CopyPixels(pixels, pixelWidth * 4, 0);
        Assert.True(Enumerable.Range(1, pixelWidth * pixelHeight - 1).Any(index =>
            pixels[index * 4] != pixels[0] || pixels[index * 4 + 1] != pixels[1] || pixels[index * 4 + 2] != pixels[2]),
            $"{fileName} contains only the window background.");
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        string path = Path.Combine(directory, fileName);
        using (FileStream output = File.Create(path))
        {
            encoder.Save(output);
        }
        using FileStream saved = File.OpenRead(path);
        BitmapFrame decoded = Assert.Single(new PngBitmapDecoder(saved, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames);
        Assert.Equal(pixelWidth, decoded.PixelWidth);
        Assert.Equal(pixelHeight, decoded.PixelHeight);
    }
}
