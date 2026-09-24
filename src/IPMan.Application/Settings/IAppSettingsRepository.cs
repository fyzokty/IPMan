using IPMan.Domain.Settings;

namespace IPMan.Application.Settings;

/// <summary>Loads and saves application preferences.</summary>
public interface IAppSettingsRepository
{
    /// <summary>Gets whether the most recent load could not read a present settings file.</summary>
    bool LastLoadFailed { get; }

    /// <summary>Loads preferences or returns safe defaults when storage is unavailable.</summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);

    /// <summary>Saves application preferences.</summary>
    Task<SettingsSaveResult> SaveAsync(
        AppSettings settings,
        CancellationToken cancellationToken);
}
