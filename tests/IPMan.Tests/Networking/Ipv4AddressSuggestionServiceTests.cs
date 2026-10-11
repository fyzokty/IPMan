using IPMan.Application.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class Ipv4AddressSuggestionServiceTests
{
    private static Task<Ipv4AddressSuggestion?> Suggest(SequenceIpv4ConflictProbe probe, string device,
        CancellationToken cancellationToken, string? adapterAddress = "192.168.1.20", string? adapterMask = "255.255.255.0") =>
        new Ipv4AddressSuggestionService(probe).SuggestAsync(
            new(device, [], adapterAddress, adapterMask), cancellationToken);

    [Theory]
    [InlineData("192.168.1.10", "192.168.1.50", "192.168.1.51")]
    [InlineData("192.168.1.50", "192.168.1.51", "192.168.1.52")]
    [InlineData("192.168.1.50", "10.0.0.51", "192.168.1.51")]
    [InlineData("192.168.1.10", null, "192.168.1.50")]
    public void Candidates_SkipDeviceGatewayAndReservedHostsAndWrap(string device, string? gateway, string first)
    {
        IReadOnlyList<string> candidates = Ipv4AddressSuggestionService.GenerateCandidates(device, gateway);
        Assert.Equal(first, candidates[0]);
        Assert.DoesNotContain(device, candidates);
        Assert.DoesNotContain("192.168.1.0", candidates);
        Assert.DoesNotContain("192.168.1.255", candidates);
        if (gateway is not null) Assert.DoesNotContain(gateway, candidates);
        Assert.Equal(candidates.Count, candidates.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("192.168.1.254", candidates[candidates.ToList().IndexOf("192.168.1.1") - 1]);
        Assert.All(candidates, address => Assert.StartsWith("192.168.1.", address, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(Ipv4ConflictProbeStatus.NoResponse, Ipv4AddressSuggestionStatus.VerifiedFree)]
    [InlineData(Ipv4ConflictProbeStatus.Unavailable, Ipv4AddressSuggestionStatus.Unverified)]
    [InlineData(Ipv4ConflictProbeStatus.Cancelled, Ipv4AddressSuggestionStatus.Unverified)]
    public async Task SuggestAsync_MapsProbeEvidence(Ipv4ConflictProbeStatus probeStatus, Ipv4AddressSuggestionStatus expected)
    {
        SequenceIpv4ConflictProbe probe = new();
        probe.Results.Enqueue(probeStatus);
        Ipv4AddressSuggestion? suggestion = await Suggest(probe, "192.168.1.10", CancellationToken.None);
        Assert.Equal(new("192.168.1.50", expected), suggestion);
        Assert.Single(probe.Addresses);
    }

    [Fact]
    public async Task SuggestAsync_ResponseObserved_TriesNextCandidate()
    {
        SequenceIpv4ConflictProbe probe = new();
        probe.Results.Enqueue(Ipv4ConflictProbeStatus.ResponseObserved);
        probe.Results.Enqueue(Ipv4ConflictProbeStatus.NoResponse);
        Ipv4AddressSuggestion? suggestion = await Suggest(probe, "192.168.1.10", CancellationToken.None);
        Assert.Equal(new("192.168.1.51", Ipv4AddressSuggestionStatus.VerifiedFree), suggestion);
        Assert.Collection(probe.Addresses,
            address => Assert.Equal("192.168.1.50", address),
            address => Assert.Equal("192.168.1.51", address));
    }

    [Fact]
    public async Task SuggestAsync_AllRespond_StopAfterFiveAndReturnSixthUnverified()
    {
        SequenceIpv4ConflictProbe probe = new();
        for (int index = 0; index < 6; index++) probe.Results.Enqueue(Ipv4ConflictProbeStatus.ResponseObserved);
        Ipv4AddressSuggestion? suggestion = await Suggest(probe, "192.168.1.10", CancellationToken.None);
        Assert.Equal(new("192.168.1.55", Ipv4AddressSuggestionStatus.Unverified), suggestion);
        Assert.Equal(5, probe.Addresses.Count);
        Assert.DoesNotContain(suggestion!.Address, probe.Addresses);
    }

    [Fact]
    public async Task SuggestAsync_ProbeFailure_ReturnsFirstUnverified()
    {
        SequenceIpv4ConflictProbe probe = new()
        {
            Handler = (_, _) => Task.FromException<Ipv4ConflictProbeResult>(new InvalidOperationException("Probe unavailable."))
        };
        Assert.Equal(new("192.168.1.50", Ipv4AddressSuggestionStatus.Unverified),
            await Suggest(probe, "192.168.1.10", CancellationToken.None));
        Assert.Single(probe.Addresses);
    }

    [Fact]
    public async Task SuggestAsync_UnavailableAfterOccupiedCandidate_ReturnsCurrentUnverified()
    {
        SequenceIpv4ConflictProbe probe = new();
        probe.Results.Enqueue(Ipv4ConflictProbeStatus.ResponseObserved);
        probe.Results.Enqueue(Ipv4ConflictProbeStatus.Unavailable);
        Assert.Equal(new("192.168.1.51", Ipv4AddressSuggestionStatus.Unverified),
            await Suggest(probe, "192.168.1.10", CancellationToken.None));
        Assert.Equal(2, probe.Addresses.Count);
    }

    [Fact]
    public async Task SuggestAsync_ProbeFailureAfterOccupiedCandidate_ReturnsCurrentUnverified()
    {
        SequenceIpv4ConflictProbe probe = new()
        {
            Handler = (address, _) => address == "192.168.1.50"
                ? Task.FromResult(new Ipv4ConflictProbeResult(Ipv4ConflictProbeStatus.ResponseObserved))
                : Task.FromException<Ipv4ConflictProbeResult>(new InvalidOperationException("Probe unavailable."))
        };
        Assert.Equal(new("192.168.1.51", Ipv4AddressSuggestionStatus.Unverified),
            await Suggest(probe, "192.168.1.10", CancellationToken.None));
        Assert.Equal(2, probe.Addresses.Count);
    }

    [Fact]
    public async Task SuggestAsync_AlreadyCancelled_DoesNotProbe()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        SequenceIpv4ConflictProbe probe = new();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Suggest(probe, "192.168.1.10", cancellation.Token));
        Assert.Empty(probe.Addresses);
    }

    [Fact]
    public async Task SuggestAsync_CancelledDuringProbe_DoesNotVerifyOrContinue()
    {
        using CancellationTokenSource cancellation = new();
        SequenceIpv4ConflictProbe probe = new()
        {
            Handler = (_, _) =>
            {
                cancellation.Cancel();
                return Task.FromResult(new Ipv4ConflictProbeResult(Ipv4ConflictProbeStatus.NoResponse));
            }
        };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Suggest(probe, "192.168.1.10", cancellation.Token));
        Assert.Single(probe.Addresses);
    }

    [Theory]
    [InlineData("192.168.1.999")]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    public async Task SuggestAsync_InvalidDevice_DoesNotProbe(string device)
    {
        SequenceIpv4ConflictProbe probe = new();
        Assert.Null(await Suggest(probe, device, CancellationToken.None));
        Assert.Empty(probe.Addresses);
    }

    [Theory]
    [InlineData("10.0.0.20", "255.255.255.0")]
    [InlineData("192.168.1.130", "255.255.255.128")]
    [InlineData(null, null)]
    [InlineData("192.168.1.20", null)]
    [InlineData("192.168.1.20", "255.0.255.0")]
    [InlineData("192.168.1.20", "0.0.0.0")]
    [InlineData("0.0.0.0", "255.255.255.0")]
    public async Task SuggestAsync_CandidateNotOnAdapterSubnet_ReturnsFirstUnverifiedWithoutProbe(
        string? adapterAddress, string? adapterMask)
    {
        SequenceIpv4ConflictProbe probe = new();
        Assert.Equal(new("192.168.1.50", Ipv4AddressSuggestionStatus.Unverified),
            await Suggest(probe, "192.168.1.10", CancellationToken.None, adapterAddress, adapterMask));
        Assert.Empty(probe.Addresses);
    }

    [Theory]
    [InlineData("192.168.1.200", "255.255.255.0")]
    [InlineData("192.168.7.9", "255.255.0.0")]
    [InlineData("192.168.1.20", "255.255.255.192")]
    public async Task SuggestAsync_CandidateOnAdapterSubnet_ProbesAndMayVerify(string adapterAddress, string adapterMask)
    {
        SequenceIpv4ConflictProbe probe = new();
        Assert.Equal(new("192.168.1.50", Ipv4AddressSuggestionStatus.VerifiedFree),
            await Suggest(probe, "192.168.1.10", CancellationToken.None, adapterAddress, adapterMask));
        Assert.Equal("192.168.1.50", Assert.Single(probe.Addresses));
    }

    [Fact]
    public async Task SuggestAsync_OccupiedCandidatesThenOffLinkCandidate_StopsProbingAtSubnetEdge()
    {
        // Adapter 192.168.1.49/28 reaches .48-.63; excluding .52-.63 makes .64 the third candidate.
        string?[] excluded = Enumerable.Range(52, 12).Select(host => (string?)$"192.168.1.{host}").ToArray();
        SequenceIpv4ConflictProbe probe = new();
        probe.Results.Enqueue(Ipv4ConflictProbeStatus.ResponseObserved);
        probe.Results.Enqueue(Ipv4ConflictProbeStatus.ResponseObserved);
        Ipv4AddressSuggestion? suggestion = await new Ipv4AddressSuggestionService(probe).SuggestAsync(
            new("192.168.1.10", excluded, "192.168.1.49", "255.255.255.240"), CancellationToken.None);
        Assert.Equal(new("192.168.1.64", Ipv4AddressSuggestionStatus.Unverified), suggestion);
        Assert.Equal(["192.168.1.50", "192.168.1.51"], probe.Addresses);
    }

    [Fact]
    public void Candidates_SkipEveryExcludedAddressAndIgnoreInvalidOnes()
    {
        IReadOnlyList<string> candidates = Ipv4AddressSuggestionService.GenerateCandidates(
            "192.168.1.10", ["192.168.1.50", null, "not an address", " 192.168.1.51 "]);
        Assert.Equal("192.168.1.52", candidates[0]);
        Assert.DoesNotContain("192.168.1.50", candidates);
        Assert.DoesNotContain("192.168.1.51", candidates);
    }
}
