using System.Net;
using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>Executes bounded, adapter-scoped network utility actions.</summary>
public interface IQuickNetworkActionService
{
    /// <summary>Pings an IPv4 default gateway using the configured bounded probe.</summary>
    Task<GatewayPingResult> PingGatewayAsync(IPAddress gateway, CancellationToken cancellationToken);

    /// <summary>Flushes the Windows DNS resolver cache for the whole system.</summary>
    Task<QuickNetworkActionResult> FlushDnsCacheAsync(CancellationToken cancellationToken);

    /// <summary>Releases and renews the DHCP lease for one adapter.</summary>
    Task<QuickNetworkActionResult> ReleaseRenewAsync(
        NetworkAdapterId adapterId,
        CancellationToken cancellationToken);
}

/// <summary>Outcome common to quick Windows networking actions.</summary>
public sealed record QuickNetworkActionResult(
    bool IsSuccess,
    uint? ErrorCode = null,
    bool IsAdapterUnavailable = false,
    bool IsAccessDenied = false);

/// <summary>Bounded ICMP statistics for an IPv4 gateway.</summary>
public sealed record GatewayPingResult(
    int Sent,
    int Received,
    long? MinimumRoundtripTimeMilliseconds,
    long? AverageRoundtripTimeMilliseconds,
    long? MaximumRoundtripTimeMilliseconds,
    uint? ErrorCode = null)
{
    /// <summary>Returns packet loss as a percentage from zero to 100.</summary>
    public int PacketLossPercent => Sent == 0 ? 100 : (Sent - Received) * 100 / Sent;

    /// <summary>True when at least one echo reply was received.</summary>
    public bool IsSuccessful => Received > 0;
}
