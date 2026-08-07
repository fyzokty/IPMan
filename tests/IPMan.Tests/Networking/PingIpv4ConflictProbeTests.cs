using IPMan.Application.Networking;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class PingIpv4ConflictProbeTests
{
    [Theory]
    [InlineData("not-an-address")]
    [InlineData("2001:db8::1")]
    public async Task ProbeAsync_WhenInputIsNotIpv4_ReturnsUnavailableWithoutNetworkProbe(string address)
    {
        PingIpv4ConflictProbe probe = new(new Ipv4ConflictProbeOptions());

        Ipv4ConflictProbeResult result = await probe.ProbeAsync(address, CancellationToken.None);

        Assert.Equal(Ipv4ConflictProbeStatus.Unavailable, result.Status);
    }

    [Fact]
    public void Constructor_WhenTimeoutIsNotPositive_Throws()
    {
        Ipv4ConflictProbeOptions options = new() { Timeout = TimeSpan.Zero };

        Assert.Throws<ArgumentOutOfRangeException>(() => new PingIpv4ConflictProbe(options));
    }

    [Fact]
    public void Constructor_WhenTimeoutIsNotShortAndBounded_Throws()
    {
        Ipv4ConflictProbeOptions options = new()
        {
            Timeout = PingIpv4ConflictProbe.MaximumTimeout + TimeSpan.FromMilliseconds(1)
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new PingIpv4ConflictProbe(options));
    }

    [Fact]
    public async Task ProbeAsync_WhenAlreadyCancelled_PropagatesCancellationWithoutProbing()
    {
        PingIpv4ConflictProbe probe = new(new Ipv4ConflictProbeOptions());
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => probe.ProbeAsync("192.0.2.1", cancellation.Token));
    }
}
