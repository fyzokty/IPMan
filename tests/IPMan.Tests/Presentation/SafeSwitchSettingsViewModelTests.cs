using IPMan.App.ViewModels;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Presentation;

[Collection(WpfTestCollectionDefinition.Name)]
public sealed class SafeSwitchSettingsViewModelTests
{
    [Fact]
    public void ResetInternetLossConfirmation_WhenOptedOut_PersistsFalseAndDisablesButton() =>
        WpfTestRunner.Run(() =>
        {
            FakeSafeSwitchSettingsRepository repository = new();
            repository.Settings = repository.Settings with { SkipInternetLossConfirmation = true };
            repository.Settings = repository.Settings with { PreferredViewMode = IPMan.Domain.Settings.PreferredViewMode.List };
            SettingsViewModel viewModel = new(repository, new FakeUserConfirmationService(), FakeWindowServices.CreateTheme());
            Assert.True(viewModel.ResetInternetLossConfirmationCommand.CanExecute(null));
            viewModel.ResetInternetLossConfirmationCommand.Execute(null);
            Assert.False(repository.Settings.SkipInternetLossConfirmation);
            Assert.Equal(IPMan.Domain.Settings.PreferredViewMode.List, repository.Settings.PreferredViewMode);
            Assert.False(viewModel.ResetInternetLossConfirmationCommand.CanExecute(null));
            Assert.Single(repository.Saved);
        }, "Safety preference reset timed out.");

    [Fact]
    public void Save_AfterTransitionChangesOptOut_PreservesLatestPreference() =>
        WpfTestRunner.Run(() =>
        {
            FakeSafeSwitchSettingsRepository repository = new();
            SettingsViewModel viewModel = new(repository, new FakeUserConfirmationService(), FakeWindowServices.CreateTheme());
            Assert.False(viewModel.ResetInternetLossConfirmationCommand.CanExecute(null));
            repository.Settings = repository.Settings with { SkipInternetLossConfirmation = true };
            viewModel.RememberWindowState = false;
            Assert.True(repository.Settings.SkipInternetLossConfirmation);
            Assert.True(viewModel.ResetInternetLossConfirmationCommand.CanExecute(null));
            viewModel.ResetInternetLossConfirmationCommand.Execute(null);
            Assert.False(repository.Settings.SkipInternetLossConfirmation);
        }, "Safety preference refresh timed out.");

    [Fact]
    public void AcceptPersistedPreference_NotifiesShellWithoutSavingAgain() =>
        WpfTestRunner.Run(() =>
        {
            FakeSafeSwitchSettingsRepository repository = new();
            SettingsViewModel viewModel = new(repository, new FakeUserConfirmationService(), FakeWindowServices.CreateTheme());
            IPMan.Domain.Settings.AppSettings? published = null;
            viewModel.SettingsChanged += (_, settings) => published = settings;
            repository.Settings = repository.Settings with { SkipInternetLossConfirmation = true };
            viewModel.AcceptPersistedInternetLossConfirmationPreference(repository.Settings);
            Assert.Equal(repository.Settings, published);
            Assert.True(viewModel.ResetInternetLossConfirmationCommand.CanExecute(null));
            Assert.Empty(repository.Saved);
        }, "Safety preference notification timed out.");

    [Fact]
    public void ResetDefaults_ResetsBothViewModeAndInternetLossPreference() =>
        WpfTestRunner.Run(() =>
        {
            FakeSafeSwitchSettingsRepository repository = new();
            repository.Settings = repository.Settings with
            {
                PreferredViewMode = IPMan.Domain.Settings.PreferredViewMode.List,
                SkipInternetLossConfirmation = true
            };
            SettingsViewModel viewModel = new(repository, new FakeUserConfirmationService { Answer = true }, FakeWindowServices.CreateTheme());
            viewModel.ResetToDefaultsCommand.Execute(null);
            Assert.Equal(IPMan.Domain.Settings.AppSettings.Default, repository.Settings);
            Assert.False(viewModel.SkipInternetLossConfirmation);
            Assert.False(viewModel.ResetInternetLossConfirmationCommand.CanExecute(null));
        }, "Combined settings reset timed out.");
}
