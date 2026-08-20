using System.Text.Json;
using System.Text.Json.Serialization;
using IPMan.Domain.Networking;
using IPMan.Domain.Profiles;

namespace IPMan.Infrastructure.Profiles;

/// <summary>Authoritative JSON contract for portable profile documents.</summary>
public sealed class ProfileJsonCodec
{
    private readonly JsonSerializerOptions _serializerOptions = CreateSerializerOptions();

    /// <summary>Serializes one validated profile to a caller-owned stream.</summary>
    public async Task SerializeAsync(
        Stream destination,
        NetworkProfile profile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(profile);
        Validate(profile);

        await JsonSerializer
            .SerializeAsync(destination, profile, _serializerOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Deserializes and validates one profile from a caller-owned stream.</summary>
    public async Task<NetworkProfile> DeserializeAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        NetworkProfile? profile = await JsonSerializer
            .DeserializeAsync<NetworkProfile>(source, _serializerOptions, cancellationToken)
            .ConfigureAwait(false);
        if (profile is null)
        {
            throw new ProfileJsonException(
                ProfileLoadFailureKind.InvalidContent,
                "Profile JSON cannot be null.");
        }

        Validate(profile);
        return profile;
    }

    internal static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        return options;
    }

    private static void Validate(NetworkProfile profile)
    {
        if (profile.SchemaVersion != NetworkProfile.CurrentSchemaVersion)
        {
            throw new ProfileJsonException(
                ProfileLoadFailureKind.UnsupportedSchemaVersion,
                $"Profile schema version {profile.SchemaVersion} is not supported.");
        }

        if (string.IsNullOrWhiteSpace(profile.ProfileId) ||
            string.IsNullOrWhiteSpace(profile.Name) ||
            profile.Mode == NetworkConfigurationMode.Unknown ||
            !Enum.IsDefined(profile.Mode))
        {
            throw new ProfileJsonException(
                ProfileLoadFailureKind.InvalidContent,
                "Profile identity, name, and mode must be valid.");
        }
    }
}

internal sealed class ProfileJsonException : JsonException
{
    public ProfileJsonException(ProfileLoadFailureKind failureKind, string message)
        : base(message)
    {
        FailureKind = failureKind;
    }

    public ProfileLoadFailureKind FailureKind { get; }
}
