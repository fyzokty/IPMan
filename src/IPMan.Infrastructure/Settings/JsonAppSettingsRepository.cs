using System.Text.Json;
using IPMan.Application.Settings;
using IPMan.Domain.Settings;
using IPMan.Infrastructure.Common;

namespace IPMan.Infrastructure.Settings;

/// <summary>Stores application preferences in one atomically written JSON document.</summary>
public sealed class JsonAppSettingsRepository : IAppSettingsRepository
{
    private readonly string _settingsFilePath;
    private readonly AppSettingsJsonCodec _codec = new();

    /// <summary>Gets whether the most recent load could not read a present settings file.</summary>
    public bool LastLoadFailed { get; private set; }

    /// <summary>Initializes a settings repository with an explicit document path.</summary>
    public JsonAppSettingsRepository(AppSettingsRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.SettingsFilePath))
        {
            throw new ArgumentException("Settings file path is required.", nameof(options));
        }

        _settingsFilePath = Path.GetFullPath(options.SettingsFilePath);
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
        catch (JsonException)
        {
            LastLoadFailed = true;
            TryPreserveInvalidDocument();
            return AppSettings.Default;
        }
        catch (UnauthorizedAccessException)
        {
            LastLoadFailed = true;
            return AppSettings.Default;
        }
        catch (IOException)
        {
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
        catch (JsonException)
        {
            return SettingsSaveResult.Failed(SettingsSaveStatus.InvalidContent);
        }
        catch (UnauthorizedAccessException)
        {
            return SettingsSaveResult.Failed(SettingsSaveStatus.AccessDenied);
        }
        catch (IOException)
        {
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
