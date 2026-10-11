using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Infrastructure.Networking;

/// <summary>Reads NLM/NCSI's IPv4 assessment and forwards Windows connection events.</summary>
public sealed class WindowsNetworkListConnectivityService : INetworkConnectivityService, IDisposable
{
    private static readonly Guid ManagerClassId = new("DCB00C01-570F-4A9B-8D69-199FDBA5723B");
    private readonly object _sync = new();
    private INetworkListManager? _manager;
    private IConnectionPoint? _connectionPoint;
    private ConnectionEventsSink? _sink;
    private int _cookie;
    private bool _disposed;

    public event EventHandler? ConnectivityChanged;

    public Task<IReadOnlyDictionary<NetworkAdapterId, InternetConnectivity>> GetConnectivityAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (_disposed)
            {
                return EmptyResult();
            }

            try
            {
                EnsureManager();
                if (_manager is null)
                {
                    return EmptyResult();
                }

                Dictionary<NetworkAdapterId, InternetConnectivity> result = new();
                IEnumNetworkConnections connections = _manager.GetNetworkConnections();
                try
                {
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        INetworkConnection? connection = null;
                        try
                        {
                            int hr = connections.Next(1, out connection, out uint fetched);
                            Marshal.ThrowExceptionForHR(hr);
                            if (fetched == 0 || connection is null)
                            {
                                break;
                            }

                            NetworkAdapterId id = NormalizeAdapterId(connection.GetAdapterId());
                            InternetConnectivity value = MapConnectivity(connection.GetConnectivity(), connection.GetIsConnected());
                            // Windows can report multiple connections for one adapter.
                            if (!result.TryGetValue(id, out InternetConnectivity previous) || value > previous)
                            {
                                result[id] = value;
                            }
                        }
                        finally
                        {
                            Release(connection);
                        }
                    }
                }
                finally
                {
                    Release(connections);
                }

                return Task.FromResult<IReadOnlyDictionary<NetworkAdapterId, InternetConnectivity>>(result);
            }
            catch (COMException)
            {
                return EmptyResult();
            }
            catch (UnauthorizedAccessException)
            {
                return EmptyResult();
            }
            catch (InvalidCastException)
            {
                return EmptyResult();
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            Volatile.Write(ref _disposed, true);
            try
            {
                if (_cookie != 0)
                {
                    _connectionPoint?.Unadvise(_cookie);
                }
            }
            catch (COMException)
            {
                // Windows may already have disconnected the event source.
            }
            finally
            {
                _cookie = 0;
                Release(_connectionPoint);
                _connectionPoint = null;
                Release(_manager);
                _manager = null;
                _sink = null;
            }
        }
    }

    internal static InternetConnectivity MapConnectivity(uint flags, bool isConnected)
    {
        const uint ipv4Internet = 0x40;
        const uint ipv4LocalNetworkOrSubnet = 0x30;
        const uint ipv4NoTraffic = 0x1;

        if ((flags & ipv4Internet) != 0)
        {
            return InternetConnectivity.Internet;
        }

        return (flags & ipv4LocalNetworkOrSubnet) != 0 || (isConnected && (flags & ipv4NoTraffic) != 0)
            ? InternetConnectivity.LocalNetwork
            : InternetConnectivity.None;
    }

    internal static NetworkAdapterId NormalizeAdapterId(Guid adapterId) => new(adapterId.ToString("B"));

    private void EnsureManager()
    {
        if (_manager is not null)
        {
            return;
        }

        // Created lazily by the coordinator's background discovery loop.
        Type? managerType = Type.GetTypeFromCLSID(ManagerClassId);
        if (managerType is null)
        {
            return;
        }

        object? instance = Activator.CreateInstance(managerType);
        if (instance is not INetworkListManager manager)
        {
            Release(instance);
            return;
        }

        _manager = manager;
        try
        {
            Guid eventId = typeof(INetworkConnectionEvents).GUID;
            ((IConnectionPointContainer)manager).FindConnectionPoint(ref eventId, out IConnectionPoint? point);
            if (point is null)
            {
                return;
            }

            _connectionPoint = point;
            _sink = new ConnectionEventsSink(RaiseConnectivityChanged);
            point.Advise(_sink, out _cookie);
        }
        catch (COMException)
        {
            Release(_connectionPoint);
            _connectionPoint = null;
            _sink = null;
            // Reads still work; the coordinator's reconciliation remains available.
        }
    }

    private void RaiseConnectivityChanged()
    {
        if (!Volatile.Read(ref _disposed))
        {
            ConnectivityChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static Task<IReadOnlyDictionary<NetworkAdapterId, InternetConnectivity>> EmptyResult() =>
        Task.FromResult<IReadOnlyDictionary<NetworkAdapterId, InternetConnectivity>>(
            new Dictionary<NetworkAdapterId, InternetConnectivity>());

    private static void Release(object? instance)
    {
        if (instance is not null && Marshal.IsComObject(instance))
        {
            try
            {
                _ = Marshal.FinalReleaseComObject(instance);
            }
            catch (COMException)
            {
                // A disconnected COM server must not interrupt cleanup or shutdown.
            }
        }
    }

    /// <summary>Public for COM visibility; owned exclusively by the connectivity service.</summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public sealed class ConnectionEventsSink : INetworkConnectionEvents
    {
        private readonly Action _changed;

        internal ConnectionEventsSink(Action changed) => _changed = changed;

        public void NetworkConnectionConnectivityChanged(Guid connectionId, uint newConnectivity) => _changed();

        public void NetworkConnectionPropertyChanged(Guid connectionId, uint flags)
        {
        }
    }
}

// Dual interfaces retain the SDK's vtable order, including unused preceding slots.
[ComImport]
[Guid("DCB00000-570F-4A9B-8D69-199FDBA5723B")]
[InterfaceType(ComInterfaceType.InterfaceIsDual)]
internal interface INetworkListManager
{
    [return: MarshalAs(UnmanagedType.Interface)]
    object GetNetworks(uint flags);

    [return: MarshalAs(UnmanagedType.Interface)]
    object GetNetwork(Guid networkId);

    IEnumNetworkConnections GetNetworkConnections();
}

[ComImport]
[Guid("DCB00006-570F-4A9B-8D69-199FDBA5723B")]
[InterfaceType(ComInterfaceType.InterfaceIsDual)]
internal interface IEnumNetworkConnections
{
    [return: MarshalAs(UnmanagedType.Interface)]
    object GetNewEnum();

    [PreserveSig]
    int Next(uint count, [MarshalAs(UnmanagedType.Interface)] out INetworkConnection? connection, out uint fetched);
}

[ComImport]
[Guid("DCB00005-570F-4A9B-8D69-199FDBA5723B")]
[InterfaceType(ComInterfaceType.InterfaceIsDual)]
internal interface INetworkConnection
{
    [return: MarshalAs(UnmanagedType.Interface)]
    object GetNetwork();

    [return: MarshalAs(UnmanagedType.VariantBool)]
    bool GetIsConnectedToInternet();

    [return: MarshalAs(UnmanagedType.VariantBool)]
    bool GetIsConnected();

    uint GetConnectivity();

    Guid GetConnectionId();

    Guid GetAdapterId();
}

/// <summary>Native connection point contract implemented by the managed event sink.</summary>
[ComImport]
[ComVisible(true)]
[Guid("DCB00007-570F-4A9B-8D69-199FDBA5723B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface INetworkConnectionEvents
{
    void NetworkConnectionConnectivityChanged(Guid connectionId, uint newConnectivity);

    void NetworkConnectionPropertyChanged(Guid connectionId, uint flags);
}
