using IPMan.Domain.Networking;

namespace IPMan.Domain.Profiles;

/// <summary>A portable, named network configuration persisted as a user document.</summary>
public sealed record NetworkProfile(
    int SchemaVersion,
    string ProfileId,
    string Name,
    string? Description,
    NetworkConfigurationMode Mode,
    string? Ipv4Address,
    string? SubnetMask,
    string? Gateway,
    string? PrimaryDns,
    string? SecondaryDns,
    bool IsFavorite,
    string? OriginAdapterName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ModifiedAtUtc)
{
    /// <summary>The profile schema version understood by this release.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Maps the stored IPv4 values without performing validation.</summary>
    public StaticIpv4Configuration ToStaticConfiguration() =>
        new(
            Ipv4Address ?? string.Empty,
            SubnetMask ?? string.Empty,
            Gateway,
            PrimaryDns,
            SecondaryDns);
}
