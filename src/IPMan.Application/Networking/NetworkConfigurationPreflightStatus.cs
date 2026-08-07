namespace IPMan.Application.Networking;

public enum NetworkConfigurationPreflightStatus
{
    Ready = 0,
    NoChange = 1,
    ValidationFailed = 2,
    AdapterUnavailable = 3,
    MultipleIpv4RequiresSafetyDecision = 4,
    PotentialAddressConflict = 5,
    ProbeIndeterminate = 6,
    Cancelled = 7,
    AdapterReadFailed = 8
}
