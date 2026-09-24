using System.ComponentModel;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Infrastructure.Networking;

/// <summary>Windows implementations for the quick adapter actions.</summary>
public sealed class WindowsQuickNetworkActionService : IQuickNetworkActionService
{
    private const int EchoCount = 4;
    private const int TotalTimeoutMilliseconds = 15_000;
    private const string ReleaseDhcpLeaseMethod = "ReleaseDHCPLease";
    private const string RenewDhcpLeaseMethod = "RenewDHCPLease";

    private readonly IWmiNetworkAdapterSessionFactory _sessionFactory;

    /// <summary>Creates the Windows quick-action service.</summary>
    public WindowsQuickNetworkActionService()
        : this(new SystemWmiNetworkAdapterSessionFactory())
    {
    }

    internal WindowsQuickNetworkActionService(IWmiNetworkAdapterSessionFactory sessionFactory)
    {
        ArgumentNullException.ThrowIfNull(sessionFactory);
        _sessionFactory = sessionFactory;
    }

    /// <inheritdoc />
    public async Task<GatewayPingResult> PingGatewayAsync(
        IPAddress gateway,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(gateway);

        int perEchoTimeout = TotalTimeoutMilliseconds / EchoCount;
        List<long> roundtrips = new(EchoCount);
        uint? errorCode = null;

        using Ping ping = new();
        for (int index = 0; index < EchoCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                PingReply reply = await ping
                    .SendPingAsync(gateway, perEchoTimeout)
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (reply.Status == IPStatus.Success)
                {
                    roundtrips.Add(reply.RoundtripTime);
                }
            }
            catch (PingException exception) when (exception.InnerException is Win32Exception win32)
            {
                errorCode ??= unchecked((uint)win32.NativeErrorCode);
            }
            catch (SocketException exception)
            {
                errorCode ??= unchecked((uint)exception.ErrorCode);
            }
        }

        return new GatewayPingResult(
            EchoCount,
            roundtrips.Count,
            roundtrips.Count == 0 ? null : roundtrips.Min(),
            roundtrips.Count == 0 ? null : (long)Math.Round(roundtrips.Average(), MidpointRounding.AwayFromZero),
            roundtrips.Count == 0 ? null : roundtrips.Max(),
            errorCode);
    }

    /// <inheritdoc />
    public Task<QuickNetworkActionResult> FlushDnsCacheAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool succeeded = DnsFlushResolverCache();
        uint? errorCode = succeeded ? null : (uint)Marshal.GetLastWin32Error();
        return Task.FromResult(new QuickNetworkActionResult(
            succeeded,
            errorCode,
            IsAccessDenied: errorCode == 5));
    }

    /// <inheritdoc />
    public Task<QuickNetworkActionResult> ReleaseRenewAsync(
        NetworkAdapterId adapterId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => ReleaseRenewCore(adapterId), CancellationToken.None);
    }

    private QuickNetworkActionResult ReleaseRenewCore(NetworkAdapterId adapterId)
    {
        try
        {
            WmiAdapterResolution resolution = _sessionFactory.ResolveBySettingId(adapterId.Value);
            if (resolution.Status != WmiAdapterResolutionStatus.Found || resolution.Session is null)
            {
                return new QuickNetworkActionResult(false, IsAdapterUnavailable: true);
            }

            using IWmiNetworkAdapterSession session = resolution.Session;
            uint releaseCode = session.Invoke(ReleaseDhcpLeaseMethod, null);
            if (releaseCode != 0 && releaseCode != 1)
            {
                return FromWmiCode(releaseCode);
            }

            uint renewCode = session.Invoke(RenewDhcpLeaseMethod, null);
            if (renewCode == 0 || renewCode == 1)
            {
                return new QuickNetworkActionResult(true);
            }

            // Windows will request the previous lease when available. A single
            // bounded retry protects DHCP mode without writing snapshot values.
            renewCode = session.Invoke(RenewDhcpLeaseMethod, null);
            return renewCode is 0 or 1
                ? new QuickNetworkActionResult(true)
                : FromWmiCode(renewCode);
        }
        catch (ManagementException)
        {
            return new QuickNetworkActionResult(false, IsAdapterUnavailable: true);
        }
        catch (UnauthorizedAccessException)
        {
            return new QuickNetworkActionResult(false, 5, IsAccessDenied: true);
        }
    }

    private static QuickNetworkActionResult FromWmiCode(uint code) =>
        new(false, code, IsAccessDenied: code == 91);

    [DllImport("dnsapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DnsFlushResolverCache();
}
