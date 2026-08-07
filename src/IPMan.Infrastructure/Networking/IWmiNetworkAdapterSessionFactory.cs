namespace IPMan.Infrastructure.Networking;

internal interface IWmiNetworkAdapterSessionFactory
{
    WmiAdapterResolution ResolveBySettingId(string adapterId);
}
