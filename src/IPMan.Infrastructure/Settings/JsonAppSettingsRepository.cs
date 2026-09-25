using System.Text.Json;
using IPMan.Application.Settings;
using IPMan.Application.Logging;
using IPMan.Domain.Settings;
using IPMan.Infrastructure.Common;

namespace IPMan.Infrastructure.Settings;

/// <summary>Stores application preferences in one atomically written JSON document.</summary>
public sealed class JsonAppSettingsRepository : IAppSettingsRepository
{
    private readonly string _settingsFilePath;
    private readonly AppSettingsJsonCodec _codec = new();
    private readonly ICriticalLogger _criticalLogger;

    /// <summary>Gets whether the most recent load could not read a present settings file.</summary>
    public bool LastLoadFailed { get; private set; }

    /// <summary>Initializes a settings repository with an explicit document path.</summary>
    public JsonAppSettingsRepository(AppSettingsRepositoryOptions options, ICriticalLogger? criticalLogger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.SettingsFilePath))
        {
            throw new ArgumentException("Settings file path is required.", nameof(options));
        }

        _settingsFilePath = Path.GetFullPath(options.SettingsFilePath);
        _criticalLogger = criticalLogger ?? new NullCriticalLogger();
    }

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            AppSettingsReadResult readResult;
            await using (FileStream stream = new(
                _settingsFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                readResult = await _codec
                    .DeserializeAsync(stream, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (readResult.ThemeWasCorrected)
            {
                SettingsSaveResult correctionResult = await SaveAsync(readResult.Settings, cancellationToken)
                    .ConfigureAwait(false);
                LastLoadFailed = !correctionResult.IsSuccess;
            }
            else
            {
                LastLoadFailed = false;
            }

            return readResult.Settings;
        }
        catch (FileNotFoundException)
        {
            LastLoadFailed = false;
            return AppSettings.Default;
        }
        catch (DirectoryNotFoundException)
        {
            LastLoadFailed = false;
            return AppSettings.Default;
        }
        catch (JsonException exception)
        {
            _criticalLogger.Log(new(CriticalLogCategory.Json, "Settings JSON could not be read.", exception.HResult, exception));
            LastLoadFailed = true;
            TryPreserveInvalidDocument();
            return AppSettings.Default;
        }
        catch (UnauthorizedAccessException exception)
        {
            _criticalLogger.Log(new(CriticalLogCategory.Json, "Settings file could not be read.", exception.HResult, exception));
            LastLoadFailed = true;
            return AppSettings.Default;
        }
        catch (IOException exception)
        {
            _criticalLogger.Log(new(CriticalLogCategory.Json, "Settings file could not be read.", exception.HResult, exception));
            LastLoadFailed = true;
            return AppSettings.Default;
        }
    }

    /// <inheritdoc />
    public async Task<SettingsSaveResult> SaveAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await AtomicJsonFileWriter.WriteAsync(
                _settingsFilePath,
                overwrite: true,
                (stream, token) => _codec.SerializeAsync(stream, settings, token),
                cancellationToken).ConfigureAwait(false);
            return SettingsSaveResult.Success();
        }
        catch (JsonException exception)
        {
            _criticalLogger.Log(new(CriticalLogCategory.SettingsWrite, "Settings could not be written.", exception.HResult, exception));
            return SettingsSaveResult.Failed(SettingsSaveStatus.InvalidContent);
        }
        catch (UnauthorizedAccessException exception)
        {
            _criticalLogger.Log(new(CriticalLogCategory.SettingsWrite, "Settings could not be written.", exception.HResult, exception));
            return SettingsSaveResult.Failed(SettingsSaveStatus.AccessDenied);
        }
        catch (IOException exception)
        {
            _criticalLogger.Log(new(CriticalLogCategory.SettingsWrite, "Settings could not be written.", exception.HResult, exception));
            return SettingsSaveResult.Failed(SettingsSaveStatus.IoFailure);
        }
    }

    private void TryPreserveInvalidDocument()
    {
        try
        {
            File.Copy(_settingsFilePath, $"{_settingsFilePath}.invalid", overwrite: true);
        }
        catch (UnauthorizedAccessException)
        {
            // Defaults remain usable even when the recovery copy cannot be created.
        }
        catch (IOException)
        {
            // Loading malformed settings must never prevent application startup.
        }
    }
}
