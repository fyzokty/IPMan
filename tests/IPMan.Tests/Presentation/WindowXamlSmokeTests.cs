using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.Services;
using IPMan.App.ViewModels;
using IPMan.App.Views;
using IPMan.Application.Networking;
using IPMan.Domain.Profiles;
using IPMan.Domain.Settings;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Presentation;

[Collection(WpfTestCollectionDefinition.Name)]
public sealed class WindowXamlSmokeTests
{
    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Windows_WhenNeverShown_LoadBamlAndMaterializeTemplates(string theme) =>
        WpfTestRunner.Run(() => VerifyWindows(theme), $"{theme} window layout timed out.");

    private static void VerifyWindows(string theme)
    {
        using WindowFixture fixture = new(theme);
        void InitializeResources(Window window) => AttachResources(window, theme);
        MainWindow main = FakeWindowServices.CreateWindow<MainWindow>(InitializeResources,
            fixture.Main, fixture.SettingsRepository, fixture.Confirmation, fixture.Tray,
            fixture.Settings, fixture.Theme, fixture.Notifications, fixture.Dispatcher,
            new FakeWindowSessionMarker(), (Func<DiagnosticsViewModel>)(() => fixture.Diagnostics));
        FrameworkElement mainContent = Layout(main);
        Expander problems = Assert.Single(Descendants(mainContent).OfType<Expander>());
        problems.IsExpanded = true;
        Layout(main);

        AdapterViewModel adapter = Assert.Single(fixture.Main.Adapters);
        ProfileListItemViewModel profile = Assert.Single(Assert.Single(fixture.Profiles.Groups).Profiles);
        ProfileProblemListItemViewModel problem = Assert.Single(fixture.Profiles.Problems);
        Assert.Contains(Descendants(mainContent).OfType<TextBlock>(), element =>
            ReferenceEquals(element.DataContext, adapter) && element.Text == adapter.DisplayName);
        Assert.Contains(Descendants(mainContent).OfType<TextBox>(), element =>
            ReferenceEquals(element.DataContext, adapter.Draft));
        Assert.Contains(Descendants(mainContent).OfType<TextBlock>(), element =>
            ReferenceEquals(element.DataContext, profile) && element.Text == profile.Name);
        TextBlock problemText = Assert.Single(Descendants(mainContent).OfType<TextBlock>(),
            element => ReferenceEquals(element.DataContext, problem));
        Assert.Contains(problemText.Inlines.OfType<Run>(), run => run.Text == problem.FileName);
        Assert.Contains(problemText.Inlines.OfType<Run>(), run => run.Text == problem.Reason);

        FluentMotion.SetIsEnabled(mainContent, false);
        TextBox draftBox = Assert.Single(Descendants(mainContent).OfType<TextBox>(), element => element.Name == "Ipv4AddressBox");
        TextBox dnsBox = Assert.Single(Descendants(mainContent).OfType<TextBox>(), element =>
            ReferenceEquals(element.DataContext, adapter.Draft) &&
            System.Windows.Automation.AutomationProperties.GetName(element) == Strings.FieldPrimaryDns);
        Point originalDnsPosition = dnsBox.TransformToAncestor(mainContent).Transform(new Point());
        Size originalFieldSize = draftBox.RenderSize;
        adapter.Draft.Ipv4Address = "invalid";
        adapter.Draft.MarkAllFieldsTouched();
        Assert.NotEmpty(adapter.Draft.Ipv4AddressError);
        fixture.Main.Actions.StatusMessage = string.Join(Environment.NewLine, Enumerable.Repeat("Result", 4));
        fixture.Main.Actions.StatusSeverity = ApplyStatusSeverity.Error;
        fixture.Main.Actions.IsBusy = true;
        Layout(main);
        Assert.Equal(originalFieldSize, draftBox.RenderSize);
        Assert.Equal(originalDnsPosition, dnsBox.TransformToAncestor(mainContent).Transform(new Point()));
        Border errorLine = (Border)draftBox.Template.FindName("FocusLine", draftBox);
        Assert.Equal(((SolidColorBrush)main.Resources["ErrorBorderBrush"]).Color, ((SolidColorBrush)errorLine.Background).Color);
        Assert.Equal(1, errorLine.Opacity);
        fixture.Main.Actions.StatusMessage = string.Empty;
        fixture.Main.Actions.StatusSeverity = ApplyStatusSeverity.None;
        fixture.Main.Actions.IsBusy = false;
        Layout(main);
        Assert.Equal(originalDnsPosition, dnsBox.TransformToAncestor(mainContent).Transform(new Point()));

        // Lay out the popup child directly: opening a Popup would create a native window.
        Popup history = Assert.Single(Descendants(mainContent).OfType<Popup>());
        Assert.False(history.IsOpen);
        FrameworkElement historyContent = Assert.IsAssignableFrom<FrameworkElement>(history.Child);
        historyContent.DataContext = fixture.Main;
        LayoutContent(historyContent);
        OperationNotificationService.OperationNotificationRecord record = Assert.Single(fixture.Main.NotificationHistory);
        TextBlock historyText = Assert.Single(Descendants(historyContent).OfType<TextBlock>(),
            element => ReferenceEquals(element.DataContext, record));
        Assert.Contains(historyText.Inlines.OfType<Run>(), run => run.Text == record.Text);
        AssertTemplates(historyContent);

        SettingsWindow settings = FakeWindowServices.CreateWindow<SettingsWindow>(InitializeResources, fixture.Settings,
            (Func<DiagnosticsViewModel>)(() => fixture.Diagnostics), fixture.Theme);
        Assert.NotEmpty(Descendants(Layout(settings)).OfType<RadioButton>());
        DiagnosticsWindow diagnostics = FakeWindowServices.CreateWindow<DiagnosticsWindow>(InitializeResources, fixture.Diagnostics);
        TextBox[] reportValues = Descendants(Layout(diagnostics)).OfType<TextBox>().ToArray();
        Assert.Equal(fixture.Diagnostics.ReportRows.Count, reportValues.Length);
        Assert.All(reportValues, element => Assert.NotEmpty(element.Text));

        foreach (bool informational in new[] { false, true })
        {
            UserConfirmationRequest request = informational
                ? new(Strings.TrayPreferenceTitle, Strings.TrayPreferenceMessage)
                : new(Strings.ExitConfirmationTitle, Strings.ExitConfirmationMessage, ShowDoNotShowAgain: true);
            PromptWindow confirmation = WpfUserConfirmationService.CreatePrompt(request, out CheckBox option);
            AttachResources(confirmation, theme);
            FrameworkElement content = Layout(confirmation);
            Assert.Same(option, Assert.Single(Descendants(content).OfType<CheckBox>()));
            Assert.Equal(2, Descendants(content).OfType<Button>().Count());
        }

        PromptWindow textInput = WpfUserTextInputService.CreatePrompt(
            Strings.CommandRenameProfile, Strings.CommandRenameProfile, "PLC", out TextBox input);
        AttachResources(textInput, theme);
        Assert.Same(input, Assert.Single(Descendants(Layout(textInput)).OfType<TextBox>()));
        Assert.Equal("PLC", input.Text);
        AssertNeverShown(main);
        AssertNeverShown(settings);
        AssertNeverShown(diagnostics);
    }

    private static FrameworkElement Layout(Window window)
    {
        AssertNeverShown(window);
        Assert.NotNull(window.Content);
        window.Measure(new Size(1280, 800));
        window.Arrange(new Rect(0, 0, 1280, 800));
        // Unshown Window.Visibility is Collapsed, so measure its content explicitly as well.
        FrameworkElement content = Assert.IsAssignableFrom<FrameworkElement>(window.Content);
        LayoutContent(content);
        AssertTemplates(content);
        Assert.Equal(((SolidColorBrush)window.Resources["PrimaryTextBrush"]).Color,
            ((SolidColorBrush)window.Foreground).Color);
        AssertNeverShown(window);
        return content;
    }

    private static void LayoutContent(FrameworkElement content)
    {
        content.Measure(new Size(1280, 800));
        content.Arrange(new Rect(0, 0, 1280, 800));
        content.UpdateLayout();
        Assert.True(content.IsMeasureValid);
        Assert.True(content.IsArrangeValid);
    }

    private static void AssertTemplates(FrameworkElement content)
    {
        Control[] controls = Descendants(content).OfType<Control>()
            .Where(control => control.Visibility == Visibility.Visible &&
                control is Button or TextBox or CheckBox or RadioButton or ComboBox or TabControl or ListBox or Expander)
            .ToArray();
        Assert.NotEmpty(controls);
        foreach (Control control in controls)
        {
            Assert.NotNull(control.Template);
            Assert.True(VisualTreeHelper.GetChildrenCount(control) > 0, $"{control.GetType().Name} template was not applied.");
        }
    }

    private static void AssertNeverShown(Window window)
    {
        Assert.False(window.IsVisible);
        Assert.Equal(IntPtr.Zero, new WindowInteropHelper(window).Handle);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (DependencyObject descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static void AttachResources(Window window, string theme)
    {
        if (window.Resources.MergedDictionaries.Count != 0)
        {
            return;
        }

        foreach (string name in new[] { theme, "Controls" })
        {
            window.Resources.MergedDictionaries.Add((ResourceDictionary)System.Windows.Application.LoadComponent(
                new Uri($"/IPMan;component/Themes/{name}.xaml", UriKind.Relative)));
        }
        window.Resources["BooleanToVisibility"] = new BooleanToVisibilityConverter();
    }

    private sealed class WindowFixture : IDisposable
    {
        private readonly FakeProfileCatalog _catalog = new()
        {
            ProfileItems = [TestData.Profile()],
            ProblemItems = [new NetworkProfileProblem(@"C:\window-smoke\broken.json", ProfileLoadFailureKind.MalformedJson)]
        };

        internal WindowFixture(string theme)
        {
            SettingsRepository = new FakeWindowSettingsRepository(AppSettings.Default with
            {
                Theme = theme == "Dark" ? AppTheme.Dark : AppTheme.Light,
                // Inline error layout must not invoke the tray/modal fallback for a hidden window.
                NotificationMode = NotificationMode.Always
            });
            FakeDiagnosticsSources sources = new();
            Notifications = new OperationNotificationService(SettingsRepository, sources);
            Notifications.Report(Strings.CommandApply, TestData.Snapshot().Id.Value, "Ethernet", true);
            FakeAdapterRefreshCoordinator coordinator = new();
            AdapterActionsViewModel actions = new(new FakeStaticIpv4ApplyService(), new FakeDhcpApplyService(),
                new FakeRecoveryRestoreService(), Confirmation, coordinator);
            FakeWindowProfileDialogs dialogs = new();
            Profiles = new ProfilePanelViewModel(_catalog, dialogs, dialogs, Confirmation, Dispatcher);
            Main = new MainWindowViewModel(coordinator, Dispatcher, new FakeClipboardService(), sources,
                sources, sources, actions, new StaticIpv4ConfigurationValidator(), Profiles, Notifications);
            Main.Initialize();
            coordinator.PublishRefresh(TestData.Snapshot());
            Settings = new SettingsViewModel(SettingsRepository, Confirmation, Theme);
            Diagnostics = new DiagnosticsViewModel(sources, sources, sources, () => Main.Adapters.Count,
                _catalog, sources, new FakeClipboardService());
            Diagnostics.RefreshCommand.Execute(null);
        }

        internal FakeUiDispatcher Dispatcher { get; } = new();
        internal FakeUserConfirmationService Confirmation { get; } = new();
        internal TrayIconManager Tray { get; } = FakeWindowServices.CreateTray();
        internal WindowsThemeService Theme { get; } = FakeWindowServices.CreateTheme();
        internal FakeWindowSettingsRepository SettingsRepository { get; }
        internal OperationNotificationService Notifications { get; }
        internal ProfilePanelViewModel Profiles { get; }
        internal MainWindowViewModel Main { get; }
        internal SettingsViewModel Settings { get; }
        internal DiagnosticsViewModel Diagnostics { get; }

        public void Dispose()
        {
            Main.Dispose();
            Profiles.Dispose();
            _catalog.Dispose();
        }
    }
}
