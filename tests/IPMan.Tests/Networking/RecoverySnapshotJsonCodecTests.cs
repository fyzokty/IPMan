using System.IO;
using System.Text;
using System.Text.Json;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class RecoverySnapshotJsonCodecTests
{
    private const string CanonicalAdapterId = "{827A2938-BB14-4D18-B67F-94E9C4F818BA}";

    [Fact]
    public async Task RoundTrip_WhenSnapshotIsComplete_PreservesFullRecoveryState()
    {
        RecoverySnapshotJsonCodec codec = new();
        RecoverySnapshot expected = Snapshot();
        await using MemoryStream stream = new();

        await codec.SerializeAsync(stream, expected, CancellationToken.None);
        stream.Position = 0;
        RecoverySnapshot actual = await codec.DeserializeAsync(stream, CancellationToken.None);

        AssertSnapshotEqual(expected, actual);
        Assert.Equal(CanonicalAdapterId, actual.AdapterId.Value);
    }

    [Fact]
    public async Task DeserializeAsync_WhenAdapterIdentityIsMissing_FailsClosed()
    {
        await Assert.ThrowsAsync<JsonException>(
            () => DeserializeAsync("""{"schemaVersion":2,"snapshotId":"snapshot-1"}"""));
    }

    [Fact]
    public async Task DeserializeAsync_WhenAdapterIdentityIsMalformed_FailsClosed()
    {
        await Assert.ThrowsAsync<JsonException>(
            () => DeserializeAsync("""{"adapterId":{"value":" "}}"""));
    }

    [Fact]
    public async Task DeserializeAsync_WhenExistingV2WireShapeIsUsed_RemainsReadable()
    {
        const string v2Json = """
            {
              "schemaVersion": 2,
              "snapshotId": "snapshot-1",
              "capturedAtUtc": "2026-08-08T09:30:00+00:00",
              "state": 0,
              "adapterId": { "value": "{827a2938-bb14-4d18-b67f-94e9c4f818ba}" },
              "adapterName": "Ethernet",
              "adapterDescription": "Contoso Adapter",
              "mode": 2,
              "ipv4Addresses": [
                { "address": "10.250.0.10", "subnetMask": "255.255.255.0" }
              ],
              "ipv4Gateways": [],
              "dnsMode": 1,
              "configuredIpv4DnsServers": [],
              "ipv4DnsServers": []
            }
            """;

        RecoverySnapshot snapshot = await DeserializeAsync(v2Json);

        Assert.Equal(2, snapshot.SchemaVersion);
        Assert.Equal(CanonicalAdapterId, snapshot.AdapterId.Value);
        Assert.Equal("10.250.0.10", Assert.Single(snapshot.Ipv4Addresses).Address);
        Assert.Equal(DnsConfigurationMode.Automatic, snapshot.DnsMode);
    }

    private static async Task<RecoverySnapshot> DeserializeAsync(string json)
    {
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
        return await new RecoverySnapshotJsonCodec()
            .DeserializeAsync(stream, CancellationToken.None);
    }

    private static RecoverySnapshot Snapshot() =>
        new(
            SchemaVersion: 2,
            SnapshotId: "snapshot-1",
            CapturedAtUtc: new DateTimeOffset(2026, 8, 8, 9, 30, 0, TimeSpan.Zero),
            State: RecoverySnapshotState.Captured,
            AdapterId: new NetworkAdapterId("827a2938-bb14-4d18-b67f-94e9c4f818ba"),
            AdapterName: "Ethernet",
            AdapterDescription: "Contoso Adapter",
            Mode: NetworkConfigurationMode.Static,
            Ipv4Addresses:
            [
                new Ipv4AddressAssignment("10.250.0.10", "255.255.255.0"),
                new Ipv4AddressAssignment("10.250.0.11", "255.255.255.0")
            ],
            Ipv4Gateways: [new Ipv4GatewayRecoveryState("10.250.0.1", 25)],
            DnsMode: DnsConfigurationMode.Manual,
            ConfiguredIpv4DnsServers: ["1.1.1.1", "8.8.8.8"],
            Ipv4DnsServers: ["1.1.1.1", "8.8.8.8"]);

    private static void AssertSnapshotEqual(
        RecoverySnapshot expected,
        RecoverySnapshot actual)
    {
        Assert.Equal(expected.SchemaVersion, actual.SchemaVersion);
        Assert.Equal(expected.SnapshotId, actual.SnapshotId);
        Assert.Equal(expected.CapturedAtUtc, actual.CapturedAtUtc);
        Assert.Equal(expected.State, actual.State);
        Assert.Equal(expected.AdapterId, actual.AdapterId);
        Assert.Equal(expected.AdapterName, actual.AdapterName);
        Assert.Equal(expected.AdapterDescription, actual.AdapterDescription);
        Assert.Equal(expected.Mode, actual.Mode);
        Assert.Equal(expected.Ipv4Addresses, actual.Ipv4Addresses);
        Assert.Equal(expected.Ipv4Gateways, actual.Ipv4Gateways);
        Assert.Equal(expected.DnsMode, actual.DnsMode);
        Assert.Equal(expected.ConfiguredIpv4DnsServers, actual.ConfiguredIpv4DnsServers);
        Assert.Equal(expected.Ipv4DnsServers, actual.Ipv4DnsServers);
    }
}
