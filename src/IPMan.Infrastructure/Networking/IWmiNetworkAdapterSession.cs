namespace IPMan.Infrastructure.Networking;

internal interface IWmiNetworkAdapterSession : IDisposable
{
    object? ReadProperty(string propertyName);

    uint Invoke(string methodName, IReadOnlyDictionary<string, object?>? parameters);
}
