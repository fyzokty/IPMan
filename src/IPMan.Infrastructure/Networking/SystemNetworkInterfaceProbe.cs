using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;

namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Reads adapter data through <see cref="NetworkInterface"/> (ADR-009).
/// No command-line tooling and no WMI is involved.
/// <para>
/// This type is only meaningful against a live Windows network stack, so its
/// behaviour is validated by manual/integration verification rather than unit
/// tests; the deterministic logic lives in <see cref="NetworkAdapterMapper"/>.
/// </para>
/// </summary>
public sealed partial class SystemNetworkInterfaceProbe : IAdapterProbe
{
    private readonly ILogger<SystemNetworkInterfaceProbe> _logger;

    public SystemNetworkInterfaceProbe(ILogger<SystemNetworkInterfaceProbe> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public IReadOnlyList<AdapterReadModel> ReadAdapters()
    {
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        List<AdapterReadModel> adapters = new(interfaces.Length);

        foreach (NetworkInterface networkInterface in interfaces)
        {
            AdapterReadModel? adapter = TryRead(networkInterface);

            if (adapter is not null)
            {
                adapters.Add(adapter);
            }
        }

        return adapters;
    }

    private AdapterReadModel? TryRead(NetworkInterface networkInterface)
    {
        try
        {
            IPInterfaceProperties properties = networkInterface.GetIPProperties();

            return new AdapterReadModel(
                networkInterface.Id,
                networkInterface.Name,
                networkInterface.Description,
                ReadPhysicalAddress(networkInterface),
                networkInterface.OperationalStatus,
                networkInterface.NetworkInterfaceType,
                ReadSpeed(networkInterface),
                ReadIsDhcpEnabled(properties),
                ReadUnicastAddresses(properties),
                properties.GatewayAddresses.Select(gateway => gateway.Address.ToString()).ToArray(),
                properties.DnsAddresses.Select(server => server.ToString()).ToArray());
        }
        catch (NetworkInformationException exception)
        {
            // A single adapter that cannot be read must not fail discovery of the rest.
            LogAdapterUnreadable(exception, networkInterface.Id);
            return null;
        }
        catch (PlatformNotSupportedException exception)
        {
            LogAdapterUnsupported(exception, networkInterface.Id);
            return null;
        }
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Adapter {adapterId} could not be read and was skipped.")]
    private partial void LogAdapterUnreadable(Exception exception, string adapterId);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Adapter {adapterId} exposes properties unsupported on this platform and was skipped.")]
    private partial void LogAdapterUnsupported(Exception exception, string adapterId);

    private static byte[] ReadPhysicalAddress(NetworkInterface networkInterface)
    {
        try
        {
            return networkInterface.GetPhysicalAddress().GetAddressBytes();
        }
        catch (NetworkInformationException)
        {
            return Array.Empty<byte>();
        }
    }

    private static long ReadSpeed(NetworkInterface networkInterface)
    {
        try
        {
            return networkInterface.Speed;
        }
        catch (NetworkInformationException)
        {
            return -1;
        }
        catch (PlatformNotSupportedException)
        {
            return -1;
        }
    }

    private static bool? ReadIsDhcpEnabled(IPInterfaceProperties properties)
    {
        try
        {
            return properties.GetIPv4Properties()?.IsDhcpEnabled;
        }
        catch (NetworkInformationException)
        {
            return null;
        }
        catch (PlatformNotSupportedException)
        {
            return null;
        }
    }

    private static List<AdapterUnicastAddressReadModel> ReadUnicastAddresses(
        IPInterfaceProperties properties)
    {
        List<AdapterUnicastAddressReadModel> addresses = new(properties.UnicastAddresses.Count);

        foreach (UnicastIPAddressInformation address in properties.UnicastAddresses)
        {
            addresses.Add(new AdapterUnicastAddressReadModel(
                address.Address.ToString(),
                address.Address.AddressFamily,
                ReadIpv4Mask(address)));
        }

        return addresses;
    }

    private static string? ReadIpv4Mask(UnicastIPAddressInformation address)
    {
        try
        {
            return address.IPv4Mask?.ToString();
        }
        catch (NotImplementedException)
        {
            // Documented behaviour for non-IPv4 addresses on some platforms.
            return null;
        }
        catch (PlatformNotSupportedException)
        {
            return null;
        }
    }
}
