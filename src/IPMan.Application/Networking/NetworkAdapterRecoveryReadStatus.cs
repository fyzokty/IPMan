namespace IPMan.Application.Networking;

public enum NetworkAdapterRecoveryReadStatus
{
    Success = 0,
    AdapterUnavailable = 1,
    AdapterMappingAmbiguous = 2,
    ReadFailed = 3
}
