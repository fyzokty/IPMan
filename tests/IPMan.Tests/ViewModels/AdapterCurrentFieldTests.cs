using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.ViewModels;

public sealed class AdapterCurrentFieldTests
{
    [Theory]
    [InlineData(DraftField.Ipv4Address, "10.0.0.5")]
    [InlineData(DraftField.SubnetMask, "255.255.255.0")]
    [InlineData(DraftField.Gateway, "10.0.0.1")]
    [InlineData(DraftField.PrimaryDns, "1.1.1.1")]
    [InlineData(DraftField.SecondaryDns, "9.9.9.9")]
    public void CopyCurrentField_UsesWindowsSnapshotAndLeavesDirtyDraftUntouched(DraftField field, string expected)
    {
        FakeClipboardService clipboard = new();
        AdapterViewModel adapter = new(TestData.Snapshot(
            ipv4Address: "10.0.0.5", subnetMask: "255.255.255.0", gateway: "10.0.0.1",
            primaryDns: "1.1.1.1", secondaryDns: "9.9.9.9"), clipboard, new StaticIpv4ConfigurationValidator());
        adapter.Draft.Ipv4Address = "10.0.0.99";

        adapter.CopyCurrentFieldCommand.Execute(field);

        Assert.Equal(expected, clipboard.LastCopiedText);
        Assert.Equal("10.0.0.99", adapter.Draft.Ipv4Address);
        Assert.True(adapter.Draft.IsDirty);
    }

    [Fact]
    public void CopyCurrentField_FollowsRefreshAndClearsClipboardForAbsentValue()
    {
        FakeClipboardService clipboard = new();
        AdapterViewModel adapter = new(TestData.Snapshot(gateway: "10.0.0.1"), clipboard, new StaticIpv4ConfigurationValidator());
        adapter.CopyCurrentFieldCommand.Execute(DraftField.Gateway);
        Assert.Equal("10.0.0.1", clipboard.LastCopiedText);

        adapter.Update(TestData.Snapshot(gateway: null));
        adapter.CopyCurrentFieldCommand.Execute(DraftField.Gateway);

        Assert.Equal(string.Empty, clipboard.LastCopiedText);
    }
}
