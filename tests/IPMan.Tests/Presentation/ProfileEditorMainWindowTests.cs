using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.App.Views;
using Xunit;

namespace IPMan.Tests.Presentation;

public sealed partial class WindowXamlSmokeTests
{
    [Theory]
    [InlineData("Light", false)]
    [InlineData("Dark", false)]
    [InlineData("Light", true)]
    [InlineData("Dark", true)]
    public void MainWindow_EditorPage_RendersWithHostResources(string theme, bool automatic) =>
        WpfTestRunner.Run(() =>
        {
            using WindowFixture fixture = new(theme, RenderProfiles(), RenderAdapters());
            MainWindow main = CreateProfileSelectionWindow(fixture, theme);
            fixture.Main.NewSettingCommand.Execute(null);
            ProfileEditorViewModel editor = fixture.Main.ProfileEditor!;
            editor.Name = "New setting";
            editor.IsAutomatic = automatic;
            editor.Ipv4Address = "192.168.10.50";
            FrameworkElement content = Layout(main);
            ProfileEditorView view = Assert.Single(Descendants(content).OfType<ProfileEditorView>());
            Assert.Same(editor, view.DataContext);
            Assert.Equal(Visibility.Visible, view.Visibility);
            Assert.Equal(((SolidColorBrush)main.FindResource("PrimaryTextBrush")).Color,
                ((SolidColorBrush)view.Foreground).Color);
            Assert.False(Assert.IsType<ComboBox>(main.FindName("ConnectionSelector")).IsEnabled);
            Button saveAndSwitch = Assert.Single(Descendants(view).OfType<Button>(),
                button => ReferenceEquals(button.Command, editor.SaveAndSwitchCommand));
            Assert.True(saveAndSwitch.IsEnabled);
            fixture.Main.Actions.IsBusy = true;
            Layout(main);
            Assert.False(saveAndSwitch.IsEnabled);
            Assert.True(Assert.Single(Descendants(view).OfType<Button>(),
                button => ReferenceEquals(button.Command, editor.SaveOnlyCommand)).IsEnabled);
            RenderTargetBitmap bitmap = new(1000, 680, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(content);
            byte[] pixels = new byte[1000 * 680 * 4];
            bitmap.CopyPixels(pixels, 4000, 0);
            Assert.Contains(pixels, value => value != 0);
            Assert.True(view.IsMeasureValid);
            Assert.True(view.IsArrangeValid);
            AssertNeverShown(main);
        }, "Editor main-window rendering timed out.");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MainWindow_DirtyEditorClosing_AsksBeforeTrayPreference(bool hideToTray) =>
        WpfTestRunner.Run(() =>
        {
            using WindowFixture fixture = new("Light");
            fixture.SettingsRepository.SaveAsync(fixture.SettingsRepository.Settings with { CloseToTray = hideToTray }, CancellationToken.None)
                .GetAwaiter().GetResult();
            MainWindow main = CreateProfileSelectionWindow(fixture, "Light");
            fixture.Main.NewSettingCommand.Execute(null);
            fixture.Main.ProfileEditor!.Name = "Unsaved";
            fixture.Confirmation.Answer = false;
            Assert.True(CloseEvent(main).Cancel);
            Assert.Equal(Strings.ProfileEditorDiscardMessage, Assert.Single(fixture.Confirmation.Requests).Message);
            Assert.True(fixture.Main.IsProfileEditorOpen);
            Assert.Equal(0, fixture.Safety.ExitRequests);
            AssertNeverShown(main);
        }, "Dirty editor window close timed out.");
}
