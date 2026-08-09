namespace IPMan.Application.Networking;

public enum Ipv4DefaultRouteClearStatus
{
    Success = 0,
    InvalidAdapterIdentity = 1,
    InterfaceResolutionFailed = 2,
    PersistentStoreReadFailed = 3,
    PersistentStoreDeleteFailed = 4,
    ActiveStoreReadFailed = 5,
    ActiveStoreDeleteFailed = 6,
    RouteStillPresent = 7,
    AccessDenied = 8
}
