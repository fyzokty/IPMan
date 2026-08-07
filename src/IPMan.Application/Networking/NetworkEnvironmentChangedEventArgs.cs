namespace IPMan.Application.Networking;

public sealed class NetworkEnvironmentChangedEventArgs : EventArgs
{
    public NetworkEnvironmentChangedEventArgs(string reason)
    {
        Reason = reason;
    }

    public string Reason { get; }
}
