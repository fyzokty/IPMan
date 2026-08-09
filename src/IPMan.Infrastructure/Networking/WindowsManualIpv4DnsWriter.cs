using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using IPMan.Domain.Networking;

namespace IPMan.Infrastructure.Networking;

internal enum ManualIpv4DnsWriteStatus
{
    Success = 0,
    InvalidAdapterIdentity = 1,
    InvalidServerList = 2,
    NativeCallFailed = 3,
    NativeLibraryUnavailable = 4,
    NativeEntryPointUnavailable = 5
}

internal sealed record ManualIpv4DnsWriteResult(
    ManualIpv4DnsWriteStatus Status,
    uint? TechnicalCode = null)
{
    public bool IsSuccess => Status == ManualIpv4DnsWriteStatus.Success;
}

internal interface IManualIpv4DnsWriter
{
    ManualIpv4DnsWriteResult Write(
        NetworkAdapterId adapterId,
        IReadOnlyList<string> servers);
}

internal interface IDnsInterfaceSettingsWriterNativeApi
{
    uint Set(Guid interfaceId, ref DnsInterfaceSettingsV1 settings);
}

internal sealed class WindowsManualIpv4DnsWriter : IManualIpv4DnsWriter
{
    internal const ulong DnsSettingNameServer = 0x0002;

    private readonly IDnsInterfaceSettingsWriterNativeApi _nativeApi;

    public WindowsManualIpv4DnsWriter()
        : this(new DnsInterfaceSettingsWriterNativeApi())
    {
    }

    internal WindowsManualIpv4DnsWriter(IDnsInterfaceSettingsWriterNativeApi nativeApi)
    {
        ArgumentNullException.ThrowIfNull(nativeApi);
        _nativeApi = nativeApi;
    }

    public ManualIpv4DnsWriteResult Write(
        NetworkAdapterId adapterId,
        IReadOnlyList<string> servers)
    {
        ArgumentNullException.ThrowIfNull(servers);

        if (!Guid.TryParse(adapterId.Value, out Guid interfaceId))
        {
            return new ManualIpv4DnsWriteResult(
                ManualIpv4DnsWriteStatus.InvalidAdapterIdentity);
        }

        string[] normalized = NormalizeServers(servers);

        if (normalized.Length != servers.Count || normalized.Length is < 1 or > 2)
        {
            return new ManualIpv4DnsWriteResult(ManualIpv4DnsWriteStatus.InvalidServerList);
        }

        IntPtr nameServer = Marshal.StringToHGlobalUni(string.Join(',', normalized));

        try
        {
            DnsInterfaceSettingsV1 settings = new()
            {
                Version = 1,
                Flags = DnsSettingNameServer,
                NameServer = nameServer
            };
            uint result = _nativeApi.Set(interfaceId, ref settings);

            return result == 0
                ? new ManualIpv4DnsWriteResult(ManualIpv4DnsWriteStatus.Success, result)
                : new ManualIpv4DnsWriteResult(
                    ManualIpv4DnsWriteStatus.NativeCallFailed,
                    result);
        }
        catch (DllNotFoundException)
        {
            return new ManualIpv4DnsWriteResult(
                ManualIpv4DnsWriteStatus.NativeLibraryUnavailable);
        }
        catch (EntryPointNotFoundException)
        {
            return new ManualIpv4DnsWriteResult(
                ManualIpv4DnsWriteStatus.NativeEntryPointUnavailable);
        }
        finally
        {
            Marshal.FreeHGlobal(nameServer);
        }
    }

    private static string[] NormalizeServers(IReadOnlyList<string> servers)
    {
        List<string> normalized = new(servers.Count);

        foreach (string server in servers)
        {
            if (!IPAddress.TryParse(server, out IPAddress? address) ||
                address.AddressFamily != AddressFamily.InterNetwork)
            {
                return Array.Empty<string>();
            }

            normalized.Add(address.ToString());
        }

        return normalized.ToArray();
    }
}

internal sealed class DnsInterfaceSettingsWriterNativeApi :
    IDnsInterfaceSettingsWriterNativeApi
{
    public uint Set(Guid interfaceId, ref DnsInterfaceSettingsV1 settings) =>
        SetInterfaceDnsSettings(interfaceId, ref settings);

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint SetInterfaceDnsSettings(
        Guid interfaceId,
        ref DnsInterfaceSettingsV1 settings);
}
