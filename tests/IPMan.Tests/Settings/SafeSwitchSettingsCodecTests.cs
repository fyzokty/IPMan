using System.IO;
using System.Text;
using System.Text.Json;
using IPMan.Domain.Settings;
using IPMan.Infrastructure.Settings;
using Xunit;

namespace IPMan.Tests.Settings;

public sealed class SafeSwitchSettingsCodecTests
{
    [Theory]
    [InlineData("{\"schemaVersion\":1}", false)]
    [InlineData("{\"schemaVersion\":1,\"skipInternetLossConfirmation\":null}", false)]
    [InlineData("{\"schemaVersion\":1,\"skipInternetLossConfirmation\":false}", false)]
    [InlineData("{\"schemaVersion\":1,\"skipInternetLossConfirmation\":true}", true)]
    public async Task DeserializeAsync_AdditiveFlag_DefaultsOrReadsWithoutSchemaChange(string json, bool expected)
    {
        using MemoryStream source = new(Encoding.UTF8.GetBytes(json));
        AppSettingsReadResult result = await new AppSettingsJsonCodec().DeserializeAsync(source, CancellationToken.None);
        Assert.Equal(expected, result.Settings.SkipInternetLossConfirmation);
        Assert.Equal(1, result.Settings.SchemaVersion);
        Assert.False(AppSettings.Default.SkipInternetLossConfirmation);
        Assert.False(new AppSettings(1, AppTheme.System, false, false).SkipInternetLossConfirmation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SerializeAsync_AdditiveFlag_RoundTripsWithCamelCase(bool skip)
    {
        AppSettingsJsonCodec codec = new();
        using MemoryStream stream = new();
        AppSettings expected = AppSettings.Default with { SkipInternetLossConfirmation = skip };
        await codec.SerializeAsync(stream, expected, CancellationToken.None);
        stream.Position = 0;
        using (JsonDocument document = await JsonDocument.ParseAsync(stream))
        {
            Assert.Equal(skip, document.RootElement.GetProperty("skipInternetLossConfirmation").GetBoolean());
            Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
        }
        stream.Position = 0;
        Assert.Equal(expected, (await codec.DeserializeAsync(stream, CancellationToken.None)).Settings);
    }
}
