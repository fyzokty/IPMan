using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace IPMan.Infrastructure.Networking;

internal interface IWindowsAdaptersAddressesDnsReader
{
    AdaptersAddressesDnsReadResult Read(WindowsInterfaceIdentity identity);
}

internal interface IWindowsAdaptersAddressesProvider
{
    WindowsAdaptersAddressesProviderReadResult Enumerate();
}

internal interface IGetAdaptersAddressesNativeApi
{
    uint Get(uint family, uint flags, IntPtr addresses, ref uint bufferSize);
}

internal sealed record WindowsNativeDnsAddress(
    DnsTruthSourceReadStatus Status,
    ushort? AddressFamily,
    string? Address);

internal sealed record WindowsAdapterDnsRecord(
    Guid? InterfaceGuid,
    ulong Luid,
    uint InterfaceIndex,
    IReadOnlyList<WindowsNativeDnsAddress> DnsAddresses);

internal sealed record WindowsAdaptersAddressesProviderReadResult(
    DnsTruthSourceReadStatus Status,
    uint? NativeResult,
    IReadOnlyList<WindowsAdapterDnsRecord> Adapters)
{
    public bool IsSuccess => Status == DnsTruthSourceReadStatus.Success;

    public static WindowsAdaptersAddressesProviderReadResult Success(
        IReadOnlyList<WindowsAdapterDnsRecord> adapters) =>
        new(DnsTruthSourceReadStatus.Success, 0, adapters);

    public static WindowsAdaptersAddressesProviderReadResult Failure(
        DnsTruthSourceReadStatus status,
        uint? nativeResult = null) =>
        new(status, nativeResult, Array.Empty<WindowsAdapterDnsRecord>());
}

internal sealed class WindowsAdaptersAddressesDnsReader : IWindowsAdaptersAddressesDnsReader
{
    internal const ushort AfInet = 2;
    internal const ushort AfInet6 = 23;

    private readonly IWindowsAdaptersAddressesProvider _provider;

    public WindowsAdaptersAddressesDnsReader()
        : this(new SystemWindowsAdaptersAddressesProvider())
    {
    }

    internal WindowsAdaptersAddressesDnsReader(IWindowsAdaptersAddressesProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    public AdaptersAddressesDnsReadResult Read(WindowsInterfaceIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        WindowsAdaptersAddressesProviderReadResult providerResult = _provider.Enumerate();

        if (!providerResult.IsSuccess)
        {
            return Failure(
                providerResult.Status,
                providerResult.NativeResult,
                exactIdentityMatches: false);
        }

        WindowsAdapterDnsRecord[] related = providerResult.Adapters
            .Where(adapter =>
                adapter.InterfaceGuid == identity.InterfaceGuid ||
                adapter.Luid == identity.Luid ||
                adapter.InterfaceIndex == identity.InterfaceIndex)
            .ToArray();
        WindowsAdapterDnsRecord[] exact = related
            .Where(adapter =>
                adapter.InterfaceGuid == identity.InterfaceGuid &&
                adapter.Luid == identity.Luid &&
                adapter.InterfaceIndex == identity.InterfaceIndex)
            .ToArray();

        if (related.Any(adapter => !exact.Contains(adapter)))
        {
            return Failure(
                DnsTruthSourceReadStatus.IdentityMismatch,
                providerResult.NativeResult,
                exactIdentityMatches: false);
        }

        if (exact.Length == 0)
        {
            return Failure(
                DnsTruthSourceReadStatus.TargetMissing,
                providerResult.NativeResult,
                exactIdentityMatches: false);
        }

        if (exact.Length != 1)
        {
            return Failure(
                DnsTruthSourceReadStatus.AmbiguousTarget,
                providerResult.NativeResult,
                exactIdentityMatches: false);
        }

        List<string> ipv4 = new();
        List<string> ipv6 = new();

        foreach (WindowsNativeDnsAddress address in exact[0].DnsAddresses)
        {
            if (address.Status != DnsTruthSourceReadStatus.Success ||
                address.AddressFamily is null ||
                address.Address is null)
            {
                return Failure(
                    address.Status == DnsTruthSourceReadStatus.Success
                        ? DnsTruthSourceReadStatus.InvalidData
                        : address.Status,
                    providerResult.NativeResult,
                    exactIdentityMatches: true);
            }

            if (address.AddressFamily == AfInet)
            {
                ipv4.Add(address.Address);
            }
            else if (address.AddressFamily == AfInet6)
            {
                ipv6.Add(address.Address);
            }
            else
            {
                return Failure(
                    DnsTruthSourceReadStatus.UnsupportedAddressFamily,
                    providerResult.NativeResult,
                    exactIdentityMatches: true);
            }
        }

        return new AdaptersAddressesDnsReadResult(
            Success(AfInet, ipv4),
            Success(AfInet6, ipv6));
    }

    private static AdaptersAddressesDnsReadResult Failure(
        DnsTruthSourceReadStatus status,
        uint? nativeResult,
        bool exactIdentityMatches) =>
        new(
            FamilyFailure(status, nativeResult, exactIdentityMatches, AfInet),
            FamilyFailure(status, nativeResult, exactIdentityMatches, AfInet6));

    private static AdaptersAddressesDnsFamilyReadResult FamilyFailure(
        DnsTruthSourceReadStatus status,
        uint? nativeResult,
        bool exactIdentityMatches,
        ushort family) =>
        new(
            status,
            nativeResult,
            exactIdentityMatches,
            family,
            Array.Empty<string>());

    private static AdaptersAddressesDnsFamilyReadResult Success(
        ushort family,
        IReadOnlyList<string> servers) =>
        new(
            DnsTruthSourceReadStatus.Success,
            NativeResult: 0,
            ExactIdentityMatches: true,
            family,
            servers);
}

internal sealed class SystemWindowsAdaptersAddressesProvider : IWindowsAdaptersAddressesProvider
{
    internal const uint ErrorBufferOverflow = 111;
    internal const uint ErrorNoData = 232;
    internal const uint ErrorNotSupported = 50;
    internal const int IpAdapterAddressesMinimumLength = 232;
    internal const int DnsAddressNodeMinimumLength = 32;
    private const int MaximumReadAttempts = 3;
    private const int MaximumLinkedListEntries = 1024;
    private const int MaximumSockaddrLength = 128;
    private const uint AfUnspec = 0;
    private const uint Flags = 0;

    private readonly IGetAdaptersAddressesNativeApi _nativeApi;

    public SystemWindowsAdaptersAddressesProvider()
        : this(new GetAdaptersAddressesNativeApi())
    {
    }

    internal SystemWindowsAdaptersAddressesProvider(IGetAdaptersAddressesNativeApi nativeApi)
    {
        ArgumentNullException.ThrowIfNull(nativeApi);
        _nativeApi = nativeApi;
    }

    internal static bool IsSupportedArchitecture =>
        IntPtr.Size == 8 && RuntimeInformation.ProcessArchitecture == Architecture.X64;

    public WindowsAdaptersAddressesProviderReadResult Enumerate()
    {
        if (!IsSupportedArchitecture)
        {
            return WindowsAdaptersAddressesProviderReadResult.Failure(
                DnsTruthSourceReadStatus.UnsupportedArchitecture,
                ErrorNotSupported);
        }

        try
        {
            return EnumerateNative();
        }
        catch (DllNotFoundException)
        {
            return WindowsAdaptersAddressesProviderReadResult.Failure(
                DnsTruthSourceReadStatus.NativeLibraryUnavailable);
        }
        catch (EntryPointNotFoundException)
        {
            return WindowsAdaptersAddressesProviderReadResult.Failure(
                DnsTruthSourceReadStatus.NativeEntryPointUnavailable);
        }
        catch (Exception exception) when (
            exception is ArgumentException or ArithmeticException)
        {
            return WindowsAdaptersAddressesProviderReadResult.Failure(
                DnsTruthSourceReadStatus.InvalidData);
        }
    }

    private WindowsAdaptersAddressesProviderReadResult EnumerateNative()
    {
        uint bufferSize = 0;
        uint result = _nativeApi.Get(AfUnspec, Flags, IntPtr.Zero, ref bufferSize);

        if (result == ErrorNoData || (result == 0 && bufferSize == 0))
        {
            return WindowsAdaptersAddressesProviderReadResult.Success(
                Array.Empty<WindowsAdapterDnsRecord>());
        }

        if (result != ErrorBufferOverflow || bufferSize == 0 || bufferSize > int.MaxValue)
        {
            return WindowsAdaptersAddressesProviderReadResult.Failure(
                DnsTruthSourceReadStatus.NativeCallFailed,
                result);
        }

        IntPtr buffer = IntPtr.Zero;

        try
        {
            for (int attempt = 0; attempt < MaximumReadAttempts; attempt++)
            {
                buffer = Marshal.AllocHGlobal(checked((int)bufferSize));
                result = _nativeApi.Get(AfUnspec, Flags, buffer, ref bufferSize);

                if (result == ErrorBufferOverflow)
                {
                    Marshal.FreeHGlobal(buffer);
                    buffer = IntPtr.Zero;

                    if (bufferSize == 0 || bufferSize > int.MaxValue)
                    {
                        return WindowsAdaptersAddressesProviderReadResult.Failure(
                            DnsTruthSourceReadStatus.InvalidData,
                            result);
                    }

                    continue;
                }

                if (result != 0)
                {
                    return WindowsAdaptersAddressesProviderReadResult.Failure(
                        DnsTruthSourceReadStatus.NativeCallFailed,
                        result);
                }

                return ParseAdapters(buffer);
            }

            return WindowsAdaptersAddressesProviderReadResult.Failure(
                DnsTruthSourceReadStatus.NativeCallFailed,
                ErrorBufferOverflow);
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private static WindowsAdaptersAddressesProviderReadResult ParseAdapters(IntPtr first)
    {
        List<WindowsAdapterDnsRecord> adapters = new();
        HashSet<IntPtr> visited = new();
        IntPtr current = first;

        while (current != IntPtr.Zero)
        {
            if (visited.Count >= MaximumLinkedListEntries || !visited.Add(current))
            {
                return WindowsAdaptersAddressesProviderReadResult.Failure(
                    DnsTruthSourceReadStatus.InvalidData,
                    nativeResult: 0);
            }

            IpAdapterAddressesHeader adapter =
                Marshal.PtrToStructure<IpAdapterAddressesHeader>(current);

            if (adapter.Length < IpAdapterAddressesMinimumLength)
            {
                return WindowsAdaptersAddressesProviderReadResult.Failure(
                    DnsTruthSourceReadStatus.InvalidData,
                    nativeResult: 0);
            }

            string? adapterName = Marshal.PtrToStringAnsi(adapter.AdapterName);
            Guid? interfaceGuid = Guid.TryParse(adapterName, out Guid parsedGuid)
                ? parsedGuid
                : null;
            DnsAddressListReadResult dnsAddresses = ReadDnsAddresses(
                adapter.FirstDnsServerAddress);

            if (!dnsAddresses.IsSuccess)
            {
                return WindowsAdaptersAddressesProviderReadResult.Failure(
                    DnsTruthSourceReadStatus.InvalidData,
                    nativeResult: 0);
            }

            adapters.Add(new WindowsAdapterDnsRecord(
                interfaceGuid,
                adapter.Luid,
                adapter.InterfaceIndex,
                dnsAddresses.Addresses));
            current = adapter.Next;
        }

        return WindowsAdaptersAddressesProviderReadResult.Success(adapters);
    }

    private static DnsAddressListReadResult ReadDnsAddresses(IntPtr first)
    {
        List<WindowsNativeDnsAddress> addresses = new();
        HashSet<IntPtr> visited = new();
        IntPtr current = first;

        while (current != IntPtr.Zero)
        {
            if (visited.Count >= MaximumLinkedListEntries || !visited.Add(current))
            {
                return new DnsAddressListReadResult(false, Array.Empty<WindowsNativeDnsAddress>());
            }

            IpAdapterDnsServerAddress node =
                Marshal.PtrToStructure<IpAdapterDnsServerAddress>(current);

            if (node.Length < DnsAddressNodeMinimumLength ||
                node.Sockaddr == IntPtr.Zero ||
                node.SockaddrLength is < 2 or > MaximumSockaddrLength)
            {
                addresses.Add(new WindowsNativeDnsAddress(
                    DnsTruthSourceReadStatus.InvalidData,
                    AddressFamily: null,
                    Address: null));
            }
            else
            {
                byte[] bytes = new byte[node.SockaddrLength];
                Marshal.Copy(node.Sockaddr, bytes, 0, bytes.Length);
                addresses.Add(ParseSockaddr(bytes));
            }

            current = node.Next;
        }

        return new DnsAddressListReadResult(true, addresses);
    }

    internal static WindowsNativeDnsAddress ParseSockaddr(byte[] sockaddr)
    {
        ArgumentNullException.ThrowIfNull(sockaddr);

        if (sockaddr.Length < 2)
        {
            return InvalidAddress();
        }

        ushort family = BitConverter.ToUInt16(sockaddr, 0);

        if (family == WindowsAdaptersAddressesDnsReader.AfInet)
        {
            if (sockaddr.Length < 16)
            {
                return InvalidAddress();
            }

            return new WindowsNativeDnsAddress(
                DnsTruthSourceReadStatus.Success,
                family,
                new IPAddress(sockaddr.AsSpan(4, 4)).ToString());
        }

        if (family == WindowsAdaptersAddressesDnsReader.AfInet6)
        {
            if (sockaddr.Length < 28)
            {
                return InvalidAddress();
            }

            long scopeId = BitConverter.ToUInt32(sockaddr, 24);
            return new WindowsNativeDnsAddress(
                DnsTruthSourceReadStatus.Success,
                family,
                new IPAddress(sockaddr.AsSpan(8, 16), scopeId).ToString());
        }

        return new WindowsNativeDnsAddress(
            DnsTruthSourceReadStatus.UnsupportedAddressFamily,
            family,
            Address: null);
    }

    private static WindowsNativeDnsAddress InvalidAddress() =>
        new(
            DnsTruthSourceReadStatus.InvalidData,
            AddressFamily: null,
            Address: null);

    private sealed record DnsAddressListReadResult(
        bool IsSuccess,
        IReadOnlyList<WindowsNativeDnsAddress> Addresses);

    [StructLayout(LayoutKind.Explicit, Size = IpAdapterAddressesMinimumLength)]
    internal struct IpAdapterAddressesHeader
    {
        [FieldOffset(0)] public uint Length;
        [FieldOffset(4)] public uint InterfaceIndex;
        [FieldOffset(8)] public IntPtr Next;
        [FieldOffset(16)] public IntPtr AdapterName;
        [FieldOffset(48)] public IntPtr FirstDnsServerAddress;
        [FieldOffset(224)] public ulong Luid;
    }

    [StructLayout(LayoutKind.Explicit, Size = DnsAddressNodeMinimumLength)]
    internal struct IpAdapterDnsServerAddress
    {
        [FieldOffset(0)] public uint Length;
        [FieldOffset(8)] public IntPtr Next;
        [FieldOffset(16)] public IntPtr Sockaddr;
        [FieldOffset(24)] public int SockaddrLength;
    }
}

internal sealed class GetAdaptersAddressesNativeApi : IGetAdaptersAddressesNativeApi
{
    public uint Get(uint family, uint flags, IntPtr addresses, ref uint bufferSize) =>
        GetAdaptersAddresses(family, flags, IntPtr.Zero, addresses, ref bufferSize);

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint GetAdaptersAddresses(
        uint family,
        uint flags,
        IntPtr reserved,
        IntPtr adapterAddresses,
        ref uint sizePointer);
}
