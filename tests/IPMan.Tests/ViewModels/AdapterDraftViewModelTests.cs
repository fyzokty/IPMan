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
        AdapterDraftViewModel draft = CreateDraft();

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
        AdapterDraftViewModel draft = CreateDraft();

        draft.UpdateCurrentValues(TestData.Snapshot(gateway: null, secondaryDns: null));

        Assert.Equal(string.Empty, draft.Gateway);
        Assert.Equal(string.Empty, draft.SecondaryDns);
    }

    [Fact]
    public void EditingAField_MarksTheDraftDirty()
    {
        AdapterDraftViewModel draft = CreateDraft();
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.5"));

        draft.Ipv4Address = "10.0.0.9";

        Assert.True(draft.IsDirty);
    }

    [Fact]
    public void EditingBackToTheLoadedValue_MakesTheDraftCleanAgain()
    {
        AdapterDraftViewModel draft = CreateDraft();
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.5"));

        draft.Ipv4Address = "10.0.0.9";
        draft.Ipv4Address = "10.0.0.5";

        Assert.False(draft.IsDirty);
    }

    [Fact]
    public void CurrentStateRefresh_UpdatesACleanDraft()
    {
        AdapterDraftViewModel draft = CreateDraft();
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.5"));

        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.77"));

        Assert.Equal("10.0.0.77", draft.Ipv4Address);
        Assert.False(draft.IsDirty);
    }

    [Fact]
    public void CurrentStateRefresh_DoesNotOverwriteADirtyDraft()
    {
        AdapterDraftViewModel draft = CreateDraft();
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.5"));

        draft.Ipv4Address = "10.0.0.9";
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.77"));

        Assert.Equal("10.0.0.9", draft.Ipv4Address);
        Assert.True(draft.IsDirty);
    }

    [Fact]
    public void CurrentStateRefresh_RetainsVisibleFieldErrorOnDirtyDraft()
    {
        AdapterDraftViewModel draft = CreateDraft();
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
        AdapterDraftViewModel draft = CreateDraft();
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.5"));

        draft.Ipv4Address = "10.0.0.9";
        draft.Gateway = "10.0.0.254";
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "10.0.0.77", gateway: "10.0.0.1"));

        draft.GetCurrentValues();

        Assert.Equal("10.0.0.77", draft.Ipv4Address);
        Assert.Equal("10.0.0.1", draft.Gateway);
        Assert.False(draft.IsDirty);
    }

    [Fact]
    public void Draft_NeverModifiesTheDomainSnapshot()
    {
        AdapterDraftViewModel draft = CreateDraft();
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
        AdapterDraftViewModel draft = CreateDraft();

        Assert.False(draft.IsValid);
        Assert.False(draft.HasErrors);
        Assert.Empty(draft.Ipv4AddressError);
        Assert.Empty(draft.SubnetMaskError);
    }

    [Fact]
    public void InvalidIpv4Edit_ShowsOnlyTheIpv4AddressError()
    {
        AdapterDraftViewModel draft = CreateDraft();

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
        AdapterDraftViewModel draft = CreateDraft();

        draft.MarkAllFieldsTouched();

        Assert.True(draft.HasErrors);
        Assert.NotEmpty(draft.Ipv4AddressError);
        Assert.NotEmpty(draft.SubnetMaskError);
    }

    [Fact]
    public void ValidValues_ExposeNormalizedConfiguration()
    {
        AdapterDraftViewModel draft = CreateDraft();

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
        AdapterDraftViewModel draft = CreateDraft();
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "192.168.1.50"));
        draft.Ipv4Address = "192.168.1.60";

        draft.MarkApplied();
        draft.UpdateCurrentValues(TestData.Snapshot(ipv4Address: "192.168.1.61"));

        Assert.False(draft.IsDirty);
        Assert.Equal("192.168.1.61", draft.Ipv4Address);
        Assert.False(draft.HasErrors);
    }

    private static AdapterDraftViewModel CreateDraft() => new(new StaticIpv4ConfigurationValidator());
}
