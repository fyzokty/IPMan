namespace IPMan.Infrastructure.Networking;

internal sealed record WmiAdapterResolution(
    WmiAdapterResolutionStatus Status,
    IWmiNetworkAdapterSession? Session = null);
