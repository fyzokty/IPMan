using IPMan.Domain.Settings;

namespace IPMan.Application.Settings;

/// <summary>Loads and saves application preferences.</summary>
public interface IAppSettingsRepository
{
    /// <summary>Loads preferences or returns safe defaults when storage is unavailable.</summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);

    /// <summary>Saves application preferences.</summary>
    Task<SettingsSaveResult> SaveAsync(
        AppSettings settings,
        CancellationToken cancellationToken);
}
