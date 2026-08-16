using System.Text.Json;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;

namespace IPMan.IntegrationTests.Evidence;

public static class RecoverySnapshotVerifier
{
    private static readonly RecoverySnapshotJsonCodec Codec = new();

    public static async Task<bool> MatchesAsync(
        RecoverySnapshotReference? reference,
        NetworkAdapterRecoverySnapshot before,
        CancellationToken cancellationToken)
    {
        if (reference is null || !File.Exists(reference.StoragePath))
        {
            return false;
        }

        RecoverySnapshot snapshot;

        try
        {
            await using FileStream stream = File.OpenRead(reference.StoragePath);
            snapshot = await Codec
                .DeserializeAsync(stream, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return false;
        }

        return snapshot.SnapshotId == reference.SnapshotId &&
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
