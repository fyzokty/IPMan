using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using EnumerationOptions = System.Management.EnumerationOptions;

namespace IPMan.Infrastructure.Networking;

public sealed class WindowsIpv4DefaultRouteManager : IIpv4DefaultRouteManager
{
    private readonly IWindowsInterfaceIdentityResolver _identityResolver;
    private readonly IWindowsPersistentRouteStore _persistentStore;
    private readonly IWindowsActiveRouteStore _activeStore;

    public WindowsIpv4DefaultRouteManager()
        : this(
            new SystemWindowsInterfaceIdentityResolver(),
            new SystemWindowsPersistentRouteStore(),
            new SystemWindowsActiveRouteStore())
    {
    }

    internal WindowsIpv4DefaultRouteManager(
        IWindowsInterfaceIdentityResolver identityResolver,
        IWindowsPersistentRouteStore persistentStore,
        IWindowsActiveRouteStore activeStore)
    {
        ArgumentNullException.ThrowIfNull(identityResolver);
        ArgumentNullException.ThrowIfNull(persistentStore);
        ArgumentNullException.ThrowIfNull(activeStore);
        _identityResolver = identityResolver;
        _persistentStore = persistentStore;
        _activeStore = activeStore;
    }

    public Ipv4DefaultRouteClearResult Clear(NetworkAdapterId adapterId)
    {
        InterfaceIdentityResolution identity = _identityResolver.Resolve(adapterId);

        if (!identity.IsSuccess)
        {
            return new Ipv4DefaultRouteClearResult(identity.Status, identity.TechnicalCode);
        }

        WindowsInterfaceIdentity exact = identity.Identity!;
        Ipv4DefaultRouteClearResult persistent = _persistentStore.Clear(exact);

        if (!persistent.IsSuccess)
        {
            return persistent;
        }

        InterfaceIdentityResolution beforeActive = _identityResolver.Revalidate(exact);

        if (!beforeActive.IsSuccess)
        {
            return IdentityFailure(beforeActive);
        }

        Ipv4DefaultRouteClearResult active = _activeStore.Clear(exact);

        if (!active.IsSuccess)
        {
            return active;
        }

        InterfaceIdentityResolution afterActive = _identityResolver.Revalidate(exact);
        return afterActive.IsSuccess
            ? Ipv4DefaultRouteClearResult.Success()
            : IdentityFailure(afterActive);
    }

    private static Ipv4DefaultRouteClearResult IdentityFailure(
        InterfaceIdentityResolution resolution) =>
        new(resolution.Status, resolution.TechnicalCode);
}

internal sealed record WindowsInterfaceIdentity(Guid InterfaceGuid, ulong Luid, uint InterfaceIndex);

internal sealed record InterfaceIdentityResolution(
    Ipv4DefaultRouteClearStatus Status,
    WindowsInterfaceIdentity? Identity = null,
    uint? TechnicalCode = null)
{
    public bool IsSuccess => Status == Ipv4DefaultRouteClearStatus.Success && Identity is not null;
}

internal interface IWindowsInterfaceIdentityResolver
{
    InterfaceIdentityResolution Resolve(NetworkAdapterId adapterId);

    InterfaceIdentityResolution Revalidate(WindowsInterfaceIdentity identity);
}

internal interface IWindowsInterfaceIdentityNativeApi
{
    uint ConvertGuidToLuid(Guid interfaceGuid, out ulong interfaceLuid);

    uint ConvertLuidToIndex(ulong interfaceLuid, out uint interfaceIndex);

    uint ConvertIndexToLuid(uint interfaceIndex, out ulong interfaceLuid);
}

internal interface IWindowsPersistentRouteStore
{
    Ipv4DefaultRouteClearResult Clear(WindowsInterfaceIdentity identity);
}

internal interface IWindowsPersistentRouteProvider
{
    WindowsPersistentRouteReadResult Enumerate(WindowsInterfaceIdentity identity);

    WindowsPersistentRouteOperationResult Delete(WindowsPersistentRoute route);
}

internal interface IWindowsPersistentRouteManagementAdapter
{
    WindowsPersistentRouteReadResult Enumerate(
        string query,
        EnumerationOptions options);

    WindowsPersistentRouteOperationResult Delete(
        WindowsPersistentRoute route,
        DeleteOptions options);
}

internal interface IWindowsPersistentRouteContextFactory
{
    EnumerationOptions CreateEnumerationOptions();

    DeleteOptions CreateDeleteOptions();
}

internal interface IWindowsActiveRouteStore
{
    Ipv4DefaultRouteClearResult Clear(WindowsInterfaceIdentity identity);
}

internal interface IWindowsActiveRouteNativeApi
{
    uint GetIpForwardTable2(ushort family, out IntPtr table);

    uint DeleteIpForwardEntry2(ref SystemWindowsActiveRouteStore.MibIpForwardRow2 row);

    void FreeMibTable(IntPtr memory);
}

internal sealed class SystemWindowsInterfaceIdentityResolver : IWindowsInterfaceIdentityResolver
{
    private readonly IWindowsInterfaceIdentityNativeApi _nativeApi;

    public SystemWindowsInterfaceIdentityResolver()
        : this(new SystemWindowsInterfaceIdentityNativeApi())
    {
    }

    internal SystemWindowsInterfaceIdentityResolver(IWindowsInterfaceIdentityNativeApi nativeApi)
    {
        ArgumentNullException.ThrowIfNull(nativeApi);
        _nativeApi = nativeApi;
    }

    public InterfaceIdentityResolution Resolve(NetworkAdapterId adapterId)
    {
        if (!Guid.TryParse(adapterId.Value, out Guid guid))
        {
            return new InterfaceIdentityResolution(
                Ipv4DefaultRouteClearStatus.InvalidAdapterIdentity);
        }

        uint result = _nativeApi.ConvertGuidToLuid(guid, out ulong luid);

        if (result != 0)
        {
            return ResolutionFailure(result);
        }

        result = _nativeApi.ConvertLuidToIndex(luid, out uint index);
        return result == 0 && index != 0
            ? new InterfaceIdentityResolution(
                Ipv4DefaultRouteClearStatus.Success,
                new WindowsInterfaceIdentity(guid, luid, index))
            : ResolutionFailure(result);
    }

    public InterfaceIdentityResolution Revalidate(WindowsInterfaceIdentity identity)
    {
        uint result = _nativeApi.ConvertGuidToLuid(identity.InterfaceGuid, out ulong guidLuid);

        if (result != 0 || guidLuid != identity.Luid)
        {
            return ResolutionFailure(result);
        }

        result = _nativeApi.ConvertLuidToIndex(identity.Luid, out uint luidIndex);

        if (result != 0 || luidIndex != identity.InterfaceIndex)
        {
            return ResolutionFailure(result);
        }

        result = _nativeApi.ConvertIndexToLuid(identity.InterfaceIndex, out ulong indexLuid);

        return result == 0 && indexLuid == identity.Luid
            ? new InterfaceIdentityResolution(Ipv4DefaultRouteClearStatus.Success, identity)
            : ResolutionFailure(result);
    }

    private static InterfaceIdentityResolution ResolutionFailure(uint technicalCode) =>
        new(
            Ipv4DefaultRouteClearStatus.InterfaceResolutionFailed,
            TechnicalCode: technicalCode == 0 ? null : technicalCode);
}

internal sealed class SystemWindowsInterfaceIdentityNativeApi : IWindowsInterfaceIdentityNativeApi
{
    public uint ConvertGuidToLuid(Guid interfaceGuid, out ulong interfaceLuid) =>
        ConvertInterfaceGuidToLuid(ref interfaceGuid, out interfaceLuid);

    public uint ConvertLuidToIndex(ulong interfaceLuid, out uint interfaceIndex) =>
        ConvertInterfaceLuidToIndex(ref interfaceLuid, out interfaceIndex);

    public uint ConvertIndexToLuid(uint interfaceIndex, out ulong interfaceLuid) =>
        ConvertInterfaceIndexToLuid(interfaceIndex, out interfaceLuid);

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint ConvertInterfaceGuidToLuid(ref Guid interfaceGuid, out ulong interfaceLuid);

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint ConvertInterfaceLuidToIndex(ref ulong interfaceLuid, out uint interfaceIndex);

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint ConvertInterfaceIndexToLuid(uint interfaceIndex, out ulong interfaceLuid);
}

internal sealed record WindowsPersistentRoute(
    string ObjectPath,
    uint InterfaceIndex,
    uint AddressFamily,
    string DestinationPrefix,
    uint Store);

internal sealed record WindowsPersistentRouteReadResult(
    IReadOnlyList<WindowsPersistentRoute> Routes,
    bool IsSuccess = true,
    bool IsAccessDenied = false,
    uint? TechnicalCode = null)
{
    public static WindowsPersistentRouteReadResult Failure(
        bool accessDenied = false,
        uint? technicalCode = null) =>
        new(Array.Empty<WindowsPersistentRoute>(), false, accessDenied, technicalCode);
}

internal sealed record WindowsPersistentRouteOperationResult(
    bool IsSuccess,
    bool IsAccessDenied = false,
    uint? TechnicalCode = null)
{
    public static WindowsPersistentRouteOperationResult Success() => new(true);

    public static WindowsPersistentRouteOperationResult Failure(
        bool accessDenied = false,
        uint? technicalCode = null) =>
        new(false, accessDenied, technicalCode);
}

internal sealed class SystemWindowsPersistentRouteStore : IWindowsPersistentRouteStore
{
    private readonly IWindowsPersistentRouteProvider _provider;
    private readonly IWindowsInterfaceIdentityResolver _identityResolver;

    public SystemWindowsPersistentRouteStore()
        : this(
            new SystemWindowsPersistentRouteProvider(),
            new SystemWindowsInterfaceIdentityResolver())
    {
    }

    internal SystemWindowsPersistentRouteStore(
        IWindowsPersistentRouteProvider provider,
        IWindowsInterfaceIdentityResolver identityResolver)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(identityResolver);
        _provider = provider;
        _identityResolver = identityResolver;
    }

    public Ipv4DefaultRouteClearResult Clear(WindowsInterfaceIdentity identity)
    {
        WindowsPersistentRouteReadResult initial = _provider.Enumerate(identity);

        if (!initial.IsSuccess)
        {
            return ProviderFailure(
                Ipv4DefaultRouteClearStatus.PersistentStoreReadFailed,
                initial.IsAccessDenied,
                initial.TechnicalCode);
        }

        foreach (WindowsPersistentRoute route in initial.Routes)
        {
            if (!IsExactPersistentDefaultRoute(route, identity))
            {
                return new Ipv4DefaultRouteClearResult(
                    Ipv4DefaultRouteClearStatus.PersistentStoreReadFailed,
                    SystemWindowsPersistentRouteProvider.ErrorInvalidData);
            }

            InterfaceIdentityResolution revalidation = _identityResolver.Revalidate(identity);

            if (!revalidation.IsSuccess)
            {
                return new Ipv4DefaultRouteClearResult(
                    Ipv4DefaultRouteClearStatus.InterfaceResolutionFailed,
                    revalidation.TechnicalCode);
            }

            WindowsPersistentRouteOperationResult deletion = _provider.Delete(route);

            if (!deletion.IsSuccess)
            {
                return ProviderFailure(
                    Ipv4DefaultRouteClearStatus.PersistentStoreDeleteFailed,
                    deletion.IsAccessDenied,
                    deletion.TechnicalCode);
            }
        }

        WindowsPersistentRouteReadResult final = _provider.Enumerate(identity);

        if (!final.IsSuccess)
        {
            return ProviderFailure(
                Ipv4DefaultRouteClearStatus.PersistentStoreReadFailed,
                final.IsAccessDenied,
                final.TechnicalCode);
        }

        if (final.Routes.Any(route => !IsExactPersistentDefaultRoute(route, identity)))
        {
            return new Ipv4DefaultRouteClearResult(
                Ipv4DefaultRouteClearStatus.PersistentStoreReadFailed,
                SystemWindowsPersistentRouteProvider.ErrorInvalidData);
        }

        return final.Routes.Count == 0
            ? Ipv4DefaultRouteClearResult.Success()
            : new Ipv4DefaultRouteClearResult(Ipv4DefaultRouteClearStatus.RouteStillPresent);
    }

    private static Ipv4DefaultRouteClearResult ProviderFailure(
        Ipv4DefaultRouteClearStatus status,
        bool accessDenied,
        uint? technicalCode) =>
        new(
            accessDenied ? Ipv4DefaultRouteClearStatus.AccessDenied : status,
            technicalCode);

    internal static bool IsExactPersistentDefaultRoute(
        WindowsPersistentRoute route,
        WindowsInterfaceIdentity identity) =>
        route.InterfaceIndex == identity.InterfaceIndex &&
        route.AddressFamily == 2 &&
        string.Equals(route.DestinationPrefix, "0.0.0.0/0", StringComparison.Ordinal) &&
        route.Store == 0;
}

internal sealed class SystemWindowsPersistentRouteProvider : IWindowsPersistentRouteProvider
{
    internal const uint ErrorInvalidData = 13;
    internal const string PolicyStoreContextKey = "PolicyStore";
    internal const string PersistentStoreContextValue = "PersistentStore";

    private readonly IWindowsPersistentRouteManagementAdapter _managementAdapter;
    private readonly IWindowsPersistentRouteContextFactory _contextFactory;

    public SystemWindowsPersistentRouteProvider()
        : this(
            new SystemWindowsPersistentRouteManagementAdapter(),
            new SystemWindowsPersistentRouteContextFactory())
    {
    }

    internal SystemWindowsPersistentRouteProvider(
        IWindowsPersistentRouteManagementAdapter managementAdapter,
        IWindowsPersistentRouteContextFactory contextFactory)
    {
        ArgumentNullException.ThrowIfNull(managementAdapter);
        ArgumentNullException.ThrowIfNull(contextFactory);
        _managementAdapter = managementAdapter;
        _contextFactory = contextFactory;
    }

    internal static string BuildExactPersistentDefaultRouteQuery(uint interfaceIndex) =>
        "SELECT * FROM MSFT_NetRoute WHERE " +
        $"InterfaceIndex = {interfaceIndex.ToString(CultureInfo.InvariantCulture)} " +
        "AND AddressFamily = 2 AND DestinationPrefix = '0.0.0.0/0' AND Store = 0";

    public WindowsPersistentRouteReadResult Enumerate(WindowsInterfaceIdentity identity)
    {
        EnumerationOptions options = _contextFactory.CreateEnumerationOptions();

        return HasPersistentStoreContext(options.Context)
            ? _managementAdapter.Enumerate(
                BuildExactPersistentDefaultRouteQuery(identity.InterfaceIndex),
                options)
            : WindowsPersistentRouteReadResult.Failure(technicalCode: ErrorInvalidData);
    }

    public WindowsPersistentRouteOperationResult Delete(WindowsPersistentRoute route)
    {
        DeleteOptions options = _contextFactory.CreateDeleteOptions();

        return HasPersistentStoreContext(options.Context)
            ? _managementAdapter.Delete(route, options)
            : WindowsPersistentRouteOperationResult.Failure(technicalCode: ErrorInvalidData);
    }

    internal static bool HasPersistentStoreContext(ManagementNamedValueCollection? context) =>
        context is not null &&
        string.Equals(
            context[PolicyStoreContextKey] as string,
            PersistentStoreContextValue,
            StringComparison.Ordinal);

    internal static WindowsPersistentRouteReadResult ParseReturnedRoute(
        string? objectPath,
        object? interfaceIndex,
        object? addressFamily,
        object? destinationPrefix,
        object? store)
    {
        if (string.IsNullOrWhiteSpace(objectPath) ||
            interfaceIndex is null ||
            addressFamily is null ||
            destinationPrefix is not string prefix ||
            string.IsNullOrWhiteSpace(prefix) ||
            store is null)
        {
            return WindowsPersistentRouteReadResult.Failure(technicalCode: ErrorInvalidData);
        }

        try
        {
            return new WindowsPersistentRouteReadResult(
            [
                new WindowsPersistentRoute(
                    objectPath,
                    Convert.ToUInt32(interfaceIndex, CultureInfo.InvariantCulture),
                    Convert.ToUInt32(addressFamily, CultureInfo.InvariantCulture),
                    prefix,
                    Convert.ToUInt32(store, CultureInfo.InvariantCulture))
            ]);
        }
        catch (FormatException)
        {
            return WindowsPersistentRouteReadResult.Failure(technicalCode: ErrorInvalidData);
        }
        catch (InvalidCastException)
        {
            return WindowsPersistentRouteReadResult.Failure(technicalCode: ErrorInvalidData);
        }
        catch (OverflowException)
        {
            return WindowsPersistentRouteReadResult.Failure(technicalCode: ErrorInvalidData);
        }
    }
}

internal sealed class SystemWindowsPersistentRouteContextFactory :
    IWindowsPersistentRouteContextFactory
{
    public EnumerationOptions CreateEnumerationOptions() =>
        new() { Context = CreatePersistentStoreContext() };

    public DeleteOptions CreateDeleteOptions() =>
        new() { Context = CreatePersistentStoreContext() };

    private static ManagementNamedValueCollection CreatePersistentStoreContext()
    {
        ManagementNamedValueCollection context = new();
        context.Add(
            SystemWindowsPersistentRouteProvider.PolicyStoreContextKey,
            SystemWindowsPersistentRouteProvider.PersistentStoreContextValue);
        return context;
    }
}

internal sealed class SystemWindowsPersistentRouteManagementAdapter :
    IWindowsPersistentRouteManagementAdapter
{
    private const uint ErrorInvalidData = SystemWindowsPersistentRouteProvider.ErrorInvalidData;
    private const string NamespacePath = @"root\StandardCimv2";

    public WindowsPersistentRouteReadResult Enumerate(
        string query,
        EnumerationOptions options)
    {
        try
        {
            using ManagementObjectSearcher searcher = new(
                NamespacePath,
                query,
                options);
            using ManagementObjectCollection results = searcher.Get();
            List<WindowsPersistentRoute> routes = new();

            foreach (ManagementObject route in results)
            {
                using (route)
                {
                    string? objectPath = route.Path?.Path;
                    WindowsPersistentRouteReadResult parsed =
                        SystemWindowsPersistentRouteProvider.ParseReturnedRoute(
                            objectPath,
                            route["InterfaceIndex"],
                            route["AddressFamily"],
                            route["DestinationPrefix"],
                            route["Store"]);

                    if (!parsed.IsSuccess)
                    {
                        return parsed;
                    }

                    routes.Add(parsed.Routes[0]);
                }
            }

            return new WindowsPersistentRouteReadResult(routes);
        }
        catch (ManagementException exception)
        {
            return ReadFailure(exception);
        }
        catch (UnauthorizedAccessException)
        {
            return WindowsPersistentRouteReadResult.Failure(accessDenied: true);
        }
        catch (FormatException)
        {
            return WindowsPersistentRouteReadResult.Failure(technicalCode: ErrorInvalidData);
        }
        catch (InvalidCastException)
        {
            return WindowsPersistentRouteReadResult.Failure(technicalCode: ErrorInvalidData);
        }
        catch (OverflowException)
        {
            return WindowsPersistentRouteReadResult.Failure(technicalCode: ErrorInvalidData);
        }
    }

    public WindowsPersistentRouteOperationResult Delete(
        WindowsPersistentRoute route,
        DeleteOptions options)
    {
        try
        {
            using ManagementObject managementRoute = new(route.ObjectPath);
            managementRoute.Delete(options);
            return WindowsPersistentRouteOperationResult.Success();
        }
        catch (ManagementException exception)
        {
            return OperationFailure(exception);
        }
        catch (UnauthorizedAccessException)
        {
            return WindowsPersistentRouteOperationResult.Failure(accessDenied: true);
        }
    }

    private static WindowsPersistentRouteReadResult ReadFailure(ManagementException exception) =>
        WindowsPersistentRouteReadResult.Failure(
            exception.ErrorCode == ManagementStatus.AccessDenied,
            unchecked((uint)(int)exception.ErrorCode));

    private static WindowsPersistentRouteOperationResult OperationFailure(ManagementException exception) =>
        WindowsPersistentRouteOperationResult.Failure(
            exception.ErrorCode == ManagementStatus.AccessDenied,
            unchecked((uint)(int)exception.ErrorCode));
}

internal sealed class SystemWindowsActiveRouteStore : IWindowsActiveRouteStore
{
    internal const ushort AfInet = 2;
    internal const uint ErrorAccessDenied = 5;
    internal const uint ErrorNotFound = 1168;
    internal const uint ErrorNotSupported = 50;
    internal const int FirstRowOffset = 8;

    private readonly IWindowsActiveRouteNativeApi _nativeApi;

    public SystemWindowsActiveRouteStore()
        : this(new SystemWindowsActiveRouteNativeApi())
    {
    }

    internal SystemWindowsActiveRouteStore(IWindowsActiveRouteNativeApi nativeApi)
    {
        ArgumentNullException.ThrowIfNull(nativeApi);
        _nativeApi = nativeApi;
    }

    internal static bool IsSupportedArchitecture =>
        IntPtr.Size == 8 && RuntimeInformation.ProcessArchitecture == Architecture.X64;

    public Ipv4DefaultRouteClearResult Clear(WindowsInterfaceIdentity identity)
    {
        if (!IsSupportedArchitecture)
        {
            return new Ipv4DefaultRouteClearResult(
                Ipv4DefaultRouteClearStatus.ActiveStoreReadFailed,
                ErrorNotSupported);
        }

        RouteTableRead first = ReadTable(identity);

        if (!first.IsSuccess)
        {
            return new Ipv4DefaultRouteClearResult(
                Ipv4DefaultRouteClearStatus.ActiveStoreReadFailed,
                first.TechnicalCode);
        }

        foreach (MibIpForwardRow2 row in first.Rows)
        {
            MibIpForwardRow2 exactRow = row;
            uint result = _nativeApi.DeleteIpForwardEntry2(ref exactRow);

            if (result != 0 && result != ErrorNotFound)
            {
                return new Ipv4DefaultRouteClearResult(
                    result == ErrorAccessDenied
                        ? Ipv4DefaultRouteClearStatus.AccessDenied
                        : Ipv4DefaultRouteClearStatus.ActiveStoreDeleteFailed,
                    result);
            }
        }

        RouteTableRead final = ReadTable(identity);

        if (!final.IsSuccess)
        {
            return new Ipv4DefaultRouteClearResult(
                Ipv4DefaultRouteClearStatus.ActiveStoreReadFailed,
                final.TechnicalCode);
        }

        return final.Rows.Count == 0
            ? Ipv4DefaultRouteClearResult.Success()
            : new Ipv4DefaultRouteClearResult(Ipv4DefaultRouteClearStatus.RouteStillPresent);
    }

    private RouteTableRead ReadTable(WindowsInterfaceIdentity identity)
    {
        IntPtr table = IntPtr.Zero;

        try
        {
            uint result = _nativeApi.GetIpForwardTable2(AfInet, out table);

            if (result == ErrorNotFound)
            {
                return new RouteTableRead(Array.Empty<MibIpForwardRow2>());
            }

            if (result != 0 || table == IntPtr.Zero)
            {
                return new RouteTableRead(result);
            }

            uint count = unchecked((uint)Marshal.ReadInt32(table));
            int rowSize = Marshal.SizeOf<MibIpForwardRow2>();
            List<MibIpForwardRow2> rows = new();

            for (uint index = 0; index < count; index++)
            {
                IntPtr pointer = IntPtr.Add(
                    table,
                    checked(FirstRowOffset + checked((int)index * rowSize)));
                MibIpForwardRow2 row = Marshal.PtrToStructure<MibIpForwardRow2>(pointer);

                if (IsExactDefaultRoute(row, identity))
                {
                    rows.Add(row);
                }
            }

            return new RouteTableRead(rows);
        }
        finally
        {
            if (table != IntPtr.Zero)
            {
                _nativeApi.FreeMibTable(table);
            }
        }
    }

    private static bool IsExactDefaultRoute(
        MibIpForwardRow2 row,
        WindowsInterfaceIdentity identity) =>
        IsExactDefaultRoute(
            row.InterfaceLuid,
            row.InterfaceIndex,
            row.DestinationPrefix.Prefix.Family,
            row.DestinationPrefix.Prefix.Ipv4Address,
            row.DestinationPrefix.PrefixLength,
            identity);

    internal static bool IsExactDefaultRoute(
        ulong routeLuid,
        uint routeIndex,
        ushort family,
        uint destinationAddress,
        byte prefixLength,
        WindowsInterfaceIdentity identity) =>
        routeLuid == identity.Luid &&
        routeIndex == identity.InterfaceIndex &&
        family == AfInet &&
        destinationAddress == 0 &&
        prefixLength == 0;

    private sealed record RouteTableRead(
        IReadOnlyList<MibIpForwardRow2> Rows,
        uint? TechnicalCode = null)
    {
        public RouteTableRead(uint technicalCode)
            : this(Array.Empty<MibIpForwardRow2>(), technicalCode)
        {
        }

        public bool IsSuccess => TechnicalCode is null;
    }

    [StructLayout(LayoutKind.Explicit, Size = 112)]
    internal struct MibIpForwardTable2Header
    {
        [FieldOffset(0)] public uint NumEntries;
        [FieldOffset(FirstRowOffset)] public MibIpForwardRow2 FirstRow;
    }

    [StructLayout(LayoutKind.Explicit, Size = 104)]
    internal struct MibIpForwardRow2
    {
        [FieldOffset(0)] public ulong InterfaceLuid;
        [FieldOffset(8)] public uint InterfaceIndex;
        [FieldOffset(12)] public IpAddressPrefix DestinationPrefix;
        [FieldOffset(44)] public SockaddrInet NextHop;
        [FieldOffset(72)] public byte SitePrefixLength;
        [FieldOffset(76)] public uint ValidLifetime;
        [FieldOffset(80)] public uint PreferredLifetime;
        [FieldOffset(84)] public uint Metric;
        [FieldOffset(88)] public int Protocol;
        [FieldOffset(92)] public byte Loopback;
        [FieldOffset(93)] public byte AutoconfigureAddress;
        [FieldOffset(94)] public byte Publish;
        [FieldOffset(95)] public byte Immortal;
        [FieldOffset(96)] public uint Age;
        [FieldOffset(100)] public int Origin;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    internal struct IpAddressPrefix
    {
        [FieldOffset(0)] public SockaddrInet Prefix;
        [FieldOffset(28)] public byte PrefixLength;
    }

    [StructLayout(LayoutKind.Explicit, Size = 28)]
    internal struct SockaddrInet
    {
        [FieldOffset(0)] public ushort Family;
        [FieldOffset(4)] public uint Ipv4Address;
        [FieldOffset(8)] public ulong AddressPart1;
        [FieldOffset(16)] public ulong AddressPart2;
        [FieldOffset(24)] public uint ScopeId;
    }
}

internal sealed class SystemWindowsActiveRouteNativeApi : IWindowsActiveRouteNativeApi
{
    public uint GetIpForwardTable2(ushort family, out IntPtr table) =>
        NativeGetIpForwardTable2(family, out table);

    public uint DeleteIpForwardEntry2(ref SystemWindowsActiveRouteStore.MibIpForwardRow2 row) =>
        NativeDeleteIpForwardEntry2(ref row);

    public void FreeMibTable(IntPtr memory) => NativeFreeMibTable(memory);

    [DllImport("iphlpapi.dll", EntryPoint = "GetIpForwardTable2", ExactSpelling = true)]
    private static extern uint NativeGetIpForwardTable2(ushort family, out IntPtr table);

    [DllImport("iphlpapi.dll", EntryPoint = "DeleteIpForwardEntry2", ExactSpelling = true)]
    private static extern uint NativeDeleteIpForwardEntry2(
        ref SystemWindowsActiveRouteStore.MibIpForwardRow2 row);

    [DllImport("iphlpapi.dll", EntryPoint = "FreeMibTable", ExactSpelling = true)]
    private static extern void NativeFreeMibTable(IntPtr memory);
}
