using System.Windows;
using System.Windows.Controls;
using IPMan.App.Resources;
using IPMan.App.Presentation;
using IPMan.App.ViewModels;
using IPMan.App.Views;
using IPMan.Domain.Settings;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Presentation;

public sealed partial class WindowXamlSmokeTests
{
    [Theory]
    [InlineData("Light", false)]
    [InlineData("Dark", false)]
    [InlineData("Light", true)]
    [InlineData("Dark", true)]
    public void MainWindow_WhenEditingAcrossGroups_LoadsOnlyRequestedSetting(string theme, bool useMenu) =>
        WpfTestRunner.Run(() =>
        {
            using WindowFixture fixture = new(theme, RenderProfiles(), RenderAdapters());
            MainWindow main = CreateProfileSelectionWindow(fixture, theme);
            fixture.Main.SelectAdapter(RenderEthernetId);
            FrameworkElement content = Layout(main);
            fixture.Confirmation.Answer = true;
            ProfileListItemViewModel home = fixture.Profiles.Profiles.Single(profile => profile.Id == "home");
            ProfileListItemViewModel office = fixture.Profiles.Profiles.Single(profile => profile.Id == "office");
            Edit(home);
            AdapterDraftViewModel draft = fixture.Main.SelectedAdapter!.Draft;
            Assert.Equal(Strings.ProfileDhcpGuidance, draft.ProfileGuidance);
            draft.Ipv4Address = "192.168.20.99";
            fixture.Main.GoBackCommand.Execute(null);
            Layout(main);
            fixture.Confirmation.Requests.Clear();
            Edit(office);
            Assert.Same(office, fixture.Profiles.SelectedProfile);
            Assert.True(fixture.Main.IsDetailsOpen);
            Assert.Equal(office.Profile.Ipv4Address, draft.Ipv4Address);
            Assert.Equal(Strings.ConfirmProfileLoadTitle, Assert.Single(fixture.Confirmation.Requests).Title);
            AssertNeverShown(main);

            void Edit(ProfileListItemViewModel profile)
            {
                if (useMenu)
                {
                    ResponsiveCardPanel panel = Assert.Single(Descendants(content).OfType<ResponsiveCardPanel>());
                    Button button = Assert.Single(Descendants(panel).OfType<Button>(), item =>
                        item.DataContext is SettingItemViewModel setting && setting.StoredSetting?.Id == profile.Id && item.ContextMenu is not null);
                    ContextMenu menu = button.ContextMenu!;
                    menu.PlacementTarget = button;
                    LayoutContent(menu);
                    MenuItem edit = menu.Items.OfType<MenuItem>().First();
                    Assert.Same(fixture.Main.EditSettingCommand, edit.Command);
                    Assert.Same(profile, edit.CommandParameter);
                    edit.Command.Execute(edit.CommandParameter);
                }
                else fixture.Main.EditSettingCommand.Execute(profile);
                Layout(main);
            }
        }, $"{theme} setting editing timed out.");

    private static MainWindow CreateProfileSelectionWindow(WindowFixture fixture, string theme) =>
        FakeWindowServices.CreateWindow<MainWindow>(window => AttachResources(window, theme),
            fixture.Main, fixture.SettingsRepository, fixture.Confirmation, fixture.Tray,
            fixture.Settings, fixture.Theme, fixture.Notifications, fixture.Dispatcher,
            new FakeWindowSessionMarker(), (Func<DiagnosticsViewModel>)(() => fixture.Diagnostics));

    [Fact]
    public void MainWindow_WhenDirtyEditIsDeclined_PreservesDraftAndHomePage() =>
        WpfTestRunner.Run(() =>
        {
            using WindowFixture fixture = new("Light", RenderProfiles(), RenderAdapters());
            MainWindow main = CreateProfileSelectionWindow(fixture, "Light");
            fixture.Main.SelectAdapter(RenderEthernetId);
            fixture.Main.EditSettingCommand.Execute(fixture.Profiles.Profiles.Single(profile => profile.Id == "home"));
            AdapterDraftViewModel draft = fixture.Main.SelectedAdapter!.Draft;
            draft.Ipv4Address = "192.168.20.99";
            fixture.Main.GoBackCommand.Execute(null);
            fixture.Confirmation.Answer = false;
            fixture.Main.EditSettingCommand.Execute(fixture.Profiles.Profiles.Single(profile => profile.Id == "office"));
            Layout(main);
            Assert.Equal("home", fixture.Profiles.SelectedProfile?.Id);
            Assert.True(fixture.Main.IsHomeVisible);
            Assert.Equal("192.168.20.99", draft.Ipv4Address);
            Assert.Equal(Strings.ConfirmProfileLoadTitle, Assert.Single(fixture.Confirmation.Requests).Title);
            AssertNeverShown(main);
        }, "Cancelled setting edit timed out.");

    [Fact]
    public void MainWindow_WhenSettingsAreFiltered_PreservesAutomaticAndMenuBindings() =>
        WpfTestRunner.Run(() =>
        {
            using WindowFixture fixture = new("Light", RenderProfiles(), RenderAdapters());
            MainWindow main = CreateProfileSelectionWindow(fixture, "Light");
            fixture.Main.ChooseListViewCommand.Execute(null);
            FrameworkElement content = Layout(main);
            Assert.Equal(PreferredViewMode.List, fixture.SettingsRepository.Settings.PreferredViewMode);
            Assert.Equal(1, fixture.SettingsRepository.SaveCount);
            Assert.Null(fixture.Profiles.SelectedProfile);
            Assert.Contains(Descendants(content).OfType<TextBlock>(), block => block.Text == Strings.ProfileGroupFavorites);
            Assert.Contains(Descendants(content).OfType<TextBlock>(), block => block.Text == Strings.ProfileGroupOthers);
            Assert.Equal(6, fixture.Main.Settings.Count);
            fixture.Profiles.SearchText = "Ofis";
            Layout(main);
            Assert.True(fixture.Main.Settings[0].IsBuiltIn);
            SettingItemViewModel setting = Assert.Single(fixture.Main.Settings.Skip(1));
            Assert.Equal("office", setting.StoredSetting!.Id);
            Assert.Null(fixture.Profiles.SelectedProfile);
            Button button = Descendants(content).OfType<Button>().First(item => ReferenceEquals(item.DataContext, setting) && item.ContextMenu is not null);
            ContextMenu menu = button.ContextMenu!;
            menu.PlacementTarget = button;
            LayoutContent(menu);
            Assert.Same(setting, menu.DataContext);
            Assert.Collection(menu.Items.OfType<MenuItem>(),
                item => AssertCommand(item, fixture.Main.EditSettingCommand),
                item => AssertCommand(item, fixture.Profiles.DuplicateCommand),
                item => AssertCommand(item, fixture.Profiles.RenameCommand),
                item => AssertCommand(item, fixture.Profiles.ExportCommand),
                item => AssertCommand(item, fixture.Profiles.DeleteCommand));
            fixture.Profiles.SearchText = "Ev Wi-Fi";
            Layout(main);
            Assert.Equal("home", Assert.Single(fixture.Main.Settings.Skip(1)).StoredSetting!.Id);
            Assert.Empty(fixture.Confirmation.Requests);
            Assert.True(fixture.Main.IsListView);
            AssertNeverShown(main);

            void AssertCommand(MenuItem item, System.Windows.Input.ICommand command)
            {
                Assert.Same(command, item.Command);
                Assert.Same(setting.StoredSetting, item.CommandParameter);
            }
        }, "Filtered settings timed out.");

    [Fact]
    public void MainWindow_LegacyApplyOnSelectionIsObsoleteAndIgnored() =>
        WpfTestRunner.Run(() =>
        {
            using WindowFixture fixture = new("Light", RenderProfiles(), RenderAdapters());
            fixture.SettingsRepository.SaveAsync(new AppSettings(1, AppTheme.Light, true, false), CancellationToken.None).GetAwaiter().GetResult();
            MainWindow main = CreateProfileSelectionWindow(fixture, "Light");
            AdapterDraftViewModel draft = fixture.Main.SelectedAdapter!.Draft;
            string previousAddress = draft.Ipv4Address;
            fixture.Profiles.SelectedProfile = fixture.Profiles.Profiles.Single(profile => profile.Id == "plc");
            Assert.Equal(previousAddress, draft.Ipv4Address);
            Assert.Empty(fixture.Confirmation.Requests);
            Assert.False(fixture.Main.IsBusy);
            Assert.NotNull(typeof(AppSettings).GetProperty("ApplyProfileOnSelection")!.GetCustomAttributes(typeof(ObsoleteAttribute), false).SingleOrDefault());
            Layout(main);
            AssertNeverShown(main);
        }, "Legacy preference compatibility timed out.");

    [Fact]
    public void MainWindow_WhenViewPreferenceWasSaved_RestoresItInNextWindow() =>
        WpfTestRunner.Run(() =>
        {
            using WindowFixture fixture = new("Light", RenderProfiles(), RenderAdapters());
            MainWindow first = CreateProfileSelectionWindow(fixture, "Light");
            fixture.Main.ChooseListViewCommand.Execute(null);
            Assert.Equal(PreferredViewMode.List, fixture.SettingsRepository.Settings.PreferredViewMode);
            fixture.Main.PreferredViewMode = PreferredViewMode.Auto;
            MainWindow second = CreateProfileSelectionWindow(fixture, "Light");
            Assert.True(fixture.Main.IsListView);
            Assert.Equal(1, fixture.SettingsRepository.SaveCount);
            AssertNeverShown(first);
            AssertNeverShown(second);
        }, "View preference restoration timed out.");
}
