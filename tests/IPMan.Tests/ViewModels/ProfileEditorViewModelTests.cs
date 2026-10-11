using System.IO;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Common;
using IPMan.Application.Networking;
using IPMan.Application.Profiles;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;
using IPMan.Infrastructure.Profiles;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.ViewModels;

public sealed class ProfileEditorViewModelTests
{
    private static ProfileEditorViewModel Create(FakeProfileEditorCatalog? catalog = null,
        NetworkProfile? profile = null, SequenceIpv4ConflictProbe? probe = null, IDelayProvider? delay = null,
        string? gateway = null, string? adapterAddress = "192.168.1.20", string? adapterMask = "255.255.255.0") =>
        new(catalog ?? new(), new StaticIpv4ConfigurationValidator(), new(probe ?? new()),
            delay ?? new ImmediateDelayProvider(), profile, gateway, adapterAddress, adapterMask);

    [Fact]
    public void Address_ValidAndMaskEmpty_FillsMaskAndClearsTagAfterManualEdit()
    {
        using ProfileEditorViewModel editor = Create();
        Assert.False(editor.HasErrors);
        editor.Ipv4Address = "192.168.1.50";
        Assert.Equal("255.255.255.0", editor.SubnetMask);
        Assert.True(editor.IsMaskAutoFilled);
        editor.SubnetMask = "255.255.0.0";
        Assert.False(editor.IsMaskAutoFilled);
        editor.Ipv4Address = "192.168.2.50";
        Assert.Equal("255.255.0.0", editor.SubnetMask);
        Assert.False(editor.IsMaskAutoFilled);
    }

    [Fact]
    public void Address_Invalid_DoesNotFillMaskOrExposeUntouchedFields()
    {
        using ProfileEditorViewModel editor = Create();
        editor.Ipv4Address = "192.168.1.999";
        Assert.Empty(editor.SubnetMask);
        Assert.Empty(editor.NameError);
        Assert.Empty(editor.SubnetMaskError);
        Assert.Equal(Strings.FormatProfileEditorOctetError(Strings.ExampleAddress), editor.Ipv4AddressError);
        Assert.False(editor.CanSave);
    }

    [Theory]
    [InlineData("192.168.1.999", "ProfileEditorOctetError")]
    [InlineData("192.168.1", "ProfileEditorAddressFormatError")]
    [InlineData("192.168.x.5", "ProfileEditorAddressFormatError")]
    public async Task Save_InvalidAddress_ShowsExampleAndDoesNotPersist(string address, string key)
    {
        FakeProfileEditorCatalog catalog = new();
        using ProfileEditorViewModel editor = Create(catalog);
        editor.Name = "Saha PLC";
        editor.Ipv4Address = address;
        await editor.SaveAsync(false);
        Assert.Equal(key == "ProfileEditorOctetError" ? Strings.FormatProfileEditorOctetError(Strings.ExampleAddress) : Strings.FormatProfileEditorAddressFormatError(Strings.ExampleAddress),
            editor.Ipv4AddressError);
        Assert.NotEmpty(editor.SubnetMaskError);
        Assert.True(editor.IsAdvancedExpanded);
        Assert.Empty(catalog.Saved);
    }

    [Fact]
    public async Task Save_UntouchedEmptyFields_ExposesAllRequiredErrors()
    {
        FakeProfileEditorCatalog catalog = new();
        using ProfileEditorViewModel editor = Create(catalog);
        Assert.True(editor.SaveOnlyCommand.CanExecute(null));
        await editor.SaveOnlyCommand.ExecuteAsync(null);
        Assert.Equal(Strings.ProfileEditorNameRequired, editor.NameError);
        Assert.NotEmpty(editor.Ipv4AddressError);
        Assert.NotEmpty(editor.SubnetMaskError);
        Assert.Empty(catalog.Saved);
    }

    [Fact]
    public async Task Save_OutsideSubnetGateway_WarnsButPersistsGatewayAndRaisesOnlySaveEvent()
    {
        FakeProfileEditorCatalog catalog = new();
        using ProfileEditorViewModel editor = Create(catalog);
        editor.Name = " Saha PLC ";
        editor.Ipv4Address = "192.168.1.50";
        editor.Gateway = "10.0.0.1";
        NetworkProfile? saved = null;
        editor.SaveRequested += (_, args) => saved = args.Profile;
        editor.SaveAndSwitchRequested += (_, _) => Assert.Fail("Save only must not request switching.");
        Assert.Equal(Strings.ProfileEditorGatewayWarning, editor.GatewayWarning);
        Assert.Empty(editor.GatewayError);
        Assert.True(editor.IsValid);
        Assert.True(editor.CanSave);
        await editor.SaveOnlyCommand.ExecuteAsync(null);
        NetworkProfile actual = Assert.Single(catalog.Saved);
        Assert.Same(actual, saved);
        Assert.Equal("Saha PLC", actual.Name);
        Assert.Equal("10.0.0.1", actual.Gateway);
        Assert.Equal(NetworkConfigurationMode.Static, actual.Mode);
    }

    [Theory]
    [InlineData("wrong")]
    [InlineData("192.168.1.50")]
    public async Task Save_InvalidGateway_BlocksAndDoesNotWarn(string gateway)
    {
        FakeProfileEditorCatalog catalog = new();
        using ProfileEditorViewModel editor = Create(catalog);
        editor.Name = "PLC";
        editor.Ipv4Address = "192.168.1.50";
        editor.Gateway = gateway;
        Assert.NotEmpty(editor.GatewayError);
        Assert.Empty(editor.GatewayWarning);
        await editor.SaveAsync(true);
        Assert.Empty(catalog.Saved);
    }

    [Fact]
    public async Task Save_EditRename_PreservesIdentityMetadataAndAllowsOwnName()
    {
        NetworkProfile profile = TestData.Profile(isFavorite: true);
        FakeProfileEditorCatalog catalog = new() { Profiles = [profile] };
        using ProfileEditorViewModel editor = Create(catalog, profile);
        Assert.Equal(Strings.ProfileEditorEditTitle, editor.Title);
        Assert.Equal(profile.Name, editor.Name);
        Assert.Equal(profile.Ipv4Address, editor.Ipv4Address);
        Assert.False(editor.IsMaskAutoFilled);
        Assert.True(editor.IsValid);
        editor.Name = "Yeni PLC";
        await editor.SaveAsync(false);
        NetworkProfile actual = Assert.Single(catalog.Saved);
        Assert.Equal(profile.ProfileId, actual.ProfileId);
        Assert.Equal(profile.CreatedAtUtc, actual.CreatedAtUtc);
        Assert.Equal(profile.Description, actual.Description);
        Assert.Equal(profile.OriginAdapterName, actual.OriginAdapterName);
        Assert.True(actual.IsFavorite);
        Assert.Equal("Yeni PLC", actual.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_DuplicateNameExcludingEditedProfile_IsBlocked(bool edit)
    {
        NetworkProfile other = TestData.Profile(profileId: "other", name: "Ofis");
        NetworkProfile profile = TestData.Profile(profileId: "editing", name: "PLC");
        FakeProfileEditorCatalog catalog = new() { Profiles = [other, profile] };
        using ProfileEditorViewModel editor = Create(catalog, edit ? profile : null);
        editor.Name = " ofis ";
        editor.Ipv4Address = "192.168.1.50";
        Assert.Equal(Strings.ProfileEditorDuplicateName, editor.NameError);
        await editor.SaveAsync(false);
        Assert.Empty(catalog.Saved);
    }

    [Fact]
    public async Task SaveAndSwitch_RaisesSavedCatalogResultOnceAndReusesNewIdentityOnNextSave()
    {
        FakeProfileEditorCatalog catalog = new();
        using ProfileEditorViewModel editor = Create(catalog);
        editor.Name = "PLC";
        editor.Ipv4Address = "192.168.1.50";
        List<NetworkProfile> requests = [];
        editor.SaveAndSwitchRequested += (_, args) => requests.Add(args.Profile);
        editor.SaveRequested += (_, _) => Assert.Fail("Switch save must raise only its own event.");
        await editor.SaveAndSwitchCommand.ExecuteAsync(null);
        Assert.Same(Assert.Single(catalog.Saved), Assert.Single(requests));
        await editor.SaveAsync(true);
        Assert.Equal(catalog.Saved[0].ProfileId, catalog.Saved[1].ProfileId);
        Assert.Equal(2, requests.Count);
    }

    [Fact]
    public async Task Save_Automatic_IgnoresHiddenStaticErrorsAndRoundTripsDhcpJson()
    {
        FakeProfileEditorCatalog catalog = new();
        using ProfileEditorViewModel editor = Create(catalog);
        editor.Name = "Ev";
        editor.Ipv4Address = "bad";
        editor.IsAutomatic = true;
        Assert.True(editor.IsValid);
        Assert.False(editor.HasErrors);
        await editor.SaveAsync(false);
        NetworkProfile actual = Assert.Single(catalog.Saved);
        Assert.Equal(NetworkConfigurationMode.Dhcp, actual.Mode);
        Assert.Null(actual.Ipv4Address);
        Assert.Null(actual.SubnetMask);
        Assert.Null(actual.Gateway);
        Assert.Null(actual.PrimaryDns);
        Assert.Null(actual.SecondaryDns);
        ProfileJsonCodec codec = new();
        using MemoryStream stream = new();
        await codec.SerializeAsync(stream, actual, CancellationToken.None);
        stream.Position = 0;
        Assert.Equal(actual, await codec.DeserializeAsync(stream, CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_FailedResultOrException_ShowsInlineErrorWithoutEvent(bool throws)
    {
        FakeProfileEditorCatalog catalog = new()
        {
            SaveResult = ProfileSaveResult.Failed(ProfileSaveStatus.AccessDenied),
            Failure = throws ? new IOException("fake persistence failure") : null
        };
        using ProfileEditorViewModel editor = Create(catalog);
        editor.Name = "Ev";
        editor.IsAutomatic = true;
        editor.SaveRequested += (_, _) => Assert.Fail("Failed saves must not raise events.");
        editor.SaveAndSwitchRequested += (_, _) => Assert.Fail("Failed saves must not request switching.");
        await editor.SaveAsync(true);
        Assert.Equal(Strings.ProfileEditorSaveFailed, editor.SaveError);
        Assert.False(editor.IsBusy);
        Assert.True(editor.CanSave);
    }

    [Fact]
    public async Task Save_DuplicateAppearsAfterNameEdited_RechecksCatalogBeforeSaving()
    {
        FakeProfileEditorCatalog catalog = new();
        using ProfileEditorViewModel editor = Create(catalog);
        editor.Name = "Ofis";
        editor.IsAutomatic = true;
        catalog.Profiles = [TestData.Profile(name: "Ofis")];
        await editor.SaveAsync(false);
        Assert.Equal(Strings.ProfileEditorDuplicateName, editor.NameError);
        Assert.Empty(catalog.Saved);
    }

    [Theory]
    [InlineData("SubnetMask")]
    [InlineData("PrimaryDns")]
    [InlineData("SecondaryDns")]
    public async Task Save_AdvancedError_ExpandsAndBlocks(string field)
    {
        FakeProfileEditorCatalog catalog = new();
        using ProfileEditorViewModel editor = Create(catalog);
        editor.Name = "PLC";
        editor.Ipv4Address = "192.168.1.50";
        switch (field)
        {
            case "SubnetMask": editor.SubnetMask = "255.0.255.0"; break;
            case "PrimaryDns": editor.PrimaryDns = "bad"; break;
            case "SecondaryDns": editor.SecondaryDns = "8.8.8.8"; break;
        }
        await editor.SaveAsync(false);
        Assert.True(editor.IsAdvancedExpanded);
        Assert.True(editor.HasErrors);
        Assert.Empty(catalog.Saved);
        string error = field switch
        {
            "SubnetMask" => editor.SubnetMaskError,
            "PrimaryDns" => editor.PrimaryDnsError,
            _ => editor.SecondaryDnsError
        };
        Assert.Contains(field == "SubnetMask" ? Strings.ExampleMask : Strings.ExamplePrimaryDns,
            error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Save_Cancelled_ShowsInlineMessageAndDoesNotRaiseEvent()
    {
        FakeProfileEditorCatalog catalog = new();
        using ProfileEditorViewModel editor = Create(catalog);
        editor.Name = "Ev";
        editor.IsAutomatic = true;
        editor.SaveAndSwitchRequested += (_, _) => Assert.Fail("Cancelled save cannot switch.");
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        await editor.SaveAsync(true, cancellation.Token);
        Assert.Equal(Strings.ProfileEditorSaveCancelled, editor.SaveError);
        Assert.Empty(catalog.Saved);
        Assert.False(editor.IsBusy);
    }

    [Fact]
    public async Task ModeChangedToAutomatic_CancelsDebounceAndHidesSuggestion()
    {
        using FakeDelayProvider delay = new();
        SequenceIpv4ConflictProbe probe = new();
        using ProfileEditorViewModel editor = Create(probe: probe, delay: delay);
        editor.DeviceAddress = "192.168.1.10";
        Task pending = editor.SuggestionTask;
        editor.IsAutomatic = true;
        await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Null(editor.Suggestion);
        Assert.Empty(probe.Addresses);
    }

    [Fact]
    public async Task Suggestion_Use_FillsComputerAddressAndMask()
    {
        using ProfileEditorViewModel editor = Create(gateway: "192.168.1.50");
        editor.DeviceAddress = "192.168.1.10";
        await editor.SuggestionTask;
        Assert.True(editor.IsSuggestionVerified);
        Assert.Equal("192.168.1.51", editor.Suggestion!.Address);
        editor.UseSuggestionCommand.Execute(null);
        Assert.Equal("192.168.1.51", editor.Ipv4Address);
        Assert.True(editor.IsMaskAutoFilled);
    }

    [Fact]
    public async Task DeviceChanged_CancelsPreviousDebounceAndProbesOnlyLatestDevice()
    {
        using FakeDelayProvider delay = new();
        SequenceIpv4ConflictProbe probe = new();
        using ProfileEditorViewModel editor = Create(probe: probe, delay: delay, adapterAddress: "10.0.0.20");
        editor.DeviceAddress = "192.168.1.10";
        Task previous = editor.SuggestionTask;
        editor.DeviceAddress = "10.0.0.10";
        await previous.WaitAsync(TimeSpan.FromSeconds(5));
        delay.ReleaseDelays(ProfileEditorViewModel.SuggestionDebounce);
        await editor.SuggestionTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("10.0.0.50", Assert.Single(probe.Addresses));
        Assert.Equal("10.0.0.50", editor.Suggestion!.Address);
    }

    [Fact]
    public async Task DeviceChanged_ProbeIgnoringCancellation_CannotPublishStaleResult()
    {
        TaskCompletionSource<Ipv4ConflictProbeResult> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken oldToken = default;
        SequenceIpv4ConflictProbe probe = new()
        {
            Handler = (address, token) =>
            {
                if (address.StartsWith("192.168.1.", StringComparison.Ordinal))
                {
                    oldToken = token;
                    return pending.Task;
                }
                return Task.FromResult(new Ipv4ConflictProbeResult(Ipv4ConflictProbeStatus.Unavailable));
            }
        };
        using ProfileEditorViewModel editor = Create(probe: probe);
        editor.DeviceAddress = "192.168.1.10";
        Task previous = editor.SuggestionTask;
        editor.DeviceAddress = "10.0.0.10";
        Assert.True(oldToken.IsCancellationRequested);
        await editor.SuggestionTask;
        pending.SetResult(new(Ipv4ConflictProbeStatus.NoResponse));
        await previous;
        Assert.Equal("10.0.0.50", editor.Suggestion!.Address);
        Assert.False(editor.IsSuggestionVerified);
    }

    [Fact]
    public async Task BackAndDispose_CancelOutstandingSuggestion()
    {
        using FakeDelayProvider delay = new();
        SequenceIpv4ConflictProbe probe = new();
        using ProfileEditorViewModel editor = Create(probe: probe, delay: delay);
        int backCount = 0;
        editor.BackRequested += (_, _) => backCount++;
        editor.DeviceAddress = "192.168.1.10";
        editor.BackCommand.Execute(null);
        await editor.SuggestionTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, backCount);
        editor.DeviceAddress = "10.0.0.10";
        editor.Dispose();
        await editor.SuggestionTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Empty(probe.Addresses);
        Assert.False(editor.CanSave);
    }

    [Fact]
    public async Task Suggestion_DeviceOutsideAdapterSubnet_IsUnverifiedWithoutProbe()
    {
        SequenceIpv4ConflictProbe probe = new();
        using ProfileEditorViewModel editor = Create(probe: probe, adapterAddress: "10.0.0.20");
        editor.DeviceAddress = "192.168.1.10";
        await editor.SuggestionTask;
        Assert.Equal("192.168.1.50", editor.Suggestion!.Address);
        Assert.False(editor.IsSuggestionVerified);
        Assert.Equal(Strings.ProfileEditorUnverified, editor.SuggestionStatusText);
        Assert.Empty(probe.Addresses);
    }

    [Fact]
    public async Task Suggestion_FormGatewayIsExcludedAndChangesRefreshSuggestion()
    {
        SequenceIpv4ConflictProbe probe = new();
        using ProfileEditorViewModel editor = Create(probe: probe, gateway: "192.168.1.52");
        editor.Gateway = "192.168.1.50";
        editor.DeviceAddress = "192.168.1.10";
        await editor.SuggestionTask;
        Assert.Equal("192.168.1.51", editor.Suggestion!.Address);
        Assert.DoesNotContain("192.168.1.50", probe.Addresses);

        editor.Gateway = "192.168.1.51";
        await editor.SuggestionTask;
        Assert.Equal("192.168.1.50", editor.Suggestion!.Address);

        Task current = editor.SuggestionTask;
        editor.Gateway = " 192.168.1.51";
        editor.Gateway = "192.168.1.51";
        Assert.Same(current, editor.SuggestionTask);

        editor.Gateway = "192.168.1.5";
        await editor.SuggestionTask;
        Assert.Equal("192.168.1.50", editor.Suggestion!.Address);
        Assert.DoesNotContain("192.168.1.52", probe.Addresses);
    }
}
