using System.Text.Json;
using IPMan.Domain.Networking;

namespace IPMan.IntegrationTests.Evidence;

public static class RollbackSnapshotVerifier
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<bool> MatchesAsync(
        RollbackSnapshotReference? reference,
        NetworkAdapterRecoverySnapshot before,
        CancellationToken cancellationToken)
    {
        if (reference is null || !File.Exists(reference.StoragePath))
        {
            return false;
        }

        await using FileStream stream = File.OpenRead(reference.StoragePath);
        NetworkRollbackSnapshot? snapshot = await JsonSerializer
            .DeserializeAsync<NetworkRollbackSnapshot>(stream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        return snapshot is not null &&
            snapshot.SnapshotId == reference.SnapshotId &&
            snapshot.AdapterId == before.Adapter.Id &&
            snapshot.Mode == before.Adapter.Mode &&
            snapshot.DnsMode == before.DnsMode &&
            snapshot.Ipv4Addresses.SequenceEqual(before.Adapter.Ipv4Addresses) &&
            snapshot.Ipv4Gateways.SequenceEqual(before.Ipv4Gateways) &&
            snapshot.ConfiguredIpv4DnsServers.SequenceEqual(
                before.ConfiguredIpv4DnsServers,
                StringComparer.Ordinal) &&
            snapshot.Ipv4DnsServers.SequenceEqual(
                before.Adapter.Ipv4DnsServers,
                StringComparer.Ordinal);
    }
}
