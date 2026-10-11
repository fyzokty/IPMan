using System.Windows;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using Xunit;

namespace IPMan.Tests.Presentation;

[Collection(WpfTestCollectionDefinition.Name)]
public sealed class DialogWindowPlacementTests
{
    [Theory]
    [InlineData(WindowState.Normal)]
    [InlineData(WindowState.Minimized)]
    public void Prepare_WhenMainWindowIsHiddenDetachesOwnerAndKeepsDialogTopmost(WindowState state) => WpfTestRunner.Run(() =>
    {
        Window visible = new();
        visible.Show();
        Window hidden = new() { WindowState = state };
        Window dialog = new() { Owner = visible };
        DialogWindowPlacement.Prepare(dialog, hidden);
        Assert.False(hidden.IsVisible);
        Assert.Null(dialog.Owner);
        Assert.True(dialog.Topmost);
        Assert.Equal(WindowStartupLocation.CenterScreen, dialog.WindowStartupLocation);
        dialog.Close();
        visible.Close();
        hidden.Close();
    }, "Hidden owner placement test timed out.");

    [Fact]
    public void Prepare_WhenMainWindowIsVisibleAttachesOwnerAndCentersOnOwner() => WpfTestRunner.Run(() =>
    {
        Window visible = new();
        visible.Show();
        Window dialog = new();
        DialogWindowPlacement.Prepare(dialog, visible);
        Assert.True(visible.IsVisible);
        Assert.Same(visible, dialog.Owner);
        Assert.True(dialog.Topmost);
        Assert.Equal(WindowStartupLocation.CenterOwner, dialog.WindowStartupLocation);
        dialog.Close();
        visible.Close();
    }, "Visible owner placement test timed out.");

    [Fact]
    public void RestoreConfirmation_WithoutVisibleMainWindowUsesIndependentTopmostPrompt() => WpfTestRunner.Run(() =>
    {
        var prompt = WpfUserConfirmationService.CreatePrompt(new(Strings.ConfirmRestoreTitle, Strings.FormatConfirmRestoreMessage("Ethernet")), out _);
        Assert.Null(prompt.Owner);
        Assert.True(prompt.Topmost);
        Assert.Equal(WindowStartupLocation.CenterScreen, prompt.WindowStartupLocation);
        prompt.Close();
    }, "Tray restore prompt test timed out.");
}
