using System.Windows;
using System.Windows.Controls;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.App.Views;
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
    public void MainWindow_WhenProfileSelectionCrossesGroups_SelectsOnlyOneItem(string theme, bool selectInView) =>
        WpfTestRunner.Run(() =>
        {
            using WindowFixture fixture = new(theme, RenderProfiles(), RenderAdapters());
            MainWindow main = CreateProfileSelectionWindow(fixture, theme);
            fixture.Main.SelectAdapter(RenderEthernetId);
            FrameworkElement content = Layout(main);
            fixture.Confirmation.Answer = true;
            ProfileListItemViewModel[] profiles = fixture.Profiles.Groups.SelectMany(group => group.Profiles).ToArray();
            ProfileListItemViewModel home = Assert.Single(profiles, profile => profile.Id == "home");
            ProfileListItemViewModel office = Assert.Single(profiles, profile => profile.Id == "office");

            SelectProfile(home);
            AdapterDraftViewModel draft = Assert.IsType<AdapterViewModel>(fixture.Main.SelectedAdapter).Draft;
            draft.Ipv4Address = "192.168.20.99";
            Assert.True(draft.IsDirty);
            fixture.Confirmation.Requests.Clear();
            SelectProfile(office);

            ListBoxItem selected = Assert.Single(Descendants(content).OfType<ListBoxItem>(), item => item.IsSelected);
            Assert.Same(office, selected.DataContext);
            Assert.Same(office, fixture.Profiles.SelectedProfile);
            Assert.Equal(Strings.ConfirmProfileLoadTitle, Assert.Single(fixture.Confirmation.Requests).Title);
            SelectProfile(office);
            Assert.Single(fixture.Confirmation.Requests);
            AssertNeverShown(main);

            void SelectProfile(ProfileListItemViewModel profile)
            {
                if (selectInView)
                {
                    Assert.Single(Descendants(content).OfType<ListBoxItem>(),
                        item => ReferenceEquals(item.DataContext, profile)).IsSelected = true;
                }
                else
                {
                    fixture.Profiles.SelectedProfile = profile;
                }

                Layout(main);
            }
        }, $"{theme} profile selection timed out.");

    private static MainWindow CreateProfileSelectionWindow(WindowFixture fixture, string theme) =>
        FakeWindowServices.CreateWindow<MainWindow>(window => AttachResources(window, theme),
            fixture.Main, fixture.SettingsRepository, fixture.Confirmation, fixture.Tray,
            fixture.Settings, fixture.Theme, fixture.Notifications, fixture.Dispatcher,
            new FakeWindowSessionMarker(), (Func<DiagnosticsViewModel>)(() => fixture.Diagnostics));

    [Fact]
    public void MainWindow_WhenCrossGroupProfileLoadIsDeclined_KeepsPreviousSelection() =>
        WpfTestRunner.Run(() =>
        {
            using WindowFixture fixture = new("Light", RenderProfiles(), RenderAdapters());
            MainWindow main = CreateProfileSelectionWindow(fixture, "Light");
            fixture.Main.SelectAdapter(RenderEthernetId);
            FrameworkElement content = Layout(main);
            fixture.Confirmation.Answer = true;
            fixture.Profiles.SelectedProfile = Assert.Single(fixture.Profiles.Profiles, profile => profile.Id == "home");
            AdapterDraftViewModel draft = Assert.IsType<AdapterViewModel>(fixture.Main.SelectedAdapter).Draft;
            draft.Ipv4Address = "192.168.20.99";
            fixture.Confirmation.Requests.Clear();
            fixture.Confirmation.Answer = false;
            Assert.Single(Descendants(content).OfType<ListBoxItem>(),
                item => item.DataContext is ProfileListItemViewModel { Id: "office" }).IsSelected = true;
            DrainProfileDispatcher();
            Layout(main);

            Assert.Equal("home", fixture.Profiles.SelectedProfile?.Id);
            ListBox list = Assert.Single(Descendants(content).OfType<ListBox>(),
                box => ReferenceEquals(box.DataContext, fixture.Profiles));
            Assert.NotNull(list.GetBindingExpression(ListBox.SelectedItemProperty));
            Assert.Same(fixture.Profiles.SelectedProfile, list.SelectedItem);
            ListBoxItem selected = Assert.Single(Descendants(content).OfType<ListBoxItem>(), item => item.IsSelected);
            Assert.Same(fixture.Profiles.SelectedProfile, selected.DataContext);
            Assert.Equal("192.168.20.99", draft.Ipv4Address);
            Assert.Equal(Strings.ConfirmProfileLoadTitle, Assert.Single(fixture.Confirmation.Requests).Title);
            AssertNeverShown(main);
        }, "Cancelled profile selection timed out.");

    [Fact]
    public void MainWindow_WhenProfilesAreFiltered_PreservesGroupedSelectionAndContextCommands() =>
        WpfTestRunner.Run(() =>
        {
            using WindowFixture fixture = new("Light", RenderProfiles(), RenderAdapters());
            MainWindow main = CreateProfileSelectionWindow(fixture, "Light");
            FrameworkElement content = Layout(main);
            ListBox list = Assert.Single(Descendants(content).OfType<ListBox>(),
                box => ReferenceEquals(box.DataContext, fixture.Profiles));
            Assert.Equal(SelectionMode.Single, list.SelectionMode);
            Assert.False(list.IsSynchronizedWithCurrentItem);
            Assert.Null(fixture.Profiles.SelectedProfile);
            Assert.DoesNotContain(Descendants(list).OfType<ListBoxItem>(), item => item.IsSelected);
            Assert.Empty(fixture.Confirmation.Requests);
            Assert.Equal(5, list.Items.Count);
            Assert.Equal(2, Descendants(list).OfType<GroupItem>().Count());
            Assert.Contains(Descendants(list).OfType<TextBlock>(), block => block.Text == Strings.ProfileGroupFavorites);
            Assert.Contains(Descendants(list).OfType<TextBlock>(), block => block.Text == Strings.ProfileGroupOthers);
            Assert.Contains(Descendants(list).OfType<TextBlock>(), block => block.Text == "\uE735");
            Assert.Contains(Descendants(list).OfType<TextBlock>(), block => block.Text == "\uE734");
            TextBlock name = Assert.Single(Descendants(list).OfType<TextBlock>(),
                block => block.DataContext is ProfileListItemViewModel { Id: "office" } && block.Text == "Ofis Ethernet");
            ContextMenu menu = Assert.IsType<ContextMenu>(name.ContextMenu);
            menu.PlacementTarget = name;
            LayoutContent(menu);
            DrainProfileDispatcher();
            Assert.Same(fixture.Profiles, menu.DataContext);
            Assert.Collection(menu.Items.Cast<MenuItem>(),
                item => AssertCommand(item, fixture.Profiles.RenameCommand),
                item => AssertCommand(item, fixture.Profiles.DuplicateCommand),
                item => AssertCommand(item, fixture.Profiles.ToggleFavoriteCommand),
                item => AssertCommand(item, fixture.Profiles.ExportCommand),
                item => AssertCommand(item, fixture.Profiles.DeleteCommand));

            Assert.Single(Descendants(list).OfType<ListBoxItem>(),
                item => ReferenceEquals(item.DataContext, name.DataContext)).IsSelected = true;
            Assert.NotNull(list.GetBindingExpression(ListBox.SelectedItemProperty));
            fixture.Profiles.SearchText = "Ofis";
            DrainProfileDispatcher();
            Layout(main);
            Assert.Single(list.Items.Cast<ProfileListItemViewModel>());
            Assert.Single(Descendants(list).OfType<GroupItem>());
            Assert.Equal("office", Assert.IsType<ProfileListItemViewModel>(list.SelectedItem).Id);
            Assert.Single(Descendants(list).OfType<ListBoxItem>(), item => item.IsSelected);
            fixture.Profiles.SearchText = "Ev Wi-Fi";
            DrainProfileDispatcher();
            Layout(main);
            Assert.Equal("home", Assert.Single(list.Items.Cast<ProfileListItemViewModel>()).Id);
            Assert.Null(fixture.Profiles.SelectedProfile);
            Assert.DoesNotContain(Descendants(list).OfType<ListBoxItem>(), item => item.IsSelected);
            AssertNeverShown(main);

            void AssertCommand(MenuItem item, System.Windows.Input.ICommand command)
            {
                Assert.Same(command, item.Command);
                Assert.Same(name.DataContext, item.CommandParameter);
            }
        }, "Filtered profile selection timed out.");

    private static void DrainProfileDispatcher()
    {
        System.Windows.Threading.DispatcherFrame frame = new();
        _ = System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background, new Action(() => frame.Continue = false));
        System.Windows.Threading.Dispatcher.PushFrame(frame);
    }
}
