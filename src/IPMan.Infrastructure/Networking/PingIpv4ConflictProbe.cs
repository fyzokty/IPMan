using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using IPMan.Application.Networking;

namespace IPMan.Infrastructure.Networking;

/// <summary>One short ICMP echo attempt used only as best-effort conflict evidence.</summary>
public sealed class PingIpv4ConflictProbe : IIpv4ConflictProbe
{
    public static readonly TimeSpan MaximumTimeout = TimeSpan.FromSeconds(5);

    private readonly int _timeoutMilliseconds;

    public PingIpv4ConflictProbe(Ipv4ConflictProbeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Timeout <= TimeSpan.Zero || options.Timeout > MaximumTimeout)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Timeout, "Ping timeout must be positive and bounded.");
        }

        _timeoutMilliseconds = checked((int)Math.Ceiling(options.Timeout.TotalMilliseconds));
    }

    public async Task<Ipv4ConflictProbeResult> ProbeAsync(
        string normalizedIpv4Address,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedIpv4Address);
        cancellationToken.ThrowIfCancellationRequested();

        if (!IPAddress.TryParse(normalizedIpv4Address, out IPAddress? address) ||
            address.AddressFamily != AddressFamily.InterNetwork)
        {
            return new Ipv4ConflictProbeResult(Ipv4ConflictProbeStatus.Unavailable);
        }

        using Ping ping = new();

        try
        {
            PingReply reply = await ping
                .SendPingAsync(address, _timeoutMilliseconds)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Ipv4ConflictProbeResult(
                reply.Status == IPStatus.Success
                    ? Ipv4ConflictProbeStatus.ResponseObserved
                    : Ipv4ConflictProbeStatus.NoResponse);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new Ipv4ConflictProbeResult(Ipv4ConflictProbeStatus.Cancelled);
        }
        catch (PingException)
        {
            return new Ipv4ConflictProbeResult(Ipv4ConflictProbeStatus.Unavailable);
        }
        catch (InvalidOperationException)
        {
            return new Ipv4ConflictProbeResult(Ipv4ConflictProbeStatus.Unavailable);
        }
    }
}
