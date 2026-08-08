using System.Net;
using System.Runtime.InteropServices;

namespace IPMan.IntegrationTests.Observation;

/// <summary>Read-only exact-interface IPv6 route observation through IP Helper.</summary>
public static class Ipv6RouteTableObserver
{
    private const ushort AfInet6 = 23;
    private const uint NoError = 0;
    private const uint ErrorNotFound = 1168;
    private const uint MaximumReasonableRouteCount = 65_536;

    public static Ipv6RouteTableObservation NotApplicable() =>
        new(
            Supported: true,
            Complete: true,
            NativeError: null,
            InterfaceIndex: null,
            Array.Empty<Ipv6RouteObservation>());

    public static Ipv6RouteTableObservation Read(uint? interfaceIndex)
    {
        if (interfaceIndex is null)
        {
            return Incomplete(null, null);
        }

        IntPtr table = IntPtr.Zero;

        try
        {
            uint result = GetIpForwardTable2(AfInet6, out table);

            if (result == ErrorNotFound)
            {
                return new Ipv6RouteTableObservation(
                    Supported: true,
                    Complete: true,
                    NativeError: null,
                    interfaceIndex,
                    Array.Empty<Ipv6RouteObservation>());
            }

            if (result != NoError || table == IntPtr.Zero)
            {
                return Incomplete(result, interfaceIndex);
            }

            return ReadTable(table, interfaceIndex.Value);
        }
        catch (DllNotFoundException)
        {
            return Incomplete(uint.MaxValue, interfaceIndex);
        }
        catch (EntryPointNotFoundException)
        {
            return Incomplete(uint.MaxValue, interfaceIndex);
        }
        finally
        {
            if (table != IntPtr.Zero)
            {
                FreeMibTable(table);
            }
        }
    }

    private static Ipv6RouteTableObservation ReadTable(IntPtr table, uint interfaceIndex)
    {
        uint count = unchecked((uint)Marshal.ReadInt32(table));

        if (count > MaximumReasonableRouteCount)
        {
            return Incomplete(uint.MaxValue, interfaceIndex);
        }

        int rowOffset = Marshal.OffsetOf<MibIpForwardTable2Header>(
            nameof(MibIpForwardTable2Header.FirstRow)).ToInt32();
        int rowSize = Marshal.SizeOf<MibIpForwardRow2>();
        List<Ipv6RouteObservation> routes = new();

        for (uint index = 0; index < count; index++)
        {
            IntPtr rowPointer = IntPtr.Add(
                table,
                checked(rowOffset + checked((int)index * rowSize)));
            MibIpForwardRow2 row = Marshal.PtrToStructure<MibIpForwardRow2>(rowPointer);

            if (row.InterfaceIndex != interfaceIndex ||
                row.DestinationPrefix.Prefix.Family != AfInet6)
            {
                continue;
            }

            routes.Add(new Ipv6RouteObservation(
                ReadIpv6(row.DestinationPrefix.Prefix),
                row.DestinationPrefix.PrefixLength,
                ReadIpv6(row.NextHop),
                row.InterfaceIndex,
                row.InterfaceLuid,
                row.SitePrefixLength,
                row.Metric,
                row.Protocol,
                row.Loopback != 0,
                row.AutoconfigureAddress != 0,
                row.Publish != 0,
                row.Immortal != 0,
                row.Origin));
        }

        return new Ipv6RouteTableObservation(
            Supported: true,
            Complete: true,
            NativeError: null,
            interfaceIndex,
            routes);
    }

    private static string ReadIpv6(SockaddrInet address)
    {
        byte[] bytes = new byte[16];
        BitConverter.GetBytes(address.AddressPart1).CopyTo(bytes, 0);
        BitConverter.GetBytes(address.AddressPart2).CopyTo(bytes, 8);
        return new IPAddress(bytes, address.ScopeId).ToString();
    }

    private static Ipv6RouteTableObservation Incomplete(
        uint? error,
        uint? interfaceIndex) =>
        new(
            Supported: false,
            Complete: false,
            NativeError: error,
            interfaceIndex,
            Array.Empty<Ipv6RouteObservation>());

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint GetIpForwardTable2(ushort family, out IntPtr table);

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern void FreeMibTable(IntPtr memory);

    [StructLayout(LayoutKind.Explicit, Size = 112)]
    private struct MibIpForwardTable2Header
    {
        [FieldOffset(0)]
        public uint NumEntries;

        [FieldOffset(8)]
        public MibIpForwardRow2 FirstRow;
    }

    [StructLayout(LayoutKind.Explicit, Size = 104)]
    private struct MibIpForwardRow2
    {
        [FieldOffset(0)]
        public ulong InterfaceLuid;

        [FieldOffset(8)]
        public uint InterfaceIndex;

        [FieldOffset(12)]
        public IpAddressPrefix DestinationPrefix;

        [FieldOffset(44)]
        public SockaddrInet NextHop;

        [FieldOffset(72)]
        public byte SitePrefixLength;

        [FieldOffset(76)]
        public uint ValidLifetime;

        [FieldOffset(80)]
        public uint PreferredLifetime;

        [FieldOffset(84)]
        public uint Metric;

        [FieldOffset(88)]
        public int Protocol;

        [FieldOffset(92)]
        public byte Loopback;

        [FieldOffset(93)]
        public byte AutoconfigureAddress;

        [FieldOffset(94)]
        public byte Publish;

        [FieldOffset(95)]
        public byte Immortal;

        [FieldOffset(96)]
        public uint Age;

        [FieldOffset(100)]
        public int Origin;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct IpAddressPrefix
    {
        [FieldOffset(0)]
        public SockaddrInet Prefix;

        [FieldOffset(28)]
        public byte PrefixLength;
    }

    [StructLayout(LayoutKind.Explicit, Size = 28)]
    private struct SockaddrInet
    {
        [FieldOffset(0)]
        public ushort Family;

        [FieldOffset(8)]
        public ulong AddressPart1;

        [FieldOffset(16)]
        public ulong AddressPart2;

        [FieldOffset(24)]
        public uint ScopeId;
    }
}
