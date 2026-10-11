using IPMan.App.ViewModels;
using IPMan.Domain.Profiles;

namespace IPMan.App.Presentation;

/// <summary>Connects the tray to the shell's selected connection and shared action commands.</summary>
internal sealed class TrayMenuController : IDisposable
{
    private readonly MainWindowViewModel _viewModel;
    private readonly TrayIconManager _tray;
    private readonly Action _activateWindow;
    private readonly TrayMenuBuilder _builder = new();

    public TrayMenuController(MainWindowViewModel viewModel, TrayIconManager tray, Action activateWindow)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(tray);
        ArgumentNullException.ThrowIfNull(activateWindow);
        _viewModel = viewModel;
        _tray = tray;
        _activateWindow = activateWindow;
        _tray.MenuProvider = BuildMenu;
        _tray.SwitchRequested += OnSwitchRequested;
        _tray.UndoRequested += OnUndoRequested;
        _tray.MoreSettingsRequested += OnOpenRequested;
        _tray.OpenRequested += OnOpenRequested;
        _tray.AlwaysVisible = true;
    }

    private TrayMenuModel BuildMenu()
    {
        AdapterViewModel? adapter = SelectFallbackAdapter();
        return _builder.Build(adapter?.Snapshot, _viewModel.ProfilePanel?.AllProfiles ?? [],
            _viewModel.Actions.RestoreLastCommand.CanExecute(null), _viewModel.IsBusy);
    }

    private AdapterViewModel? SelectFallbackAdapter()
    {
        AdapterViewModel? adapter = _viewModel.SelectedAdapter ?? _viewModel.Adapters.FirstOrDefault();
        if (!_viewModel.IsBusy && _viewModel.SelectedAdapter is null && adapter is not null)
        {
            _viewModel.SelectedAdapter = adapter;
        }
        return adapter;
    }

    private async void OnSwitchRequested(object? sender, string? profileId)
    {
        if (_viewModel.IsBusy || SelectFallbackAdapter() is null) return;
        NetworkProfile? profile = profileId is null ? null : _viewModel.ProfilePanel?.AllProfiles
            .FirstOrDefault(item => item.ProfileId == profileId);
        if (profileId is not null && profile is null) return;
        if (_viewModel.Actions.ApplyProfileCommand.CanExecute(profile))
        {
            await _viewModel.Actions.ApplyProfileCommand.ExecuteAsync(profile);
        }
    }

    private async void OnUndoRequested(object? sender, EventArgs e)
    {
        if (_viewModel.IsBusy || SelectFallbackAdapter() is null) return;
        if (_viewModel.Actions.RestoreLastCommand.CanExecute(null))
        {
            await _viewModel.Actions.RestoreLastCommand.ExecuteAsync(null);
        }
    }

    private void OnOpenRequested(object? sender, EventArgs e)
    {
        _activateWindow();
        if (!_viewModel.IsHomeVisible)
        {
            _viewModel.GoBackCommand.Execute(null);
        }
    }

    public void Dispose()
    {
        _tray.MenuProvider = null;
        _tray.SwitchRequested -= OnSwitchRequested;
        _tray.UndoRequested -= OnUndoRequested;
        _tray.MoreSettingsRequested -= OnOpenRequested;
        _tray.OpenRequested -= OnOpenRequested;
    }
}
