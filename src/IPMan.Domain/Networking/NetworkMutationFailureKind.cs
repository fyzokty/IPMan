namespace IPMan.Domain.Networking;

public enum NetworkMutationFailureKind
{
    None = 0,
    AdapterUnavailable = 1,
    AdapterMappingAmbiguous = 2,
    OperationalFailure = 3,
    ManagementFailure = 4,
    InterfaceResolutionFailure = 5,
    PersistentRouteFailure = 6,
    ActiveRouteFailure = 7,
    RouteVerificationFailure = 8,
    AccessDenied = 9
}
