using System.ComponentModel;
using System.Reflection;
using IPMan.App.Views;
using IPMan.Domain.Settings;
using Xunit;

namespace IPMan.Tests.Presentation;

public sealed partial class WindowXamlSmokeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MainWindow_ClosingDuringCountdown_AsksCoordinatorBeforeExit(bool approved) =>
        WpfTestRunner.Run(() =>
        {
            using WindowFixture fixture = new("Light");
            fixture.SettingsRepository.SaveAsync(fixture.SettingsRepository.Settings with { CloseToTray = false }, CancellationToken.None)
                .GetAwaiter().GetResult();
            fixture.Safety.IsCountdownActive = true;
            fixture.Safety.ExitApproved = approved;
            fixture.Main.Actions.IsBusy = true;
            MainWindow main = CreateProfileSelectionWindow(fixture, "Light");
            CancelEventArgs closing = CloseEvent(main);
            Assert.True(closing.Cancel);
            Assert.Equal(1, fixture.Safety.ExitRequests);
            Assert.Empty(fixture.Confirmation.Requests);
            if (!approved)
            {
                Assert.True(fixture.Safety.IsCountdownActive);
                Assert.True(CloseEvent(main).Cancel);
                Assert.Equal(2, fixture.Safety.ExitRequests);
            }
            AssertNeverShown(main);
        }, "Main window countdown exit timed out.");

    [Fact]
    public void MainWindow_ClosingWithoutCountdown_UsesDetailsExitConfirmation() =>
        WpfTestRunner.Run(() =>
        {
            using WindowFixture fixture = new("Light");
            fixture.SettingsRepository.SaveAsync(fixture.SettingsRepository.Settings with { CloseToTray = false }, CancellationToken.None)
                .GetAwaiter().GetResult();
            MainWindow main = CreateProfileSelectionWindow(fixture, "Light");
            fixture.Main.OpenDetailsCommand.Execute(null);
            fixture.Main.DetailsPage!.IsStatic = true;
            fixture.Main.DetailsPage.Fields[0].Value = "10.0.0.50";
            fixture.Confirmation.Answer = false;
            Assert.True(CloseEvent(main).Cancel);
            Assert.Equal(0, fixture.Safety.ExitRequests);
            Assert.Single(fixture.Confirmation.Requests);
            AssertNeverShown(main);
        }, "Main window ordinary exit timed out.");

    [Fact]
    public void MainWindow_SettingsChanged_RefreshesCachedSafetyAndViewPreferences() =>
        WpfTestRunner.Run(() =>
        {
            using WindowFixture fixture = new("Light");
            MainWindow main = CreateProfileSelectionWindow(fixture, "Light");
            AppSettings updated = fixture.SettingsRepository.Settings with
            {
                CloseToTray = false,
                PreferredViewMode = PreferredViewMode.List,
                SkipInternetLossConfirmation = true
            };
            fixture.Settings.AcceptPersistedInternetLossConfirmationPreference(updated);
            Assert.Equal(PreferredViewMode.List, fixture.Main.PreferredViewMode);
            fixture.Safety.IsCountdownActive = true;
            Assert.True(CloseEvent(main).Cancel);
            Assert.Equal(1, fixture.Safety.ExitRequests);
            Assert.Empty(fixture.Confirmation.Requests);
            Assert.True(fixture.Settings.SkipInternetLossConfirmation);
            AssertNeverShown(main);
        }, "Main window safety settings cache timed out.");

    private static CancelEventArgs CloseEvent(MainWindow window)
    {
        CancelEventArgs args = new();
        typeof(MainWindow).GetMethod("OnClosing", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)!
            .Invoke(window, [window, args]);
        return args;
    }
}
