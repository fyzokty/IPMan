using IPMan.IntegrationTests.Harness;
using Xunit;

namespace IPMan.IntegrationTests.Observation;

public sealed class ObservationComparerTests
{
    [Fact]
    public void CompareNonInterference_WhenOnlyIpv4Changes_Passes()
    {
        NetworkObservation before = Observation("192.0.2.10", "1.1.1.1");
        NetworkObservation after = Observation("192.0.2.20", "1.1.1.1");

        ObservationComparisonResult result = ObservationComparer.CompareNonInterference(
            before,
            after,
            new[] { ScenarioPreconditionValidator.Ipv4AddressDimension });

        Assert.True(result.IsMatch);
    }

    [Fact]
    public void CompareNonInterference_WhenDnsIsRequested_AllowsOnlyNameServerSemantics()
    {
        NetworkObservation before = Observation("192.0.2.10", "1.1.1.1");
        NetworkObservation after = Observation("192.0.2.10", "8.8.8.8");

        ObservationComparisonResult result = ObservationComparer.CompareNonInterference(
            before,
            after,
            new[] { ScenarioPreconditionValidator.DnsDimension });

        Assert.True(result.IsMatch);
    }

    [Fact]
    public void CompareNonInterference_WhenStaticToStaticDoesNotRequestDns_DetectsNameServerChange()
    {
        NetworkObservation before = Observation("192.0.2.10", "1.1.1.1");
        NetworkObservation after = Observation("192.0.2.20", "8.8.8.8");

        ObservationComparisonResult result = ObservationComparer.CompareNonInterference(
            before,
            after,
            new[] { ScenarioPreconditionValidator.Ipv4AddressDimension });

        Assert.False(result.IsMatch);
        Assert.Contains(result.Differences, difference =>
            difference.Contains("name-server", StringComparison.Ordinal));
    }

    [Fact]
    public void CompareNonInterference_WhenStaticToStaticExplicitlyRequestsDns_AllowsNameServerChange()
    {
        NetworkObservation before = Observation("192.0.2.10", "1.1.1.1");
        NetworkObservation after = Observation("192.0.2.20", "8.8.8.8");

        ObservationComparisonResult result = ObservationComparer.CompareNonInterference(
            before,
            after,
            new[]
            {
                ScenarioPreconditionValidator.Ipv4AddressDimension,
                ScenarioPreconditionValidator.DnsDimension
            });

        Assert.True(result.IsMatch);
    }

    [Fact]
    public void CompareNonInterference_WhenDhcpToStaticDoesNotRequestDns_DetectsNameServerChange()
    {
        NetworkObservation before = Observation("192.0.2.10", "1.1.1.1");
        NetworkObservation after = Observation("192.0.2.20", "8.8.8.8");

        ObservationComparisonResult result = ObservationComparer.CompareNonInterference(
            before,
            after,
            new[]
            {
                ScenarioPreconditionValidator.ModeDimension,
                ScenarioPreconditionValidator.Ipv4AddressDimension
            });

        Assert.False(result.IsMatch);
    }

    [Fact]
    public void CompareNonInterference_WhenFocusedGatewayScenarioChangesDns_RemainsStrict()
    {
        NetworkObservation before = Observation("192.0.2.10", "1.1.1.1");
        NetworkObservation after = Observation("192.0.2.10", "8.8.8.8");

        ObservationComparisonResult result = ObservationComparer.CompareNonInterference(
            before,
            after,
            new[] { ScenarioPreconditionValidator.GatewayDimension });

        Assert.False(result.IsMatch);
    }

    [Fact]
    public void CompareNonInterference_WhenDohPropertyDisappears_Fails()
    {
        NetworkObservation before = Observation("192.0.2.10", "1.1.1.1", hasDoh: true);
        NetworkObservation after = Observation("192.0.2.10", "8.8.8.8");

        ObservationComparisonResult result = ObservationComparer.CompareNonInterference(
            before,
            after,
            new[] { ScenarioPreconditionValidator.DnsDimension });

        Assert.False(result.IsMatch);
        Assert.Contains(result.Differences, difference =>
            difference.Contains("DoH", StringComparison.Ordinal));
    }

    [Fact]
    public void CompareNonInterference_WhenIpv6Changes_Fails()
    {
        NetworkObservation before = Observation("192.0.2.10", "1.1.1.1");
        NetworkObservation after = Observation(
            "192.0.2.10",
            "1.1.1.1",
            ipv6: "2001:db8::2");

        ObservationComparisonResult result = ObservationComparer.CompareNonInterference(
            before,
            after,
            new[] { ScenarioPreconditionValidator.Ipv4AddressDimension });

        Assert.False(result.IsMatch);
    }

    [Fact]
    public void CompareNonInterference_WhenIpv6RouteIsLost_Fails()
    {
        NetworkObservation before = Observation("192.0.2.10", "1.1.1.1");
        NetworkObservation after = Observation("192.0.2.10", "1.1.1.1", includeIpv6Route: false);

        ObservationComparisonResult result = ObservationComparer.CompareNonInterference(
            before,
            after,
            new[] { ScenarioPreconditionValidator.Ipv4AddressDimension });

        Assert.False(result.IsMatch);
        Assert.Contains(result.Differences, difference =>
            difference.Contains("IPv6 routes", StringComparison.Ordinal));
    }

    [Fact]
    public void CompareNonInterference_WhenIpv6RouteObservationIsIncomplete_FailsClosed()
    {
        NetworkObservation before = Observation("192.0.2.10", "1.1.1.1", routesComplete: false);
        NetworkObservation after = Observation("192.0.2.10", "1.1.1.1", routesComplete: false);

        ObservationComparisonResult result = ObservationComparer.CompareNonInterference(
            before,
            after,
            new[] { ScenarioPreconditionValidator.Ipv4AddressDimension });

        Assert.False(result.IsMatch);
        Assert.Contains(result.Differences, difference =>
            difference.Contains("incomplete", StringComparison.Ordinal));
    }

    [Fact]
    public void CompareNonInterference_WhenUnknownDnsPayloadIsUnchanged_FailsClosed()
    {
        NetworkObservation baseline = Observation("192.0.2.10", "1.1.1.1");
        DnsSettingsObservation incompleteDns = baseline.DnsSettings with
        {
            RicherPropertiesStatus = DnsRicherPropertiesObservationStatus.Incomplete,
            UnsupportedPropertyTypes = new uint[] { 2 },
            ServerProperties = new[]
            {
                new DnsServerPropertyObservation(1, 0, 2, false, null, false, null)
            }
        };
        NetworkObservation before = baseline with { DnsSettings = incompleteDns };
        NetworkObservation after = baseline with { DnsSettings = incompleteDns };

        ObservationComparisonResult result = ObservationComparer.CompareNonInterference(
            before,
            after,
            new[] { ScenarioPreconditionValidator.DnsDimension });

        Assert.False(result.IsMatch);
        Assert.Contains(result.Differences, difference =>
            difference.Contains("unsupported property type", StringComparison.Ordinal));
    }

    private static NetworkObservation Observation(
        string ipv4,
        string dns,
        bool hasDoh = false,
        string ipv6 = "2001:db8::1",
        bool includeIpv6Route = true,
        bool routesComplete = true)
    {
        DnsServerPropertyObservation[] properties = hasDoh
            ? new[] { new DnsServerPropertyObservation(1, 0, 1, true, 1, true, "HASH") }
            : Array.Empty<DnsServerPropertyObservation>();
        Ipv6RouteObservation[] routes = includeIpv6Route
            ? new[]
            {
                new Ipv6RouteObservation(
                    "::",
                    0,
                    "fe80::1%12",
                    12,
                    1234,
                    0,
                    25,
                    3,
                    false,
                    true,
                    false,
                    false,
                    3)
            }
            : Array.Empty<Ipv6RouteObservation>();

        return new NetworkObservation(
            DateTimeOffset.UtcNow,
            "{11111111-2222-3333-4444-555555555555}",
            "Adapter",
            "Disposable adapter",
            "00-11-22-33-44-55",
            true,
            new[] { new IpAddressObservation(ipv4, 24, "255.255.255.0") },
            Array.Empty<string>(),
            new[] { dns },
            new[] { new IpAddressObservation(ipv6, 64, null) },
            Array.Empty<string>(),
            Array.Empty<string>(),
            new Ipv6RouteTableObservation(
                routesComplete,
                routesComplete,
                routesComplete ? null : 50,
                12,
                routes),
            new DnsSettingsObservation(
                true,
                null,
                3,
                0x2,
                dns,
                null,
                false,
                null,
                DnsRicherPropertiesObservationStatus.Complete,
                Array.Empty<uint>(),
                properties,
                Array.Empty<DnsServerPropertyObservation>()));
    }
}
