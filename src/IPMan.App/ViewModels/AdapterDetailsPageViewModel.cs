using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.App.ViewModels;

/// <summary>Immutable apply/save payload, also retained for retry.</summary>
public sealed record ManualSettingsRequest(NetworkAdapterId AdapterId, NetworkConfigurationMode Mode, DraftValues Values);

/// <summary>Standalone manual page. The owner attaches Actions and disposes this page on navigation.</summary>
public sealed partial class AdapterDetailsPageViewModel : ObservableObject, IDisposable
{
    private readonly IAsyncRelayCommand<ManualSettingsRequest>? _applyBoundary;
    private ManualSettingsRequest? _lastRequest;
    private ManualSettingsRequest? _appliedRequest;
    private NetworkConfigurationMode _baselineMode;
    [ObservableProperty]
    private bool _isAutomatic;

    [ObservableProperty]
    private DetailsFailureMessage? _failure;

    [ObservableProperty]
    private bool _showTechnicalDetails;

    public AdapterDetailsPageViewModel(AdapterViewModel adapter, AdapterActionsViewModel actions,
        IAsyncRelayCommand<ManualSettingsRequest>? applyBoundary = null)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(actions);
        Adapter = adapter;
        Actions = actions;
        _applyBoundary = applyBoundary;
        _baselineMode = adapter.Snapshot.Mode;
        _isAutomatic = _baselineMode == NetworkConfigurationMode.Dhcp;
        Fields = new[]
        {
            new ManualSettingsFieldViewModel(this, DraftField.Ipv4Address, Strings.DetailsAddress),
            new ManualSettingsFieldViewModel(this, DraftField.SubnetMask, Strings.DetailsMask),
            new ManualSettingsFieldViewModel(this, DraftField.Gateway, Strings.DetailsGateway),
            new ManualSettingsFieldViewModel(this, DraftField.PrimaryDns, Strings.DetailsPrimaryDns),
            new ManualSettingsFieldViewModel(this, DraftField.SecondaryDns, Strings.DetailsSecondaryDns)
        };
        ApplyCommand = new AsyncRelayCommand(ApplyAsync, CanApply);
        RetryCommand = new AsyncRelayCommand(RetryAsync, CanRetry);
        ApplyCommand.PropertyChanged += CommandChanged;
        RetryCommand.PropertyChanged += CommandChanged;
        adapter.PropertyChanged += AdapterChanged;
        adapter.Draft.PropertyChanged += StateChanged;
        actions.PropertyChanged += StateChanged;
        actions.ApplyCompleted += ApplyCompleted;
        if (_applyBoundary is not null) _applyBoundary.CanExecuteChanged += BoundaryChanged;
    }

    public AdapterViewModel Adapter { get; }
    public AdapterActionsViewModel Actions { get; }
    public IReadOnlyList<ManualSettingsFieldViewModel> Fields { get; }
    public IEnumerable<ManualSettingsFieldViewModel> BasicFields => Fields.Take(3);
    public IEnumerable<ManualSettingsFieldViewModel> DnsFields => Fields.Skip(3);
    public IAsyncRelayCommand ApplyCommand { get; }
    public IAsyncRelayCommand RetryCommand { get; }
    public event EventHandler? BackRequested;
    public event EventHandler<ManualSettingsRequest>? SaveAsSettingRequested;
    public bool IsBusy => Actions.IsBusy || ApplyCommand.IsRunning || RetryCommand.IsRunning;
    public bool CanEdit => !IsBusy;
    public bool IsStatic { get => !IsAutomatic; set => IsAutomatic = !value; }
    public NetworkConfigurationMode Mode => IsAutomatic ? NetworkConfigurationMode.Dhcp : NetworkConfigurationMode.Static;
    public bool IsModeChanged => Mode != Adapter.Snapshot.Mode;
    public int ChangeCount => Fields.Count(field => field.IsChanged) + (IsModeChanged ? 1 : 0);
    public bool HasUnappliedChanges => ChangeCount > 0 && CreateRequest() != _appliedRequest;
    public string ChangeSummary => ChangeCount == 0 ? Strings.DetailsNoChanges :
        Strings.FormatDetailsChangeCount(ChangeCount);
    public string ModeCurrentText => Strings.FormatDetailsModeCurrent(
        Adapter.Snapshot.Mode == NetworkConfigurationMode.Dhcp ? Strings.DetailsAutomatic : Strings.DetailsStatic);
    public string StatusText => Adapter.Snapshot.IsConnected && Adapter.Snapshot.InternetConnectivity == InternetConnectivity.Internet
        ? Strings.DetailsInternet : Adapter.ConnectionState;
    public ApplyStatusSeverity ConnectivitySeverity => !Adapter.Snapshot.IsConnected ? ApplyStatusSeverity.None :
        Adapter.Snapshot.InternetConnectivity == InternetConnectivity.Internet ? ApplyStatusSeverity.Success :
        Adapter.Snapshot.InternetConnectivity is InternetConnectivity.LocalNetwork or InternetConnectivity.None
            ? ApplyStatusSeverity.Warning : ApplyStatusSeverity.None;
    public bool HasFailure => Failure is not null;
    public bool HasActionStatus => !HasFailure && Actions.HasStatusMessage;
    public ManualSettingsRequest CreateRequest() => new(Adapter.Id, Mode, IsAutomatic ? DraftValues.FromSnapshot(Adapter.Snapshot) : Adapter.Draft.ToValues());

    /// <summary>Accepts a structured failure and the exact operation the retry must replay.</summary>
    public void ShowFailure(DetailsFailureMessage message, ManualSettingsRequest request)
    {
        ArgumentNullException.ThrowIfNull(message);
        PresentMessage(message, request);
    }

    public void PresentResult(StaticIpv4ApplyResult result, ManualSettingsRequest request, string settingName, string previousName) =>
        PresentMessage(DetailsFailureMessageFormatter.Describe(result, settingName, previousName), request);

    public void PresentResult(DhcpApplyResult result, ManualSettingsRequest request, string settingName, string previousName) =>
        PresentMessage(DetailsFailureMessageFormatter.Describe(result, settingName, previousName), request);

    private void PresentMessage(DetailsFailureMessage? message, ManualSettingsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.AdapterId != Adapter.Id) throw new ArgumentException("Request belongs to another adapter.", nameof(request));
        _lastRequest = request;
        if (message is null) _appliedRequest = request;
        Failure = message;
        Refresh();
        RetryCommand.NotifyCanExecuteChanged();
    }

    private void ApplyCompleted(object? sender, bool succeeded)
    {
        if (succeeded && ReferenceEquals(Actions.CurrentAdapter, Adapter))
        {
            _appliedRequest = _lastRequest;
            Failure = null;
            Refresh();
        }
    }

    [RelayCommand] private void Back() => BackRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand(CanExecute = nameof(CanEdit))] private void SaveAsSetting() => SaveAsSettingRequested?.Invoke(this, CreateRequest());
    private bool CanCancel() => !IsBusy && ChangeCount > 0;
    [RelayCommand(CanExecute = nameof(CanCancel))] private void Cancel()
    {
        if (IsBusy) return;
        Adapter.Draft.GetCurrentValues();
        IsAutomatic = Adapter.Snapshot.Mode == NetworkConfigurationMode.Dhcp;
        Refresh();
    }
    [RelayCommand] private void DismissFailure() => Failure = null;
    [RelayCommand] private void ToggleTechnicalDetails() => ShowTechnicalDetails = !ShowTechnicalDetails;
    partial void OnFailureChanged(DetailsFailureMessage? value)
    {
        ShowTechnicalDetails = false;
        OnPropertyChanged(nameof(HasFailure));
        OnPropertyChanged(nameof(HasActionStatus));
        RetryCommand.NotifyCanExecuteChanged();
    }
    partial void OnIsAutomaticChanged(bool value) => Refresh();

    private bool CanApply() => !IsBusy && HasUnappliedChanges && (IsAutomatic || Adapter.Draft.IsValid) && CanExecute(CreateRequest());
    private bool CanRetry() => !IsBusy && HasFailure && _lastRequest is not null && CanExecute(_lastRequest);
    private bool CanExecute(ManualSettingsRequest request) => ReferenceEquals(Actions.CurrentAdapter, Adapter) &&
        (_applyBoundary?.CanExecute(request) ?? (request.Mode == NetworkConfigurationMode.Dhcp
            ? Actions.ApplyDhcpCommand.CanExecute(null) : Actions.ApplyStaticCommand.CanExecute(null)));
    private async Task ApplyAsync()
    {
        _lastRequest = CreateRequest();
        await ExecuteAsync(_lastRequest);
    }
    private Task RetryAsync() => ExecuteAsync(_lastRequest!);
    private async Task ExecuteAsync(ManualSettingsRequest request)
    {
        Refresh();
        try
        {
            if (_applyBoundary is not null) await _applyBoundary.ExecuteAsync(request);
            else if (request.Mode == NetworkConfigurationMode.Dhcp) await Actions.ApplyDhcpCommand.ExecuteAsync(null);
            else
            {
                string[] values = { request.Values.Ipv4Address, request.Values.SubnetMask, request.Values.Gateway,
                    request.Values.PrimaryDns, request.Values.SecondaryDns };
                for (int index = 0; index < Fields.Count; index++) Fields[index].Value = values[index];
                await Actions.ApplyStaticCommand.ExecuteAsync(null);
            }
        }
        finally { Refresh(); }
    }
    private void AdapterChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(AdapterViewModel.Snapshot))
        {
            bool followsMode = Mode == _baselineMode;
            _baselineMode = Adapter.Snapshot.Mode;
            if (followsMode) IsAutomatic = _baselineMode == NetworkConfigurationMode.Dhcp;
            if (ChangeCount == 0) _appliedRequest = null;
        }
        Refresh();
    }
    private void CommandChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(IAsyncRelayCommand.IsRunning)) Refresh();
    }
    private void StateChanged(object? sender, PropertyChangedEventArgs args) => Refresh();
    private void BoundaryChanged(object? sender, EventArgs args) => Refresh();
    private void Refresh()
    {
        foreach (ManualSettingsFieldViewModel field in Fields) field.Refresh();
        OnPropertyChanged(nameof(IsStatic));
        OnPropertyChanged(nameof(Mode));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(IsModeChanged));
        OnPropertyChanged(nameof(ModeCurrentText));
        OnPropertyChanged(nameof(ChangeCount));
        OnPropertyChanged(nameof(HasUnappliedChanges));
        OnPropertyChanged(nameof(ChangeSummary));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(ConnectivitySeverity));
        OnPropertyChanged(nameof(HasActionStatus));
        CancelCommand.NotifyCanExecuteChanged();
        SaveAsSettingCommand.NotifyCanExecuteChanged();
        ApplyCommand.NotifyCanExecuteChanged();
        RetryCommand.NotifyCanExecuteChanged();
    }
    public void Dispose()
    {
        ApplyCommand.PropertyChanged -= CommandChanged;
        RetryCommand.PropertyChanged -= CommandChanged;
        Adapter.PropertyChanged -= AdapterChanged;
        Adapter.Draft.PropertyChanged -= StateChanged;
        Actions.PropertyChanged -= StateChanged;
        Actions.ApplyCompleted -= ApplyCompleted;
        if (_applyBoundary is not null) _applyBoundary.CanExecuteChanged -= BoundaryChanged;
    }
}
