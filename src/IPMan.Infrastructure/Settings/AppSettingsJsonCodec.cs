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
            document.NotificationsEnabled ?? defaults.NotificationsEnabled,
            ReadWindowPlacement(document.WindowPlacement),
            ReadAdapterId(document.LastSelectedAdapterId),
            ReadBoolean(document.CloseToTray),
            document.ShowVirtualAdapters ?? defaults.ShowVirtualAdapters,
            document.RememberWindowState ?? defaults.RememberWindowState);
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

    private static AppWindowPlacement? ReadWindowPlacement(JsonElement? element)
    {
        if (element is not { ValueKind: JsonValueKind.Object } placement)
        {
            return null;
        }

        double? left = ReadFiniteDouble(placement, "left", mustBePositive: false);
        double? top = ReadFiniteDouble(placement, "top", mustBePositive: false);
        double? width = ReadFiniteDouble(placement, "width", mustBePositive: true);
        double? height = ReadFiniteDouble(placement, "height", mustBePositive: true);
        bool? isMaximized = ReadBoolean(placement, "isMaximized");

        return left is null && top is null && width is null && height is null && isMaximized is null
            ? null
            : new AppWindowPlacement(left, top, width, height, isMaximized);
    }

    private static string? ReadAdapterId(JsonElement? element)
    {
        if (element is not { ValueKind: JsonValueKind.String } value ||
            !Guid.TryParse(value.GetString(), out Guid adapterId))
        {
            return null;
        }

        return adapterId.ToString("B").ToUpperInvariant();
    }

    private static bool? ReadBoolean(JsonElement? element) =>
        element is { ValueKind: JsonValueKind.True } ? true :
        element is { ValueKind: JsonValueKind.False } ? false : null;

    private static bool? ReadBoolean(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement value)
            ? ReadBoolean(value)
            : null;

    private static double? ReadFiniteDouble(
        JsonElement element,
        string propertyName,
        bool mustBePositive)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetDouble(out double number) ||
            !double.IsFinite(number) ||
            (mustBePositive && number <= 0))
        {
            return null;
        }

        return number;
    }

    private sealed record SettingsDocument(
        int? SchemaVersion,
        AppTheme? Theme,
        bool? ApplyProfileOnSelection,
        bool? NotificationsEnabled,
        JsonElement? WindowPlacement,
        JsonElement? LastSelectedAdapterId,
        JsonElement? CloseToTray,
        bool? ShowVirtualAdapters,
        bool? RememberWindowState);
}
