using IPMan.App.ViewModels;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.ViewModels;

public sealed class AdapterDraftViewModelTests
{
    [Fact]
    public void Draft_InitializesFromCurrentSnapshot()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);

        draft.UpdateCurrentValues(TestData.Snapshot(
            ipv4Address: "10.0.0.5",
            subnetMask: "255.255.255.0",
            gateway: "10.0.0.1",
            primaryDns: "1.1.1.1",
            secondaryDns: "9.9.9.9"));

        Assert.Equal("10.0.0.5", draft.Ipv4Address);
        Assert.Equal("255.255.255.0", draft.SubnetMask);
        Assert.Equal("10.0.0.1", draft.Gateway);
        Assert.Equal("1.1.1.1", draft.PrimaryDns);
        Assert.Equal("9.9.9.9", draft.SecondaryDns);
        Assert.False(draft.IsDirty);
    }

    [Fact]
    public void Draft_ForAbsentOptionalValues_UsesEmptyTextNotPlaceholder()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);

        draft.UpdateCurrentValues(TestData.Snapshot(gateway: null, secondaryDns: null));

        Assert.Equal(string.Empty, draft.Gateway);
        Assert.Equal(string.Empty, draft.SecondaryDns);
    }

    [Fact]
    public void EditingAField_MarksTheDraftDirty()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.5"));

        draft.Ipv4Address = "10.0.0.9";

        Assert.True(draft.IsDirty);
    }

    [Fact]
    public void EditingBackToTheLoadedValue_MakesTheDraftCleanAgain()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.5"));

        draft.Ipv4Address = "10.0.0.9";
        draft.Ipv4Address = "10.0.0.5";

        Assert.False(draft.IsDirty);
    }

    [Fact]
    public void CurrentStateRefresh_UpdatesACleanDraft()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.5"));

        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.77"));

        Assert.Equal("10.0.0.77", draft.Ipv4Address);
        Assert.False(draft.IsDirty);
    }

    [Fact]
    public void CurrentStateRefresh_DoesNotOverwriteADirtyDraft()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.5"));

        draft.Ipv4Address = "10.0.0.9";
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.77"));

        Assert.Equal("10.0.0.9", draft.Ipv4Address);
        Assert.True(draft.IsDirty);
    }

    [Fact]
    public void GetCurrentValues_ResetsTheDraftToTheLatestSnapshotAndClearsDirtyState()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.5"));

        draft.Ipv4Address = "10.0.0.9";
        draft.Gateway = "10.0.0.254";
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.77", gateway: "10.0.0.1"));

        draft.GetCurrentValuesCommand.Execute(null);

        Assert.Equal("10.0.0.77", draft.Ipv4Address);
        Assert.Equal("10.0.0.1", draft.Gateway);
        Assert.False(draft.IsDirty);
    }

    [Fact]
    public void CopyField_CopiesExactlyThatFieldsDraftText()
    {
        AdapterDraftViewModel draft = CreateDraft(out FakeClipboardService clipboard);
        draft.UpdateCurrentValues(TestData.Snapshot(
            ipv4Address: "10.0.0.5",
            primaryDns: "1.1.1.1"));

        draft.PrimaryDns = "8.8.8.8";
        draft.CopyFieldCommand.Execute(DraftField.PrimaryDns);

        Assert.Equal("8.8.8.8", clipboard.LastCopiedText);
        Assert.Single(clipboard.CopiedTexts);
    }

    [Theory]
    [InlineData(DraftField.Ipv4Address, "10.0.0.5")]
    [InlineData(DraftField.SubnetMask, "255.255.255.0")]
    [InlineData(DraftField.Gateway, "10.0.0.1")]
    [InlineData(DraftField.PrimaryDns, "1.1.1.1")]
    [InlineData(DraftField.SecondaryDns, "9.9.9.9")]
    public void CopyField_SupportsEveryEditableField(DraftField field, string expected)
    {
        AdapterDraftViewModel draft = CreateDraft(out FakeClipboardService clipboard);
        draft.UpdateCurrentValues(TestData.Snapshot(
            ipv4Address: "10.0.0.5",
            subnetMask: "255.255.255.0",
            gateway: "10.0.0.1",
            primaryDns: "1.1.1.1",
            secondaryDns: "9.9.9.9"));

        draft.CopyFieldCommand.Execute(field);

        Assert.Equal(expected, clipboard.LastCopiedText);
    }

    [Fact]
    public void CopyField_WithEmptyValue_CopiesEmptyTextWithoutFailing()
    {
        AdapterDraftViewModel draft = CreateDraft(out FakeClipboardService clipboard);
        draft.UpdateCurrentValues(TestData.Snapshot(secondaryDns: null));

        draft.CopyFieldCommand.Execute(DraftField.SecondaryDns);

        Assert.Equal(string.Empty, clipboard.LastCopiedText);
    }

    [Fact]
    public void CopyField_DoesNotChangeDraftOrDirtyState()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.5"));

        draft.CopyFieldCommand.Execute(DraftField.Ipv4Address);

        Assert.Equal("10.0.0.5", draft.Ipv4Address);
        Assert.False(draft.IsDirty);
    }

    [Fact]
    public void Draft_NeverModifiesTheDomainSnapshot()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);
        NetworkAdapterSnapshot snapshot = TestData.Snapshot(ipv4Address: "10.0.0.5");
        NetworkAdapterSnapshot expected = snapshot with { };

        draft.UpdateCurrentValues(snapshot);
        draft.Ipv4Address = "10.0.0.9";

        Assert.Equal(expected, snapshot);
        Assert.Equal("10.0.0.5", snapshot.Ipv4Address);
    }

    private static AdapterDraftViewModel CreateDraft(out FakeClipboardService clipboard)
    {
        clipboard = new FakeClipboardService();
        return new AdapterDraftViewModel(clipboard);
    }
}
