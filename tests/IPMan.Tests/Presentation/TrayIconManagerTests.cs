using System.Windows.Forms;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Presentation;

[Collection(WpfTestCollectionDefinition.Name)]
public sealed class TrayIconManagerTests
{
    [Fact]
    public void Update_BuildsMenuAndPublishesRequestedIdentities() => WpfTestRunner.Run(() =>
    {
        using TrayIconManager tray = new(new FakeApplicationVersionProvider("1.0"));
        TrayMenuModel model = new TrayMenuBuilder().Build(TestData.Snapshot(mode: NetworkConfigurationMode.Static),
            Enumerable.Range(0, 10).Select(i => TestData.Profile(profileId: $"id-{i}", name: $"Setting {i}")), true, false);
        List<string?> switches = [];
        int undo = 0, more = 0, open = 0, exit = 0;
        tray.SwitchRequested += (_, id) => switches.Add(id);
        tray.UndoRequested += (_, _) => undo++;
        tray.MoreSettingsRequested += (_, _) => more++;
        tray.OpenRequested += (_, _) => open++;
        tray.ExitRequested += (_, _) => exit++;
        tray.Update(model);
        ToolStripMenuItem[] items = tray.Menu.Items.OfType<ToolStripMenuItem>().ToArray();
        Assert.Equal(model.Title, items[0].Text);
        Assert.Equal(model.StatusText, tray.Menu.Items[1].Text);
        Assert.NotNull(tray.Menu.Items[1].Image);
        Assert.False(items[0].Enabled);
        Assert.False(tray.Menu.Items[1].CanSelect);
        Assert.Equal(Strings.TraySwitch, items[1].Text);
        for (int i = 0; i < model.Settings.Count; i++)
        {
            Assert.Equal(model.Settings[i].Name, items[i + 2].Text);
            items[i + 2].PerformClick();
        }

        Assert.Equal(model.Settings.Select(setting => setting.ProfileId), switches);
        items.Single(item => item.Text == Strings.TrayMoreSettings).PerformClick();
        items.Single(item => item.Text == Strings.TrayUndo).PerformClick();
        items.Single(item => item.Text == Strings.TrayOpen).PerformClick();
        items.Single(item => item.Text == Strings.TrayExit).PerformClick();
        Assert.Equal(1, more);
        Assert.Equal(1, undo);
        Assert.Equal(1, open);
        Assert.Equal(1, exit);
    }, "Tray menu test timed out.");

    [Fact]
    public void RefreshMenu_UsesLatestProviderAndDisposesReplacedItems() => WpfTestRunner.Run(() =>
    {
        using TrayIconManager tray = new(new FakeApplicationVersionProvider("1.0"));
        bool busy = false;
        int calls = 0, switches = 0, undos = 0;
        tray.SwitchRequested += (_, _) => switches++;
        tray.UndoRequested += (_, _) => undos++;
        tray.MenuProvider = () =>
        {
            calls++;
            return new TrayMenuBuilder().Build(TestData.Snapshot(), [TestData.Profile()], true, busy);
        };
        tray.RefreshMenu();
        ToolStripItem oldStatus = tray.Menu.Items[1];
        bool disposed = false;
        oldStatus.Disposed += (_, _) => disposed = true;
        Assert.True(((ToolStripMenuItem)tray.Menu.Items[4]).Checked);
        busy = true;
        tray.RefreshMenu();
        Assert.Equal(2, calls);
        Assert.True(disposed);
        Assert.DoesNotContain(tray.Menu.Items.OfType<ToolStripMenuItem>(), item => item.Text == Strings.TrayMoreSettings);
        foreach (ToolStripMenuItem item in tray.Menu.Items.OfType<ToolStripMenuItem>()
            .Where(item => item.Text == Strings.ModeDhcp || item.Text == "PLC" || item.Text == Strings.TrayUndo))
        {
            Assert.False(item.Enabled);
            item.PerformClick();
        }

        Assert.Equal(0, switches);
        Assert.Equal(0, undos);
        Assert.False(tray.AlwaysVisible);
    }, "Tray menu refresh test timed out.");
}
