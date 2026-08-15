using System.Globalization;
using System.Management;
using System.Net;
using System.Net.Sockets;

namespace IPMan.Infrastructure.Networking;

internal interface IWindowsDnsClientServerAddressReader
{
    DnsClientServerAddressReadResult Read(WindowsInterfaceIdentity identity);
}

internal interface IWindowsDnsClientServerAddressProvider
{
    DnsClientServerAddressProviderReadResult Enumerate(uint interfaceIndex);
}

internal sealed record DnsClientServerAddressRawRecord(
    object? InterfaceIndex,
    object? InterfaceAlias,
    object? AddressFamily,
    object? ServerAddresses);

internal sealed record DnsClientServerAddressProviderReadResult(
    DnsTruthSourceReadStatus Status,
    uint? TechnicalCode,
    IReadOnlyList<DnsClientServerAddressRawRecord> Records)
{
    public bool IsSuccess => Status == DnsTruthSourceReadStatus.Success;

    public static DnsClientServerAddressProviderReadResult Success(
        IReadOnlyList<DnsClientServerAddressRawRecord> records) =>
        new(DnsTruthSourceReadStatus.Success, null, records);

    public static DnsClientServerAddressProviderReadResult Failure(
        DnsTruthSourceReadStatus status,
        uint? technicalCode = null) =>
        new(status, technicalCode, Array.Empty<DnsClientServerAddressRawRecord>());
}

internal sealed class WindowsDnsClientServerAddressReader : IWindowsDnsClientServerAddressReader
{
    internal const ushort AfInet = 2;
    internal const ushort AfInet6 = 23;
    internal const uint ErrorInvalidData = 13;

    private readonly IWindowsDnsClientServerAddressProvider _provider;

    public WindowsDnsClientServerAddressReader()
        : this(new SystemWindowsDnsClientServerAddressProvider())
    {
    }

    internal WindowsDnsClientServerAddressReader(
        IWindowsDnsClientServerAddressProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    public DnsClientServerAddressReadResult Read(WindowsInterfaceIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        DnsClientServerAddressProviderReadResult providerResult =
            _provider.Enumerate(identity.InterfaceIndex);

        if (!providerResult.IsSuccess)
        {
            return Failure(
                providerResult.Status,
                providerResult.TechnicalCode,
                identity.InterfaceIndex);
        }

        DnsClientServerAddressFamilyReadResult? ipv4 = null;
        DnsClientServerAddressFamilyReadResult? ipv6 = null;

        foreach (DnsClientServerAddressRawRecord record in providerResult.Records)
        {
            if (!TryConvertUInt32(record.InterfaceIndex, out uint interfaceIndex) ||
                interfaceIndex != identity.InterfaceIndex)
            {
                return Failure(
                    DnsTruthSourceReadStatus.IdentityMismatch,
                    ErrorInvalidData,
                    identity.InterfaceIndex);
            }

            if (!TryConvertUInt16(record.AddressFamily, out ushort family))
            {
                return Failure(
                    DnsTruthSourceReadStatus.InvalidData,
                    ErrorInvalidData,
                    identity.InterfaceIndex);
            }

            if (family is not AfInet and not AfInet6)
            {
                return Failure(
                    DnsTruthSourceReadStatus.UnsupportedAddressFamily,
                    ErrorInvalidData,
                    identity.InterfaceIndex);
            }

            if (record.InterfaceAlias is not null && record.InterfaceAlias is not string)
            {
                return Failure(
                    DnsTruthSourceReadStatus.InvalidData,
                    ErrorInvalidData,
                    identity.InterfaceIndex);
            }

            if (!TryReadServers(record.ServerAddresses, family, out string[] servers))
            {
                return Failure(
                    DnsTruthSourceReadStatus.InvalidData,
                    ErrorInvalidData,
                    identity.InterfaceIndex);
            }

            DnsClientServerAddressFamilyReadResult parsed = new(
                DnsTruthSourceReadStatus.Success,
                TechnicalCode: null,
                interfaceIndex,
                record.InterfaceAlias as string,
                family,
                servers);

            if (family == AfInet)
            {
                if (ipv4 is not null)
                {
                    ipv4 = FamilyFailure(
                        DnsTruthSourceReadStatus.AmbiguousTarget,
                        ErrorInvalidData,
                        identity.InterfaceIndex,
                        AfInet);
                }
                else
                {
                    ipv4 = parsed;
                }
            }
            else if (ipv6 is not null)
            {
                ipv6 = FamilyFailure(
                    DnsTruthSourceReadStatus.AmbiguousTarget,
                    ErrorInvalidData,
                    identity.InterfaceIndex,
                    AfInet6);
            }
            else
            {
                ipv6 = parsed;
            }
        }

        return new DnsClientServerAddressReadResult(
            ipv4 ?? FamilyFailure(
                DnsTruthSourceReadStatus.TargetMissing,
                technicalCode: null,
                identity.InterfaceIndex,
                AfInet),
            ipv6 ?? FamilyFailure(
                DnsTruthSourceReadStatus.TargetMissing,
                technicalCode: null,
                identity.InterfaceIndex,
                AfInet6));
    }

    private static DnsClientServerAddressReadResult Failure(
        DnsTruthSourceReadStatus status,
        uint? technicalCode,
        uint interfaceIndex) =>
        new(
            FamilyFailure(status, technicalCode, interfaceIndex, AfInet),
            FamilyFailure(status, technicalCode, interfaceIndex, AfInet6));

    private static DnsClientServerAddressFamilyReadResult FamilyFailure(
        DnsTruthSourceReadStatus status,
        uint? technicalCode,
        uint interfaceIndex,
        ushort family) =>
        new(
            status,
            technicalCode,
            interfaceIndex,
            InterfaceAlias: null,
            family,
            Array.Empty<string>());

    private static bool TryReadServers(
        object? rawServers,
        ushort family,
        out string[] servers)
    {
        if (rawServers is null)
        {
            servers = Array.Empty<string>();
            return true;
        }

        if (rawServers is not string[] values)
        {
            servers = Array.Empty<string>();
            return false;
        }

        AddressFamily expectedFamily = family == AfInet
            ? AddressFamily.InterNetwork
            : AddressFamily.InterNetworkV6;
        List<string> parsed = new(values.Length);

        foreach (string? value in values)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !IPAddress.TryParse(value, out IPAddress? address) ||
                address.AddressFamily != expectedFamily)
            {
                servers = Array.Empty<string>();
                return false;
            }

            parsed.Add(address.ToString());
        }

        servers = parsed.ToArray();
        return true;
    }

    private static bool TryConvertUInt32(object? value, out uint result)
    {
        try
        {
            result = Convert.ToUInt32(value, CultureInfo.InvariantCulture);
            return value is not null;
        }
        catch (Exception exception) when (
            exception is FormatException or InvalidCastException or OverflowException)
        {
            result = 0;
            return false;
        }
    }

    private static bool TryConvertUInt16(object? value, out ushort result)
    {
        try
        {
            result = Convert.ToUInt16(value, CultureInfo.InvariantCulture);
            return value is not null;
        }
        catch (Exception exception) when (
            exception is FormatException or InvalidCastException or OverflowException)
        {
            result = 0;
            return false;
        }
    }
}

internal sealed class SystemWindowsDnsClientServerAddressProvider :
    IWindowsDnsClientServerAddressProvider
{
    private const string ScopePath = @"\\.\root\StandardCimv2";

    internal static string BuildQuery(uint interfaceIndex) =>
        "SELECT InterfaceIndex, InterfaceAlias, AddressFamily, ServerAddresses " +
        "FROM MSFT_DNSClientServerAddress WHERE InterfaceIndex = " +
        interfaceIndex.ToString(CultureInfo.InvariantCulture);

    public DnsClientServerAddressProviderReadResult Enumerate(uint interfaceIndex)
    {
        try
        {
            ManagementScope scope = new(ScopePath);
            ObjectQuery query = new(BuildQuery(interfaceIndex));
            using ManagementObjectSearcher searcher = new(scope, query);
            using ManagementObjectCollection instances = searcher.Get();
            List<DnsClientServerAddressRawRecord> records = new();

            foreach (ManagementObject instance in instances)
            {
                using (instance)
                {
                    records.Add(new DnsClientServerAddressRawRecord(
                        instance["InterfaceIndex"],
                        instance["InterfaceAlias"],
                        instance["AddressFamily"],
                        instance["ServerAddresses"]));
                }
            }

            return DnsClientServerAddressProviderReadResult.Success(records);
        }
        catch (ManagementException exception)
        {
            return DnsClientServerAddressProviderReadResult.Failure(
                exception.ErrorCode == ManagementStatus.AccessDenied
                    ? DnsTruthSourceReadStatus.AccessDenied
                    : DnsTruthSourceReadStatus.ProviderReadFailed,
                unchecked((uint)(int)exception.ErrorCode));
        }
        catch (UnauthorizedAccessException)
        {
            return DnsClientServerAddressProviderReadResult.Failure(
                DnsTruthSourceReadStatus.AccessDenied);
        }
    }
}
