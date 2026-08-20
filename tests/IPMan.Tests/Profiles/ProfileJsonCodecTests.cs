using System.IO;
using System.Text;
using System.Text.Json;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;
using IPMan.Infrastructure.Profiles;
using Xunit;

namespace IPMan.Tests.Profiles;

public sealed class ProfileJsonCodecTests
{
    private const string ValidV1Json = """
        {
          "schemaVersion": 1,
          "profileId": "profile-1",
          "name": "PLC",
          "description": "Factory controller",
          "mode": "Static",
          "ipv4Address": "10.20.30.40",
          "subnetMask": "255.255.255.0",
          "gateway": "10.20.30.1",
          "primaryDns": "1.1.1.1",
          "secondaryDns": "8.8.8.8",
          "isFavorite": true,
          "originAdapterName": "Ethernet",
          "createdAtUtc": "2026-08-20T09:00:00+00:00",
          "modifiedAtUtc": "2026-08-20T09:30:00+00:00"
        }
        """;

    [Fact]
    public async Task RoundTrip_WhenProfileIsComplete_PreservesAllFields()
    {
        NetworkProfile expected = TestData.Profile(isFavorite: true);
        ProfileJsonCodec codec = new();
        await using MemoryStream stream = new();

        await codec.SerializeAsync(stream, expected, CancellationToken.None);
        stream.Position = 0;
        NetworkProfile actual = await codec.DeserializeAsync(stream, CancellationToken.None);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task SerializeAsync_WhenProfileIsValid_UsesFrozenReadableWireFormat()
    {
        await using MemoryStream stream = new();

        await new ProfileJsonCodec().SerializeAsync(
            stream,
            TestData.Profile(),
            CancellationToken.None);
        stream.Position = 0;
        using JsonDocument document = await JsonDocument.ParseAsync(stream);
        JsonElement root = document.RootElement;

        Assert.Equal(
            [
                "schemaVersion",
                "profileId",
                "name",
                "description",
                "mode",
                "ipv4Address",
                "subnetMask",
                "gateway",
                "primaryDns",
                "secondaryDns",
                "isFavorite",
                "originAdapterName",
                "createdAtUtc",
                "modifiedAtUtc"
            ],
            root.EnumerateObject().Select(property => property.Name));
        Assert.Equal(JsonValueKind.String, root.GetProperty("mode").ValueKind);
        Assert.Equal("Static", root.GetProperty("mode").GetString());
    }

    [Fact]
    public async Task DeserializeAsync_WhenExistingV1WireShapeIsUsed_RemainsReadable()
    {
        NetworkProfile profile = await DeserializeAsync(ValidV1Json);

        Assert.Equal(1, profile.SchemaVersion);
        Assert.Equal("profile-1", profile.ProfileId);
        Assert.Equal("PLC", profile.Name);
        Assert.Equal("Factory controller", profile.Description);
        Assert.Equal(NetworkConfigurationMode.Static, profile.Mode);
        Assert.Equal("10.20.30.40", profile.Ipv4Address);
        Assert.Equal("255.255.255.0", profile.SubnetMask);
        Assert.Equal("10.20.30.1", profile.Gateway);
        Assert.Equal("1.1.1.1", profile.PrimaryDns);
        Assert.Equal("8.8.8.8", profile.SecondaryDns);
        Assert.True(profile.IsFavorite);
        Assert.Equal("Ethernet", profile.OriginAdapterName);
        Assert.Equal(
            new DateTimeOffset(2026, 8, 20, 9, 0, 0, TimeSpan.Zero),
            profile.CreatedAtUtc);
        Assert.Equal(
            new DateTimeOffset(2026, 8, 20, 9, 30, 0, TimeSpan.Zero),
            profile.ModifiedAtUtc);
    }

    [Fact]
    public async Task DeserializeAsync_WhenProfileIdIsEmpty_FailsWithInvalidContent()
    {
        string json = ValidV1Json.Replace(
            "\"profileId\": \"profile-1\"",
            "\"profileId\": \" \"",
            StringComparison.Ordinal);

        ProfileJsonException exception = await Assert.ThrowsAsync<ProfileJsonException>(
            () => DeserializeAsync(json));

        Assert.Equal(ProfileLoadFailureKind.InvalidContent, exception.FailureKind);
    }

    [Fact]
    public async Task DeserializeAsync_WhenNameIsEmpty_FailsWithInvalidContent()
    {
        string json = ValidV1Json.Replace(
            "\"name\": \"PLC\"",
            "\"name\": \" \"",
            StringComparison.Ordinal);

        ProfileJsonException exception = await Assert.ThrowsAsync<ProfileJsonException>(
            () => DeserializeAsync(json));

        Assert.Equal(ProfileLoadFailureKind.InvalidContent, exception.FailureKind);
    }

    [Fact]
    public async Task DeserializeAsync_WhenModeIsUnknown_FailsWithInvalidContent()
    {
        string json = ValidV1Json.Replace(
            "\"mode\": \"Static\"",
            "\"mode\": \"Unknown\"",
            StringComparison.Ordinal);

        ProfileJsonException exception = await Assert.ThrowsAsync<ProfileJsonException>(
            () => DeserializeAsync(json));

        Assert.Equal(ProfileLoadFailureKind.InvalidContent, exception.FailureKind);
    }

    [Fact]
    public async Task DeserializeAsync_WhenSchemaVersionIsUnsupported_FailsWithUnsupportedSchemaVersion()
    {
        string json = ValidV1Json.Replace(
            "\"schemaVersion\": 1",
            "\"schemaVersion\": 2",
            StringComparison.Ordinal);

        ProfileJsonException exception = await Assert.ThrowsAsync<ProfileJsonException>(
            () => DeserializeAsync(json));

        Assert.Equal(ProfileLoadFailureKind.UnsupportedSchemaVersion, exception.FailureKind);
    }

    [Fact]
    public async Task DeserializeAsync_WhenModeIsInteger_FailsWithJsonException()
    {
        string json = ValidV1Json.Replace(
            "\"mode\": \"Static\"",
            "\"mode\": 1",
            StringComparison.Ordinal);

        await Assert.ThrowsAsync<JsonException>(() => DeserializeAsync(json));
    }

    private static async Task<NetworkProfile> DeserializeAsync(string json)
    {
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
        return await new ProfileJsonCodec()
            .DeserializeAsync(stream, CancellationToken.None);
    }
}
