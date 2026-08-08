using System.Runtime.InteropServices;

namespace IPMan.Infrastructure.Networking;

internal interface IDnsInterfaceSettingsReader
{
    DnsInterfaceSettingsReadResult Read(Guid interfaceId);
}

internal sealed record DnsInterfaceSettingsReadResult(
    uint NativeResult,
    ulong Flags,
    string? NameServers);

internal sealed class WindowsDnsInterfaceSettingsReader : IDnsInterfaceSettingsReader
{
    private readonly IDnsInterfaceSettingsNativeApi _nativeApi;

    public WindowsDnsInterfaceSettingsReader()
        : this(new DnsInterfaceSettingsNativeApi())
    {
    }

    internal WindowsDnsInterfaceSettingsReader(IDnsInterfaceSettingsNativeApi nativeApi)
    {
        ArgumentNullException.ThrowIfNull(nativeApi);
        _nativeApi = nativeApi;
    }

    public DnsInterfaceSettingsReadResult Read(Guid interfaceId)
    {
        DnsInterfaceSettingsV1 settings = new() { Version = 1 };
        uint result = _nativeApi.Get(interfaceId, ref settings);

        if (result != 0)
        {
            return new DnsInterfaceSettingsReadResult(result, 0, null);
        }

        try
        {
            return new DnsInterfaceSettingsReadResult(
                result,
                settings.Flags,
                Marshal.PtrToStringUni(settings.NameServer));
        }
        finally
        {
            _nativeApi.Free(ref settings);
        }
    }
}

internal interface IDnsInterfaceSettingsNativeApi
{
    uint Get(Guid interfaceId, ref DnsInterfaceSettingsV1 settings);

    void Free(ref DnsInterfaceSettingsV1 settings);
}

internal sealed class DnsInterfaceSettingsNativeApi : IDnsInterfaceSettingsNativeApi
{
    public uint Get(Guid interfaceId, ref DnsInterfaceSettingsV1 settings) =>
        GetInterfaceDnsSettings(interfaceId, ref settings);

    public void Free(ref DnsInterfaceSettingsV1 settings) =>
        FreeInterfaceDnsSettings(ref settings);

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern uint GetInterfaceDnsSettings(
        Guid interfaceId,
        ref DnsInterfaceSettingsV1 settings);

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern void FreeInterfaceDnsSettings(ref DnsInterfaceSettingsV1 settings);
}

[StructLayout(LayoutKind.Sequential)]
internal struct DnsInterfaceSettingsV1
{
    public uint Version;
    public ulong Flags;
    public IntPtr Domain;
    public IntPtr NameServer;
    public IntPtr SearchList;
    public uint RegistrationEnabled;
    public uint RegisterAdapterName;
    public uint EnableLlmnr;
    public uint QueryAdapterName;
    public IntPtr ProfileNameServer;
}
