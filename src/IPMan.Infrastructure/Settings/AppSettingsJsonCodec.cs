using System.Text.Json;
using IPMan.Domain.Settings;
using IPMan.Infrastructure.Profiles;

namespace IPMan.Infrastructure.Settings;

internal sealed class AppSettingsJsonCodec
{
    private readonly JsonSerializerOptions _serializerOptions =
        ProfileJsonCodec.CreateSerializerOptions();

    public async Task SerializeAsync(
        Stream destination,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        Validate(settings);
        await JsonSerializer
            .SerializeAsync(destination, settings, _serializerOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<AppSettings> DeserializeAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        SettingsDocument? document = await JsonSerializer
            .DeserializeAsync<SettingsDocument>(source, _serializerOptions, cancellationToken)
            .ConfigureAwait(false);
        if (document?.SchemaVersion is null)
        {
            throw new JsonException("Settings schema version is required.");
        }

        AppSettings defaults = AppSettings.Default;
        AppSettings settings = new(
            document.SchemaVersion.Value,
            document.Theme ?? defaults.Theme,
            document.ApplyProfileOnSelection ?? defaults.ApplyProfileOnSelection,
            document.NotificationsEnabled ?? defaults.NotificationsEnabled);
        Validate(settings);
        return settings;
    }

    private static void Validate(AppSettings settings)
    {
        if (settings.SchemaVersion != AppSettings.CurrentSchemaVersion ||
            !Enum.IsDefined(settings.Theme))
        {
            throw new JsonException("Settings schema version or theme is not supported.");
        }
    }

    private sealed record SettingsDocument(
        int? SchemaVersion,
        AppTheme? Theme,
        bool? ApplyProfileOnSelection,
        bool? NotificationsEnabled);
}
