namespace IPMan.Application.Networking;

public enum Ipv4ConflictProbeStatus
{
    ResponseObserved = 0,
    NoResponse = 1,
    Unavailable = 2,
    Cancelled = 3
}
