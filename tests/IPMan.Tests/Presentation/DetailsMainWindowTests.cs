using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.App.Views;
using IPMan.Domain.Networking;
using Xunit;

namespace IPMan.Tests.Presentation;

public sealed partial class WindowXamlSmokeTests
{
    [Theory]
    [InlineData("Light", false)]
    [InlineData("Dark", false)]
    [InlineData("Light", true)]
    [InlineData("Dark", true)]
    public void MainWindow_DetailsPage_RendersBothModesAndExistingToolCommands(string theme, bool automatic) =>
        WpfTestRunner.Run(() =>
        {
            using WindowFixture fixture = new(theme, RenderProfiles(), RenderAdapters());
            MainWindow main = CreateProfileSelectionWindow(fixture, theme);
            fixture.Main.OpenDetailsCommand.Execute(null);
            AdapterDetailsPageViewModel page = fixture.Main.DetailsPage!;
            page.IsAutomatic = automatic;
            FrameworkElement content = Layout(main);
            AdapterDetailsPage view = Assert.Single(Descendants(content).OfType<AdapterDetailsPage>());
            Assert.Same(page, view.DataContext);
            Assert.Equal(((SolidColorBrush)main.FindResource("PrimaryTextBrush")).Color,
                ((SolidColorBrush)view.Foreground).Color);
            Assert.Single(Descendants(view).OfType<Expander>()).IsExpanded = true;
            Layout(main);
            Assert.Equal(5, Descendants(view).OfType<TextBox>().Count(box => box.DataContext is ManualSettingsFieldViewModel));
            AssertEditableTextCommitsOnEveryChange(view, automatic ? 0 : 5);
            foreach (var command in new System.Windows.Input.ICommand[] { page.BackCommand, page.ApplyCommand,
                page.SaveAsSettingCommand, page.CancelCommand, page.Actions.CopyNetworkInfoCommand,
                page.Actions.PingGatewayCommand, page.Actions.FlushDnsCommand, page.Actions.RenewIpCommand,
                page.Actions.RestoreLastCommand })
                Assert.Single(Descendants(view).OfType<Button>(), button => ReferenceEquals(button.Command, command));
            Assert.DoesNotContain(Descendants(content).OfType<TextBlock>(),
                block => block.IsVisible && block.Text.Contains("taslak", StringComparison.OrdinalIgnoreCase));
            page.ShowFailure(new(Strings.FormatDetailsFailureTitle(Strings.SafeSwitchManualSetting),
                Strings.ApplyMutationFailed, Strings.DetailsUncertain, "Failure code"), page.CreateRequest());
            Layout(main);
            Assert.Contains(Descendants(view).OfType<TextBlock>(), block => block.Text == Strings.DetailsUncertain);
            Assert.Single(Descendants(view).OfType<Button>(), button => ReferenceEquals(button.Command, page.RetryCommand));
            RenderTargetBitmap bitmap = new(1280, 800, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(content);
            byte[] pixels = new byte[1280 * 800 * 4];
            bitmap.CopyPixels(pixels, 5120, 0);
            Assert.Contains(pixels, value => value != 0);
            Assert.True(view.IsMeasureValid);
            Assert.True(view.IsArrangeValid);
            AssertNeverShown(main);
        }, "Details main-window rendering timed out.");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MainWindow_DetailsCountdownClosing_UsesExistingCoordinatorExit(bool approved) =>
        WpfTestRunner.Run(() =>
        {
            using WindowFixture fixture = new("Light");
            fixture.SettingsRepository.SaveAsync(fixture.SettingsRepository.Settings with { CloseToTray = false }, CancellationToken.None)
                .GetAwaiter().GetResult();
            MainWindow main = CreateProfileSelectionWindow(fixture, "Light");
            fixture.Main.OpenDetailsCommand.Execute(null);
            fixture.Main.DetailsPage!.IsStatic = true;
            fixture.Main.Actions.IsBusy = true;
            fixture.Safety.IsCountdownActive = true;
            fixture.Safety.ExitApproved = approved;
            Assert.True(CloseEvent(main).Cancel);
            Assert.Equal(1, fixture.Safety.ExitRequests);
            Assert.Empty(fixture.Confirmation.Requests);
            AssertNeverShown(main);
        }, "Details countdown exit timed out.");
}
