using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace IPMan.Infrastructure.Networking;

internal interface IGetIfTable2NativeApi
{
    uint Get(out IntPtr table);

    void Free(IntPtr table);
}

/// <summary>Reads NDIS physical media by interface GUID without changing network state.</summary>
internal sealed class WindowsAdapterMediumReader
{
    private const uint MaximumTableEntries = 65_536;
    private readonly IGetIfTable2NativeApi _nativeApi;

    public WindowsAdapterMediumReader()
        : this(new GetIfTable2NativeApi())
    {
    }

    internal WindowsAdapterMediumReader(IGetIfTable2NativeApi nativeApi)
    {
        ArgumentNullException.ThrowIfNull(nativeApi);
        _nativeApi = nativeApi;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Optional native medium discovery must never fail adapter discovery.")]
    public IReadOnlyDictionary<Guid, uint> Read()
    {
        try
        {
            IntPtr table = IntPtr.Zero;
            try
            {
                if (_nativeApi.Get(out table) != 0 || table == IntPtr.Zero)
                {
                    return new Dictionary<Guid, uint>();
                }

                uint count = unchecked((uint)Marshal.ReadInt32(table));
                if (count > MaximumTableEntries)
                {
                    return new Dictionary<Guid, uint>();
                }

                int firstRowOffset = Marshal.OffsetOf<MibIfTable2>(nameof(MibIfTable2.Table)).ToInt32();
                int rowSize = Marshal.SizeOf<MibIfRow2>();
                int guidOffset = Marshal.OffsetOf<MibIfRow2>(nameof(MibIfRow2.InterfaceGuid)).ToInt32();
                int mediumOffset = Marshal.OffsetOf<MibIfRow2>(nameof(MibIfRow2.PhysicalMediumType)).ToInt32();
                Dictionary<Guid, uint> media = new();

                for (int index = 0; index < count; index++)
                {
                    IntPtr row = IntPtr.Add(table, checked(firstRowOffset + (index * rowSize)));
                    Guid interfaceGuid = Marshal.PtrToStructure<Guid>(IntPtr.Add(row, guidOffset));
                    if (interfaceGuid == Guid.Empty)
                    {
                        continue;
                    }

                    uint medium = unchecked((uint)Marshal.ReadInt32(row, mediumOffset));
                    if (!media.TryGetValue(interfaceGuid, out uint existingMedium) || existingMedium == 0)
                    {
                        media[interfaceGuid] = medium;
                    }
                }

                return media;
            }
            finally
            {
                if (table != IntPtr.Zero)
                {
                    _nativeApi.Free(table);
                }
            }
        }
        catch (Exception)
        {
            return new Dictionary<Guid, uint>();
        }
    }

    public static uint GetMedium(IReadOnlyDictionary<Guid, uint> media, string adapterId) =>
        Guid.TryParse(adapterId, out Guid interfaceGuid) && media.TryGetValue(interfaceGuid, out uint medium)
            ? medium
            : 0;

    // netioapi.h: WCHAR[IF_MAX_STRING_SIZE + 1] and BYTE[IF_MAX_PHYS_ADDRESS_LENGTH].
    // Keep the full sequential layout so both row stride and field offsets follow native alignment.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct MibIfRow2
    {
        public ulong InterfaceLuid;
        public uint InterfaceIndex;
        public Guid InterfaceGuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)] public string Alias;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)] public string Description;
        public uint PhysicalAddressLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] PhysicalAddress;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] PermanentPhysicalAddress;
        public uint Mtu;
        public uint Type;
        public uint TunnelType;
        public uint MediaType;
        public uint PhysicalMediumType;
        public uint AccessType;
        public uint DirectionType;
        public byte InterfaceAndOperStatusFlags;
        public uint OperStatus;
        public uint AdminStatus;
        public uint MediaConnectState;
        public Guid NetworkGuid;
        public uint ConnectionType;
        public ulong TransmitLinkSpeed;
        public ulong ReceiveLinkSpeed;
        public ulong InOctets;
        public ulong InUcastPkts;
        public ulong InNUcastPkts;
        public ulong InDiscards;
        public ulong InErrors;
        public ulong InUnknownProtos;
        public ulong InUcastOctets;
        public ulong InMulticastOctets;
        public ulong InBroadcastOctets;
        public ulong OutOctets;
        public ulong OutUcastPkts;
        public ulong OutNUcastPkts;
        public ulong OutDiscards;
        public ulong OutErrors;
        public ulong OutUcastOctets;
        public ulong OutMulticastOctets;
        public ulong OutBroadcastOctets;
        public ulong OutQLen;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct MibIfTable2
    {
        public uint NumEntries;
        public MibIfRow2 Table;
    }
}

internal sealed class GetIfTable2NativeApi : IGetIfTable2NativeApi
{
    public uint Get(out IntPtr table) => GetIfTable2(out table);

    public void Free(IntPtr table) => FreeMibTable(table);

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint GetIfTable2(out IntPtr table);

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern void FreeMibTable(IntPtr memory);
}
