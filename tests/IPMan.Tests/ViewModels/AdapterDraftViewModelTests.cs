using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;
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
    public void CurrentStateRefresh_RetainsVisibleFieldErrorOnDirtyDraft()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.5"));
        draft.Ipv4Address = "invalid";
        string visibleError = draft.Ipv4AddressError;

        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.77"));

        Assert.True(draft.IsDirty);
        Assert.Equal("invalid", draft.Ipv4Address);
        Assert.NotEmpty(visibleError);
        Assert.Equal(visibleError, draft.Ipv4AddressError);
        Assert.True(draft.HasErrors);
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

    [Fact]
    public void UntouchedEmptyDraft_IsInvalidWithoutVisibleErrors()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);

        Assert.False(draft.IsValid);
        Assert.False(draft.HasErrors);
        Assert.Empty(draft.Ipv4AddressError);
        Assert.Empty(draft.SubnetMaskError);
    }

    [Fact]
    public void InvalidIpv4Edit_ShowsOnlyTheIpv4AddressError()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);

        draft.Ipv4Address = "invalid";

        Assert.NotEmpty(draft.Ipv4AddressError);
        Assert.Empty(draft.SubnetMaskError);
        Assert.Empty(draft.GatewayError);
        Assert.Empty(draft.PrimaryDnsError);
        Assert.Empty(draft.SecondaryDnsError);
    }

    [Fact]
    public void MarkAllFieldsTouched_ShowsRequiredFieldErrors()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);

        draft.MarkAllFieldsTouched();

        Assert.True(draft.HasErrors);
        Assert.NotEmpty(draft.Ipv4AddressError);
        Assert.NotEmpty(draft.SubnetMaskError);
    }

    [Fact]
    public void ValidValues_ExposeNormalizedConfiguration()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);

        draft.Ipv4Address = " 192.168.20.25 ";
        draft.SubnetMask = " 255.255.255.0 ";
        draft.Gateway = " 192.168.20.1 ";

        Assert.True(draft.IsValid);
        Assert.NotNull(draft.ValidatedConfiguration);
        Assert.Equal("192.168.20.25", draft.ValidatedConfiguration.Ipv4Address);
        Assert.Equal("255.255.255.0", draft.ValidatedConfiguration.SubnetMask);
        Assert.Equal("192.168.20.1", draft.ValidatedConfiguration.Gateway);
    }

    [Fact]
    public void MarkApplied_ClearsDirtyStateAndAllowsTheNextRefreshToSynchronize()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "192.168.1.50"));
        draft.Ipv4Address = "192.168.1.60";

        draft.MarkApplied();
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "192.168.1.61"));

        Assert.False(draft.IsDirty);
        Assert.Equal("192.168.1.61", draft.Ipv4Address);
        Assert.False(draft.HasErrors);
    }

    [Fact]
    public void LoadProfile_WhenStaticProfileSelected_PopulatesDraftAndMarksOnlyDifferentFields()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);
        draft.UpdateCurrentValues(TestData.Snapshot(
            ipv4Address: "192.168.1.50",
            subnetMask: "255.255.255.0",
            gateway: "192.168.1.1",
            primaryDns: "1.1.1.1",
            secondaryDns: "8.8.8.8"));
        NetworkProfile profile = TestData.Profile(
            ipv4Address: "192.168.1.60",
            subnetMask: "255.255.255.0",
            gateway: "192.168.1.254",
            primaryDns: "9.9.9.9",
            secondaryDns: "8.8.8.8");

        draft.LoadProfile(profile);

        Assert.Equal("192.168.1.60", draft.Ipv4Address);
        Assert.Equal("255.255.255.0", draft.SubnetMask);
        Assert.Equal("192.168.1.254", draft.Gateway);
        Assert.Equal("9.9.9.9", draft.PrimaryDns);
        Assert.Equal("8.8.8.8", draft.SecondaryDns);
        Assert.True(draft.IsIpv4AddressDifferentFromWindows);
        Assert.False(draft.IsSubnetMaskDifferentFromWindows);
        Assert.True(draft.IsGatewayDifferentFromWindows);
        Assert.True(draft.IsPrimaryDnsDifferentFromWindows);
        Assert.False(draft.IsSecondaryDnsDifferentFromWindows);
        Assert.False(draft.IsDirty);
        Assert.Empty(draft.ProfileGuidance);
    }

    [Fact]
    public void LoadProfile_WhenDhcpProfileSelected_ClearsStaticValuesWithoutInventingValues()
    {
        AdapterDraftViewModel draft = CreateDraft(out _);
        draft.UpdateCurrentValues(TestData.Snapshot(
            mode: NetworkConfigurationMode.Static,
            ipv4Address: "192.168.1.50",
            gateway: "192.168.1.1"));

        draft.LoadProfile(TestData.Profile(
            mode: NetworkConfigurationMode.Dhcp,
            ipv4Address: "192.168.1.60",
            gateway: "192.168.1.254"));

        Assert.Empty(draft.Ipv4Address);
        Assert.Empty(draft.SubnetMask);
        Assert.Empty(draft.Gateway);
        Assert.Empty(draft.PrimaryDns);
        Assert.Empty(draft.SecondaryDns);
        Assert.NotEmpty(draft.ProfileGuidance);
        Assert.True(draft.IsIpv4AddressDifferentFromWindows);
        Assert.True(draft.IsGatewayDifferentFromWindows);
        Assert.False(draft.IsDirty);
    }

    private static AdapterDraftViewModel CreateDraft(out FakeClipboardService clipboard)
    {
        clipboard = new FakeClipboardService();
        return new AdapterDraftViewModel(clipboard, new StaticIpv4ConfigurationValidator());
    }
}
