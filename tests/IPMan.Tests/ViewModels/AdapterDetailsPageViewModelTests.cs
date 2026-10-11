using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.ViewModels;

public sealed class AdapterDetailsPageViewModelTests
{
    [Theory]
    [InlineData(0, "192.168.1.60", "192.168.1.50")]
    [InlineData(1, "255.255.0.0", "255.255.255.0")]
    [InlineData(2, "192.168.1.2", "192.168.1.1")]
    [InlineData(3, "1.1.1.1", "192.168.1.1")]
    [InlineData(4, "8.8.8.8", "")]
    public void Field_CountsChangeAndShowsLiveValue(int index, string value, string current)
    {
        using DetailsPageFixture fixture = new();
        ManualSettingsFieldViewModel field = fixture.Page.Fields[index];
        Assert.Equal(0, fixture.Page.ChangeCount);
        Assert.False(fixture.Page.ApplyCommand.CanExecute(null));
        field.Value = value;
        Assert.Equal(1, fixture.Page.ChangeCount);
        Assert.True(field.IsChanged);
        Assert.Equal(current, field.CurrentValue);
        Assert.Equal(Strings.FormatDetailsCurrent(current.Length == 0 ? Strings.DetailsEmpty : current), field.CurrentText);
        Assert.True(fixture.Page.ApplyCommand.CanExecute(null));
        field.Value = current;
        Assert.False(field.IsChanged);
        Assert.Equal(Strings.DetailsNoChanges, fixture.Page.ChangeSummary);
    }

    [Fact]
    public void StaticSelectionAndMultipleFields_CountModeAndEachChangedField()
    {
        using DetailsPageFixture fixture = new(TestData.Snapshot());
        Assert.True(fixture.Page.IsAutomatic);
        fixture.Page.IsStatic = true;
        Assert.Equal(1, fixture.Page.ChangeCount);
        fixture.Page.Fields[0].Value = "192.168.1.60";
        fixture.Page.Fields[3].Value = "1.1.1.1";
        Assert.Equal(3, fixture.Page.ChangeCount);
        Assert.Equal(Strings.FormatDetailsChangeCount(3), fixture.Page.ChangeSummary);
        fixture.Page.CancelCommand.Execute(null);
        Assert.Equal(0, fixture.Page.ChangeCount);
        Assert.True(fixture.Page.IsAutomatic);
    }

    [Fact]
    public async Task PendingSafetyBoundary_DisablesBothApplyAndRetryAndLocksInputs()
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AsyncRelayCommand<ManualSettingsRequest> boundary = new(_ => completion.Task);
        using DetailsPageFixture fixture = new(boundary: boundary);
        fixture.Page.Fields[0].Value = "192.168.1.60";
        Task applying = fixture.Page.ApplyCommand.ExecuteAsync(null);
        Assert.True(fixture.Page.IsBusy);
        Assert.False(fixture.Page.ApplyCommand.CanExecute(null));
        Assert.False(fixture.Page.CancelCommand.CanExecute(null));
        Assert.All(fixture.Page.Fields, field => Assert.True(field.IsReadOnly));
        completion.SetResult();
        await applying;
        Assert.False(fixture.Page.IsBusy);
        Assert.True(fixture.Page.ApplyCommand.CanExecute(null));
    }

    [Fact]
    public void ModeChange_CountsOnceAndAutomaticIgnoresInvalidStaticFields()
    {
        using DetailsPageFixture fixture = new();
        fixture.Adapter.Draft.Ipv4Address = "invalid";
        Assert.False(fixture.Page.ApplyCommand.CanExecute(null));
        Assert.Contains(Strings.ExampleAddress, fixture.Page.Fields[0].Error, StringComparison.Ordinal);
        fixture.Page.IsAutomatic = true;
        Assert.Equal(1, fixture.Page.ChangeCount);
        Assert.Equal(fixture.Adapter.Snapshot.Ipv4Address, fixture.Page.Fields[0].Value);
        Assert.Equal("invalid", fixture.Adapter.Draft.Ipv4Address);
        Assert.True(fixture.Page.IsModeChanged);
        Assert.True(fixture.Page.ApplyCommand.CanExecute(null));
        Assert.All(fixture.Page.Fields, field => { Assert.True(field.IsReadOnly); Assert.Empty(field.Error); Assert.False(field.IsChanged); });
        fixture.Actions.IsBusy = true;
        Assert.False(fixture.Page.ApplyCommand.CanExecute(null));
        Assert.False(fixture.Page.CanEdit);
    }

    [Fact]
    public void Cancel_ResetsEveryValueErrorsAndModeToLatestSnapshot()
    {
        using DetailsPageFixture fixture = new();
        fixture.Adapter.Draft.Ipv4Address = "invalid";
        fixture.Adapter.Draft.SecondaryDns = "8.8.8.8";
        fixture.Page.IsAutomatic = true;
        fixture.Adapter.Update(fixture.Adapter.Snapshot with { Gateway = "192.168.1.2" });
        fixture.Page.CancelCommand.Execute(null);
        Assert.Equal(DraftValues.FromSnapshot(fixture.Adapter.Snapshot), fixture.Adapter.Draft.ToValues());
        Assert.Equal(0, fixture.Page.ChangeCount);
        Assert.False(fixture.Page.IsAutomatic);
        Assert.False(fixture.Adapter.Draft.HasErrors);
        Assert.False(fixture.Page.ApplyCommand.CanExecute(null));
    }

    [Fact]
    public void Refresh_PreservesEditedModeAndValuesAndUpdatesLiveDiff()
    {
        using DetailsPageFixture fixture = new();
        fixture.Adapter.Draft.Ipv4Address = "192.168.1.60";
        fixture.Adapter.Update(fixture.Adapter.Snapshot with { Ipv4Address = "192.168.1.70" });
        Assert.Equal("192.168.1.60", fixture.Page.Fields[0].Value);
        Assert.Equal("192.168.1.70", fixture.Page.Fields[0].CurrentValue);
        fixture.Page.IsAutomatic = true;
        fixture.Adapter.Update(fixture.Adapter.Snapshot with { Description = "Updated" });
        Assert.True(fixture.Page.IsAutomatic);
        fixture.Adapter.Update(fixture.Adapter.Snapshot with { Mode = NetworkConfigurationMode.Dhcp });
        Assert.Equal(0, fixture.Page.ChangeCount);
    }

    [Theory]
    [InlineData(true, InternetConnectivity.Internet, "Bağlı · İnternet çalışıyor", ApplyStatusSeverity.Success)]
    [InlineData(true, InternetConnectivity.LocalNetwork, "Yerel ağ — internet yok", ApplyStatusSeverity.Warning)]
    [InlineData(false, InternetConnectivity.Internet, "Kablo takılı değil", ApplyStatusSeverity.None)]
    [InlineData(true, InternetConnectivity.Unknown, "Bağlı", ApplyStatusSeverity.None)]
    public void Status_UsesOperatingSystemAssessment(bool connected, InternetConnectivity connectivity, string text, ApplyStatusSeverity severity)
    {
        using DetailsPageFixture fixture = new(TestData.Snapshot(isConnected: connected) with { InternetConnectivity = connectivity });
        Assert.Equal(text, fixture.Page.StatusText);
        Assert.Equal(severity, fixture.Page.ConnectivitySeverity);
        Assert.Contains("Ethernet", fixture.Adapter.DisplayName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Apply_DelegatesStaticAndAutomaticToExistingCommands()
    {
        using DetailsPageFixture fixture = new();
        fixture.Adapter.Draft.Ipv4Address = "192.168.1.60";
        await fixture.Page.ApplyCommand.ExecuteAsync(null);
        Assert.Equal("192.168.1.60", Assert.Single(fixture.StaticApply.Requests).DesiredConfiguration.Ipv4Address);
        fixture.Page.IsAutomatic = true;
        await fixture.Page.ApplyCommand.ExecuteAsync(null);
        Assert.Single(fixture.DhcpApply.Requests);
    }

    [Fact]
    public async Task Retry_ReplaysImmutableRequestThroughSafetyBoundaryDespiteLaterEdits()
    {
        List<ManualSettingsRequest> requests = new();
        AsyncRelayCommand<ManualSettingsRequest> boundary = new(request => { requests.Add(request!); return Task.CompletedTask; });
        using DetailsPageFixture fixture = new(boundary: boundary);
        fixture.Adapter.Draft.Ipv4Address = "192.168.1.60";
        await fixture.Page.ApplyCommand.ExecuteAsync(null);
        ManualSettingsRequest first = Assert.Single(requests);
        fixture.Page.ShowFailure(DetailsFailureMessageFormatter.Describe(new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.ValidationFailed), "Elle", "Ofis")!, first);
        fixture.Adapter.Draft.Ipv4Address = "invalid";
        await fixture.Page.RetryCommand.ExecuteAsync(null);
        Assert.Equal(2, requests.Count);
        Assert.Equal(first, requests[1]);
        Assert.Empty(fixture.StaticApply.Requests);
        fixture.Page.ToggleTechnicalDetailsCommand.Execute(null);
        Assert.True(fixture.Page.ShowTechnicalDetails);
        fixture.Page.DismissFailureCommand.Execute(null);
        Assert.False(fixture.Page.HasFailure);
        Assert.False(fixture.Page.RetryCommand.CanExecute(null));
        Assert.False(fixture.Page.ShowTechnicalDetails);
    }

    [Fact]
    public async Task Retry_WithExistingCommands_ReplaysOldValuesAndClearsBannerAfterSuccess()
    {
        using DetailsPageFixture fixture = new();
        fixture.StaticApply.Result = new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.MutationFailed);
        fixture.Page.Fields[0].Value = "192.168.1.60";
        ManualSettingsRequest request = fixture.Page.CreateRequest();
        await fixture.Page.ApplyCommand.ExecuteAsync(null);
        fixture.Page.PresentResult(fixture.StaticApply.Result, request, "Elle", "Ofis");
        fixture.Page.Fields[0].Value = "192.168.1.70";
        fixture.StaticApply.Result = new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.VerifiedSuccess);
        await fixture.Page.RetryCommand.ExecuteAsync(null);
        Assert.Equal(2, fixture.StaticApply.Requests.Count);
        Assert.Equal("192.168.1.60", fixture.StaticApply.Requests[1].DesiredConfiguration.Ipv4Address);
        Assert.False(fixture.Page.HasFailure);
        Assert.False(fixture.Page.RetryCommand.CanExecute(null));
    }

    [Fact]
    public void PresentResult_ClearsEarlierFailureForSuccessfulDhcpResult()
    {
        using DetailsPageFixture fixture = new();
        ManualSettingsRequest request = fixture.Page.CreateRequest();
        fixture.Page.PresentResult(new DhcpApplyResult(DhcpApplyStatus.PartialFailure), request, "Otomatik", "Ofis");
        Assert.True(fixture.Page.HasFailure);
        fixture.Page.PresentResult(new DhcpApplyResult(DhcpApplyStatus.VerifiedSuccess), request, "Otomatik", "Ofis");
        Assert.False(fixture.Page.HasFailure);
    }

    [Fact]
    public void NavigationAndSave_EmitRequestsWithoutNetworkMutation()
    {
        using DetailsPageFixture fixture = new();
        int back = 0;
        ManualSettingsRequest? saved = null;
        fixture.Page.BackRequested += (_, _) => back++;
        fixture.Page.SaveAsSettingRequested += (_, request) => saved = request;
        fixture.Page.BackCommand.Execute(null);
        fixture.Page.SaveAsSettingCommand.Execute(null);
        Assert.Equal(1, back);
        Assert.Equal(fixture.Page.CreateRequest(), saved);
        Assert.Empty(fixture.StaticApply.Requests);
        fixture.Actions.Attach(null);
        fixture.Adapter.Draft.Ipv4Address = "192.168.1.60";
        Assert.False(fixture.Page.ApplyCommand.CanExecute(null));
    }
}
