using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Profiles;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.ViewModels;

public sealed partial class ProfileHomeViewModelTests
{
    [Theory]
    [InlineData(NetworkConfigurationMode.Static)]
    [InlineData(NetworkConfigurationMode.Dhcp)]
    public void Editor_Edit_OpensSavedAddressMode(NetworkConfigurationMode mode)
    {
        using HomeFixture fixture = new([TestData.Profile(mode: mode)]);
        fixture.Main.EditSettingCommand.Execute(fixture.Profiles.Profiles[0]);
        Assert.Equal(mode == NetworkConfigurationMode.Dhcp, fixture.Main.ProfileEditor!.IsAutomatic);
        Assert.Equal(Strings.ProfileEditorEditTitle, fixture.Main.ProfileEditor.Title);
        Assert.False(fixture.Main.ProfileEditor.IsDirty);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Editor_SaveOnly_ReturnsHomeAndRefreshesSavedSetting(bool edit)
    {
        using HomeFixture fixture = new([TestData.Profile()]);
        fixture.Profiles.SearchText = "PLC";
        if (edit) fixture.Main.EditSettingCommand.Execute(fixture.Profiles.Profiles[0]);
        else fixture.Main.NewSettingCommand.Execute(null);
        ProfileEditorViewModel editor = fixture.Main.ProfileEditor!;
        editor.Name = "Updated setting";
        editor.IsAutomatic = true;
        await editor.SaveOnlyCommand.ExecuteAsync(null);
        Assert.True(fixture.Main.IsHomeVisible);
        Assert.Null(fixture.Main.ProfileEditor);
        Assert.False(editor.IsDirty);
        Assert.Empty(fixture.Profiles.SearchText);
        var saved = Assert.Single(fixture.Catalog.Saved);
        Assert.Contains(fixture.Main.Settings, setting => setting.StoredSetting?.Id == saved.ProfileId && setting.Name == saved.Name);
        Assert.Equal(edit ? 1 : 2, fixture.Catalog.Profiles.Count);
        Assert.Empty(fixture.Safety.Requests);
        Assert.Empty(fixture.Static.Requests);
        Assert.Empty(fixture.Dhcp.Requests);
        Assert.Empty(fixture.Confirmation.Requests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Editor_SaveAndSwitch_UsesSavedTargetThroughSafetyCoordinator(bool automatic)
    {
        using HomeFixture fixture = new([]);
        fixture.Confirmation.Answer = true;
        fixture.Safety.PendingOutcome = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Main.NewSettingCommand.Execute(null);
        ProfileEditorViewModel editor = fixture.Main.ProfileEditor!;
        editor.Name = "Saved setting";
        editor.IsAutomatic = automatic;
        editor.Ipv4Address = "10.0.0.60";
        await editor.SaveAndSwitchCommand.ExecuteAsync(null);
        Assert.True(fixture.Main.IsHomeVisible);
        Assert.True(fixture.Main.IsBusy);
        Assert.True(fixture.Safety.IsCountdownActive);
        var saved = Assert.Single(fixture.Catalog.Saved);
        SafeSwitchRequest request = Assert.Single(fixture.Safety.Requests);
        Assert.Equal(fixture.Main.SelectedAdapter!.Id, request.Adapter.Id);
        if (automatic)
        {
            Assert.Equal(NetworkConfigurationMode.Dhcp, saved.Mode);
            Assert.Null(request.Target);
            Assert.Equal(Strings.SafeSwitchAutomatic, request.SettingName);
            Assert.Single(fixture.Dhcp.Requests);
            Assert.Empty(fixture.Static.Requests);
        }
        else
        {
            Assert.Equal(saved.ToStaticConfiguration(), request.Target);
            Assert.Equal(saved.Name, request.SettingName);
            Assert.Single(fixture.Static.Requests);
            Assert.Empty(fixture.Dhcp.Requests);
        }
        fixture.Safety.PendingOutcome.SetResult(new(SafeSwitchStatus.Kept));
        await fixture.Main.ProfileSwitchTask;
        Assert.False(fixture.Main.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Editor_DirtyBack_AsksAndHonorsAnswer(bool accepted)
    {
        using HomeFixture fixture = new([]);
        fixture.Main.NewSettingCommand.Execute(null);
        ProfileEditorViewModel editor = fixture.Main.ProfileEditor!;
        editor.Name = "Unsaved";
        fixture.Confirmation.Answer = accepted;
        editor.BackCommand.Execute(null);
        Assert.Equal(accepted, fixture.Main.IsHomeVisible);
        Assert.Equal(!accepted, fixture.Main.IsProfileEditorOpen);
        Assert.Equal(Strings.ProfileEditorDiscardMessage, Assert.Single(fixture.Confirmation.Requests).Message);
        if (!accepted) Assert.Same(editor, fixture.Main.ProfileEditor);
    }

    [Fact]
    public void Editor_RevertedEdits_BackDoesNotAsk()
    {
        using HomeFixture fixture = new([TestData.Profile()]);
        fixture.Main.EditSettingCommand.Execute(fixture.Profiles.Profiles[0]);
        ProfileEditorViewModel editor = fixture.Main.ProfileEditor!;
        string name = editor.Name;
        editor.Name = "Changed";
        editor.IsAutomatic = true;
        editor.Name = name;
        editor.IsStatic = true;
        Assert.False(editor.IsDirty);
        editor.BackCommand.Execute(null);
        Assert.True(fixture.Main.IsHomeVisible);
        Assert.Empty(fixture.Confirmation.Requests);
    }

    [Fact]
    public async Task Editor_BusyAdapter_DisablesSwitchButAllowsSaveOnly()
    {
        using HomeFixture fixture = new([]);
        fixture.Main.NewSettingCommand.Execute(null);
        ProfileEditorViewModel editor = fixture.Main.ProfileEditor!;
        editor.Name = "Saved";
        editor.IsAutomatic = true;
        fixture.Main.Actions.IsBusy = true;
        Assert.False(editor.SaveAndSwitchCommand.CanExecute(null));
        Assert.True(editor.SaveOnlyCommand.CanExecute(null));
        await editor.SaveAsync(true);
        Assert.Empty(fixture.Catalog.Saved);
        fixture.Main.Actions.IsBusy = false;
        Assert.True(editor.SaveAndSwitchCommand.CanExecute(null));
        await editor.SaveOnlyCommand.ExecuteAsync(null);
        Assert.Single(fixture.Catalog.Saved);
    }

    [Fact]
    public async Task Editor_SaveFailure_PreservesDirtyFormAndNeverSwitches()
    {
        using HomeFixture fixture = new([]);
        fixture.Catalog.SaveResult = ProfileSaveResult.Failed(ProfileSaveStatus.IoFailure);
        fixture.Main.NewSettingCommand.Execute(null);
        ProfileEditorViewModel editor = fixture.Main.ProfileEditor!;
        editor.Name = "Unsaved";
        editor.IsAutomatic = true;
        await editor.SaveAndSwitchCommand.ExecuteAsync(null);
        Assert.True(fixture.Main.IsProfileEditorOpen);
        Assert.True(editor.IsDirty);
        Assert.NotEmpty(editor.SaveError);
        Assert.Empty(fixture.Safety.Requests);
    }

    [Fact]
    public async Task Editor_Suggestion_SkipsGatewayCapturedWhenOpened()
    {
        using HomeFixture fixture = new([]);
        fixture.Coordinator.PublishRefresh(TestData.Snapshot(gateway: "192.168.1.50"));
        fixture.Main.NewSettingCommand.Execute(null);
        ProfileEditorViewModel editor = fixture.Main.ProfileEditor!;
        editor.DeviceAddress = "192.168.1.10";
        await editor.SuggestionTask;
        Assert.Equal("192.168.1.51", editor.Suggestion!.Address);
        Assert.False(fixture.Main.IsConnectionSelectionEnabled);
    }

    [Fact]
    public async Task Editor_AdapterDisappears_PreservesFormAndAllowsSaveOnly()
    {
        using HomeFixture fixture = new([]);
        fixture.Main.NewSettingCommand.Execute(null);
        ProfileEditorViewModel editor = fixture.Main.ProfileEditor!;
        editor.Name = "Saved";
        editor.IsAutomatic = true;
        fixture.Coordinator.PublishRefresh();
        Assert.True(fixture.Main.IsContentVisible);
        Assert.False(fixture.Main.IsEmptyStateVisible);
        Assert.Same(editor, fixture.Main.ProfileEditor);
        Assert.False(editor.SaveAndSwitchCommand.CanExecute(null));
        Assert.True(editor.SaveOnlyCommand.CanExecute(null));
        await editor.SaveOnlyCommand.ExecuteAsync(null);
        Assert.True(fixture.Main.IsHomeVisible);
        Assert.True(fixture.Main.IsEmptyStateVisible);
        Assert.Empty(fixture.Safety.Requests);
    }
}

