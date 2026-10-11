using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.Services;
using IPMan.App.ViewModels;
using IPMan.App.Views;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;
using IPMan.Domain.Settings;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Presentation;

[Collection(WpfTestCollectionDefinition.Name)]
public sealed partial class WindowXamlSmokeTests
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
            new FakeWindowSessionMarker(), (Func<DiagnosticsViewModel>)(() => fixture.Diagnostics), fixture.Safety);
        FrameworkElement mainContent = Layout(main);
        Assert.Equal(900, main.MinWidth);
        Assert.Equal(600, main.MinHeight);
        Assert.Empty(Descendants(mainContent).OfType<TabControl>());
        ComboBox selector = Assert.Single(Descendants(mainContent).OfType<ComboBox>());
        Assert.Same(fixture.Main.SelectedAdapter, selector.SelectedItem);
        Assert.Contains(Descendants(selector).OfType<TextBlock>(), block => block.Text == fixture.Main.SelectedAdapter!.DisplayName);
        Assert.Contains(Descendants(mainContent).OfType<TextBlock>(), element => element.Text == fixture.Main.StatusHeadline);
        SettingItemViewModel automatic = fixture.Main.Settings[0];
        Assert.True(automatic.IsBuiltIn);
        Assert.False(automatic.CanManage);
        Assert.Contains(Descendants(mainContent).OfType<TextBlock>(), element =>
            ReferenceEquals(element.DataContext, automatic) && element.Text == Strings.ModeDhcp);
        SettingItemViewModel profile = Assert.Single(fixture.Main.Settings.Skip(1));
        Assert.Contains(Descendants(mainContent).OfType<TextBlock>(), element =>
            ReferenceEquals(element.DataContext, profile) && element.Text == profile.Name);
        ResponsiveCardPanel cardsPanel = Assert.Single(Descendants(mainContent).OfType<ResponsiveCardPanel>());
        Button menuButton = Assert.Single(Descendants(cardsPanel).OfType<Button>(), button =>
            ReferenceEquals(button.DataContext, profile) && button.ContextMenu is not null);
        menuButton.ContextMenu!.PlacementTarget = menuButton;
        menuButton.ContextMenu.ApplyTemplate();
        Assert.Equal(5, menuButton.ContextMenu.Items.OfType<MenuItem>().Count());
        fixture.Main.Actions.IsBusy = true;
        Layout(main);
        Assert.False(selector.IsEnabled);
        Assert.All(Descendants(mainContent).OfType<Button>().Where(button => button.Command is not null &&
            ReferenceEquals(button.Command, profile.SwitchCommand)), button => Assert.False(button.IsEnabled));
        fixture.Main.Actions.IsBusy = false;

        Expander problems = Assert.Single(Descendants(mainContent).OfType<Expander>());
        problems.IsExpanded = true;
        Layout(main);
        ProfileProblemListItemViewModel problem = Assert.Single(fixture.Profiles.Problems);
        TextBlock problemText = Assert.Single(Descendants(mainContent).OfType<TextBlock>(),
            element => ReferenceEquals(element.DataContext, problem));
        Assert.Contains(problemText.Inlines.OfType<Run>(), run => run.Text == problem.FileName);
        Assert.Contains(problemText.Inlines.OfType<Run>(), run => run.Text == problem.Reason);

        fixture.Main.OpenDetailsCommand.Execute(null);
        Layout(main);
        AdapterViewModel adapter = Assert.Single(fixture.Main.Adapters);
        AdapterDetailsPage detailsView = Assert.Single(Descendants(mainContent).OfType<AdapterDetailsPage>());
        Assert.Same(fixture.Main.DetailsPage, detailsView.DataContext);
        fixture.Main.DetailsPage!.IsStatic = true;
        fixture.Main.DetailsPage.Fields[0].Value = "invalid";
        Layout(main);
        Assert.NotEmpty(fixture.Main.DetailsPage.Fields[0].Error);
        Assert.False(fixture.Main.DetailsPage.ApplyCommand.CanExecute(null));
        fixture.Confirmation.Answer = true;
        fixture.Main.GoBackCommand.Execute(null);
        Assert.False(adapter.Draft.IsDirty);
        Assert.False(fixture.Main.HasUnappliedChanges);
        Assert.Null(fixture.Main.DetailsPage);
        fixture.Confirmation.Requests.Clear();
        Layout(main);

        // Lay out the popup child directly: opening a Popup would create a native window.
        Popup history = Assert.Single(Descendants(mainContent).OfType<Popup>(), popup =>
            BindingOperations.GetBinding(popup, Popup.IsOpenProperty)?.Path.Path == nameof(MainWindowViewModel.IsHistoryOpen));
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
        Assert.Equal(0, fixture.SettingsRepository.SaveCount);
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

        internal WindowFixture(string theme, IReadOnlyList<NetworkProfile>? profiles = null,
            NetworkAdapterSnapshot[]? adapters = null)
        {
            if (profiles is not null)
            {
                _catalog.ProfileItems = profiles;
            }
            SettingsRepository = new FakeWindowSettingsRepository(AppSettings.Default with
            {
                Theme = theme == "Dark" ? AppTheme.Dark : AppTheme.Light,
                ShowVirtualAdapters = true,
                // Inline error layout must not invoke the tray/modal fallback for a hidden window.
                NotificationMode = NotificationMode.Always
            });
            FakeDiagnosticsSources sources = new();
            Notifications = new OperationNotificationService(SettingsRepository, sources);
            Notifications.Report(Strings.DetailsApply, TestData.Snapshot().Id.Value, "Ethernet", true);
            AdapterActionsViewModel actions = new(new FakeStaticIpv4ApplyService(), new FakeDhcpApplyService(),
                new FakeRecoveryRestoreService(), Confirmation, Coordinator, Safety);
            FakeWindowProfileDialogs dialogs = new();
            Profiles = new ProfilePanelViewModel(_catalog, dialogs, dialogs, Confirmation, Dispatcher);
            Main = new MainWindowViewModel(Coordinator, Dispatcher, new FakeClipboardService(), sources,
                sources, sources, actions, new StaticIpv4ConfigurationValidator(), Profiles, Notifications,
                (profile, gateway) => new(_catalog, new StaticIpv4ConfigurationValidator(),
                    new(new SequenceIpv4ConflictProbe()), new ImmediateDelayProvider(), profile, gateway), Confirmation);
            Main.SetShowVirtualAdapters(SettingsRepository.Settings.ShowVirtualAdapters);
            Main.Initialize();
            Coordinator.PublishRefresh(adapters ?? [TestData.Snapshot()]);
            Settings = new SettingsViewModel(SettingsRepository, Confirmation, Theme);
            Diagnostics = new DiagnosticsViewModel(sources, sources, sources, () => Main.Adapters.Count,
                _catalog, sources, new FakeClipboardService());
            Diagnostics.RefreshCommand.Execute(null);
        }

        internal FakeSafetyCountdownCoordinator Safety { get; } = new();
        internal FakeUiDispatcher Dispatcher { get; } = new();
        internal FakeAdapterRefreshCoordinator Coordinator { get; } = new();
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
