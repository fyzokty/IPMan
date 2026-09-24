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

    public async Task<AppSettingsReadResult> DeserializeAsync(
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
        (AppTheme theme, bool themeWasCorrected) = ReadTheme(document.Theme, defaults.Theme);
        AppSettings settings = new(
            document.SchemaVersion.Value,
            theme,
            document.ApplyProfileOnSelection ?? defaults.ApplyProfileOnSelection,
            document.NotificationsEnabled ?? defaults.NotificationsEnabled,
            ReadWindowPlacement(document.WindowPlacement),
            ReadAdapterId(document.LastSelectedAdapterId),
            ReadBoolean(document.CloseToTray),
            document.ShowVirtualAdapters ?? defaults.ShowVirtualAdapters,
            document.RememberWindowState ?? defaults.RememberWindowState,
            document.SkipDhcpQuickActionConfirmation ?? defaults.SkipDhcpQuickActionConfirmation,
            ReadNotificationMode(document.NotificationMode, document.NotificationsEnabled),
            document.NotificationPromptShown ?? defaults.NotificationPromptShown);
        Validate(settings);
        return new AppSettingsReadResult(settings, themeWasCorrected);
    }

    private static void Validate(AppSettings settings)
    {
        if (settings.SchemaVersion != AppSettings.CurrentSchemaVersion ||
            !Enum.IsDefined(settings.Theme))
        {
            throw new JsonException("Settings schema version or theme is not supported.");
        }
    }

    private static (AppTheme Theme, bool WasCorrected) ReadTheme(
        JsonElement? element,
        AppTheme defaultTheme)
    {
        if (element is null || element.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return (defaultTheme, false);
        }

        JsonElement value = element.Value;
        if (value.ValueKind == JsonValueKind.String &&
            Enum.TryParse(value.GetString(), ignoreCase: true, out AppTheme stringTheme) &&
            Enum.IsDefined(stringTheme))
        {
            return (stringTheme, false);
        }

        if (value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out int numericTheme) &&
            Enum.IsDefined((AppTheme)numericTheme))
        {
            return ((AppTheme)numericTheme, false);
        }

        return (AppTheme.System, true);
    }

    private static NotificationMode ReadNotificationMode(JsonElement? element, bool? legacyEnabled)
    {
        if (element is { } value)
        {
            if (value.ValueKind == JsonValueKind.String &&
                Enum.TryParse(value.GetString(), ignoreCase: true, out NotificationMode mode) &&
                Enum.IsDefined(mode))
            {
                return mode;
            }

            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out int numericMode) &&
                Enum.IsDefined((NotificationMode)numericMode))
            {
                return (NotificationMode)numericMode;
            }

            if (value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            {
                return NotificationMode.Disabled;
            }
        }

        return legacyEnabled == true ? NotificationMode.WhenUnfocused : NotificationMode.Disabled;
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
        JsonElement? Theme,
        bool? ApplyProfileOnSelection,
        bool? NotificationsEnabled,
        JsonElement? WindowPlacement,
        JsonElement? LastSelectedAdapterId,
        JsonElement? CloseToTray,
        bool? ShowVirtualAdapters,
        bool? RememberWindowState,
        bool? SkipDhcpQuickActionConfirmation,
        JsonElement? NotificationMode,
        bool? NotificationPromptShown);
}

internal sealed record AppSettingsReadResult(AppSettings Settings, bool ThemeWasCorrected);
