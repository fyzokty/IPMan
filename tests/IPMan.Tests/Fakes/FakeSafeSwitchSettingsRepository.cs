using IPMan.Application.Settings;
using IPMan.Domain.Settings;

namespace IPMan.Tests.Fakes;

public sealed class FakeSafeSwitchSettingsRepository : IAppSettingsRepository
{
    public AppSettings Settings { get; set; } = AppSettings.Default;
    public List<AppSettings> Saved { get; } = [];
    public SettingsSaveResult SaveResult { get; set; } = SettingsSaveResult.Success();
    public bool LastLoadFailed => false;

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Settings);
    }

    public Task<SettingsSaveResult> SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Saved.Add(settings);
        if (SaveResult.IsSuccess)
        {
            Settings = settings;
        }
        return Task.FromResult(SaveResult);
    }
}
