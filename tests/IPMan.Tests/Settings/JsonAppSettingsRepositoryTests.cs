using System.IO;
using System.Text.Json;
using IPMan.Application.Settings;
using IPMan.Domain.Settings;
using IPMan.Infrastructure.Settings;
using Xunit;

namespace IPMan.Tests.Settings;

public sealed class JsonAppSettingsRepositoryTests
{
    [Fact]
    public async Task LoadAsync_WhenSettingsFileDoesNotExist_ReturnsDefaultsWithoutThrowing()
    {
        string directory = CreateTestDirectory();

        try
        {
            AppSettings result = await CreateRepository(directory).LoadAsync(CancellationToken.None);

            Assert.Equal(AppSettings.Default, result);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenSettingsAreValid_RoundTripsWithStringTheme()
    {
        string directory = CreateTestDirectory();
        string settingsPath = Path.Combine(directory, "settings.json");
        AppSettings expected = new(
            AppSettings.CurrentSchemaVersion,
            AppTheme.Dark,
            ApplyProfileOnSelection: true,
            NotificationsEnabled: false,
            WindowPlacement: new AppWindowPlacement(120, 80, 1000, 700, IsMaximized: true),
            LastSelectedAdapterId: "{827A2938-BB14-4D18-B67F-94E9C4F818BA}",
            CloseToTray: true);

        try
        {
            JsonAppSettingsRepository repository = CreateRepository(directory);

            SettingsSaveResult saved = await repository.SaveAsync(expected, CancellationToken.None);
            AppSettings loaded = await repository.LoadAsync(CancellationToken.None);

            Assert.True(saved.IsSuccess);
            Assert.Equal(expected, loaded);
            using JsonDocument document = JsonDocument.Parse(
                await File.ReadAllTextAsync(settingsPath));
            JsonElement theme = document.RootElement.GetProperty("theme");
            Assert.Equal(JsonValueKind.String, theme.ValueKind);
            Assert.Equal("Dark", theme.GetString());
            Assert.False(document.RootElement.TryGetProperty("Theme", out _));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenSettingsJsonIsMalformed_ReturnsDefaultsAndPreservesOriginalContent()
    {
        string directory = CreateTestDirectory();
        string settingsPath = Path.Combine(directory, "settings.json");
        string invalidPath = $"{settingsPath}.invalid";
        const string malformedJson = "{ user-authored invalid settings";
        await File.WriteAllTextAsync(settingsPath, malformedJson);

        try
        {
            AppSettings result = await CreateRepository(directory).LoadAsync(CancellationToken.None);

            Assert.Equal(AppSettings.Default, result);
            Assert.True(File.Exists(invalidPath));
            Assert.Equal(malformedJson, await File.ReadAllTextAsync(invalidPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Default_WhenRead_LeavesApplyProfileOnSelectionDisabled()
    {
        Assert.False(AppSettings.Default.ApplyProfileOnSelection);
    }

    [Fact]
    public async Task LoadAsync_WhenValidJsonOmitsOptionalProperties_UsesPropertyDefaults()
    {
        string directory = CreateTestDirectory();
        string settingsPath = Path.Combine(directory, "settings.json");
        await File.WriteAllTextAsync(settingsPath, """
            {
              "schemaVersion": 1
            }
            """);

        try
        {
            AppSettings result = await CreateRepository(directory).LoadAsync(CancellationToken.None);

            Assert.Equal(AppSettings.Default, result);
            Assert.False(File.Exists($"{settingsPath}.invalid"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenNewPlacementFieldsAreInvalid_ResetsOnlyThoseFields()
    {
        string directory = CreateTestDirectory();
        string settingsPath = Path.Combine(directory, "settings.json");
        await File.WriteAllTextAsync(settingsPath, """
            {
              "schemaVersion": 1,
              "theme": "Dark",
              "applyProfileOnSelection": true,
              "notificationsEnabled": false,
              "windowPlacement": {
                "left": 120,
                "top": "bozuk",
                "width": 0,
                "height": 700,
                "isMaximized": "bozuk"
              },
              "lastSelectedAdapterId": "bozuk",
              "closeToTray": "bozuk"
            }
            """);

        try
        {
            AppSettings result = await CreateRepository(directory).LoadAsync(CancellationToken.None);

            Assert.Equal(AppTheme.Dark, result.Theme);
            Assert.True(result.ApplyProfileOnSelection);
            Assert.False(result.NotificationsEnabled);
            Assert.NotNull(result.WindowPlacement);
            Assert.Equal(120, result.WindowPlacement.Left);
            Assert.Null(result.WindowPlacement.Top);
            Assert.Null(result.WindowPlacement.Width);
            Assert.Equal(700, result.WindowPlacement.Height);
            Assert.Null(result.WindowPlacement.IsMaximized);
            Assert.Null(result.LastSelectedAdapterId);
            Assert.Null(result.CloseToTray);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static JsonAppSettingsRepository CreateRepository(string directory) =>
        new(new AppSettingsRepositoryOptions
        {
            SettingsFilePath = Path.Combine(directory, "settings.json")
        });

    private static string CreateTestDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"IPMan-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
