using System.Text.Json;
using System.Text.Json.Serialization;
using IPMan.Domain.Networking;

namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Authoritative JSON contract for the versioned rollback snapshot wire format.
/// </summary>
public sealed class RollbackSnapshotJsonCodec
{
    private readonly JsonSerializerOptions _serializerOptions;

    public RollbackSnapshotJsonCodec()
    {
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        _serializerOptions.Converters.Add(new NetworkAdapterIdJsonConverter());
    }

    public async Task SerializeAsync(
        Stream destination,
        NetworkRollbackSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(snapshot);
        EnsureAdapterIdentity(snapshot.AdapterId);

        await JsonSerializer
            .SerializeAsync(destination, snapshot, _serializerOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<NetworkRollbackSnapshot> DeserializeAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        NetworkRollbackSnapshot? snapshot = await JsonSerializer
            .DeserializeAsync<NetworkRollbackSnapshot>(
                source,
                _serializerOptions,
                cancellationToken)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            throw new JsonException("Rollback snapshot JSON cannot be null.");
        }

        EnsureAdapterIdentity(snapshot.AdapterId);
        return snapshot;
    }

    private static void EnsureAdapterIdentity(NetworkAdapterId adapterId)
    {
        if (string.IsNullOrWhiteSpace(adapterId.Value))
        {
            throw new JsonException("Rollback snapshot adapter identity is missing or invalid.");
        }
    }

    private sealed class NetworkAdapterIdJsonConverter : JsonConverter<NetworkAdapterId>
    {
        public override NetworkAdapterId Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Rollback snapshot adapter identity must be an object.");
            }

            string? value = null;
            bool valueSeen = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (!valueSeen || string.IsNullOrWhiteSpace(value))
                    {
                        throw new JsonException(
                            "Rollback snapshot adapter identity is missing or invalid.");
                    }

                    try
                    {
                        return new NetworkAdapterId(value);
                    }
                    catch (ArgumentException exception)
                    {
                        throw new JsonException(
                            "Rollback snapshot adapter identity is missing or invalid.",
                            exception);
                    }
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Rollback snapshot adapter identity is malformed.");
                }

                string propertyName = reader.GetString() ?? string.Empty;

                if (!reader.Read())
                {
                    throw new JsonException("Rollback snapshot adapter identity is malformed.");
                }

                if (string.Equals(propertyName, "value", StringComparison.OrdinalIgnoreCase))
                {
                    if (valueSeen || reader.TokenType != JsonTokenType.String)
                    {
                        throw new JsonException(
                            "Rollback snapshot adapter identity is missing or invalid.");
                    }

                    valueSeen = true;
                    value = reader.GetString();
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("Rollback snapshot adapter identity is malformed.");
        }

        public override void Write(
            Utf8JsonWriter writer,
            NetworkAdapterId value,
            JsonSerializerOptions options)
        {
            EnsureAdapterIdentity(value);
            writer.WriteStartObject();
            writer.WriteString("value", value.Value);
            writer.WriteEndObject();
        }
    }
}
