using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.IntegrationTests.Harness;
using IPMan.IntegrationTests.Observation;
using Xunit;

namespace IPMan.IntegrationTests.Evidence;

public sealed class EvidenceWriterTests
{
    [Fact]
    public async Task WriteResultAsync_WhenFailureContainsSensitiveText_SanitizedFileOmitsIt()
    {
        const string sensitiveText = "SECRET_ADAPTER_42_INTERNAL_DNS";
        string directory = Path.Combine(
            Path.GetTempPath(),
            "IPMan-Sprint08-EvidenceTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            EvidenceWriter writer = new(directory);
            Sprint08ResultEvidence evidence = Evidence(
                failure: $"IOException: {sensitiveText}",
                differences: new[] { sensitiveText });

            await writer.WriteResultAsync(evidence, CancellationToken.None);

            string raw = await File.ReadAllTextAsync(Path.Combine(directory, "result.json"));
            string sanitized = await File.ReadAllTextAsync(
                Path.Combine(directory, "summary.sanitized.json"));
            Assert.Contains(sensitiveText, raw, StringComparison.Ordinal);
            Assert.DoesNotContain(sensitiveText, sanitized, StringComparison.Ordinal);
            Assert.Contains("SCENARIO_EXCEPTION", sanitized, StringComparison.Ordinal);
            Assert.DoesNotContain("IOException", sanitized, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static Sprint08ResultEvidence Evidence(
        string failure,
        IReadOnlyList<string> differences)
    {
        NetworkAdapterRecoverySnapshot recovery = Recovery();
        NetworkObservation observation = Observation();
        Sprint08BeforeEvidence before = new(
            "run-id",
            new DateTimeOffset(2026, 8, 8, 12, 0, 0, TimeSpan.Zero),
            "Windows",
            "1.0.0",
            Sprint08Scenario.StaticToStatic,
            new[] { ScenarioPreconditionValidator.Ipv4AddressDimension },
            recovery.Adapter.Id,
            new StaticIpv4Configuration(
                "192.0.2.20",
                "255.255.255.0",
                null,
                null,
                null),
            observation,
            recovery);

        return new Sprint08ResultEvidence(
            before,
            before.StartedAtUtc.AddSeconds(1),
            ApplyResult: null,
            observation,
            NetworkAdapterRecoveryReadResult.Success(recovery),
            RecoveryMatchesBeforeState: false,
            Passed: false,
            differences,
            failure);
    }

    private static NetworkAdapterRecoverySnapshot Recovery()
    {
        NetworkAdapterId id = new("{11111111-2222-3333-4444-555555555555}");
        NetworkAdapterSnapshot adapter = new(
            id,
            "Adapter",
            "Disposable adapter",
            "00-11-22-33-44-55",
            true,
            1_000_000_000,
            NetworkConfigurationMode.Static,
            "192.0.2.10",
            "255.255.255.0",
            null,
            null,
            null,
            new Ipv4AddressCollection(
                new[] { new Ipv4AddressAssignment("192.0.2.10", "255.255.255.0") }),
            Ipv4AddressValueCollection.Empty,
            Ipv4AddressValueCollection.Empty);

        return new NetworkAdapterRecoverySnapshot(
            adapter,
            DnsConfigurationMode.Automatic,
            Array.Empty<string>(),
            Array.Empty<Ipv4GatewayRecoveryState>());
    }

    private static NetworkObservation Observation() =>
        new(
            new DateTimeOffset(2026, 8, 8, 12, 0, 0, TimeSpan.Zero),
            "{11111111-2222-3333-4444-555555555555}",
            "Adapter",
            "Disposable adapter",
            "00-11-22-33-44-55",
            true,
            new[] { new IpAddressObservation("192.0.2.10", 24, "255.255.255.0") },
            Array.Empty<string>(),
            Array.Empty<string>(),
            new[] { new IpAddressObservation("2001:db8::1", 64, null) },
            Array.Empty<string>(),
            Array.Empty<string>(),
            new Ipv6RouteTableObservation(
                true,
                true,
                null,
                12,
                Array.Empty<Ipv6RouteObservation>()),
            new DnsSettingsObservation(
                true,
                null,
                3,
                0,
                null,
                null,
                false,
                null,
                DnsRicherPropertiesObservationStatus.Complete,
                Array.Empty<uint>(),
                Array.Empty<DnsServerPropertyObservation>(),
                Array.Empty<DnsServerPropertyObservation>()));
}
