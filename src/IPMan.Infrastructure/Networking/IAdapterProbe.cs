namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Reads raw adapter data from the operating system.
/// Implementations are synchronous because the underlying Windows APIs are.
/// </summary>
public interface IAdapterProbe
{
    /// <summary>
    /// Returns raw data for every adapter the OS reports. Adapters whose data
    /// cannot be read are omitted rather than failing the whole read.
    /// </summary>
    IReadOnlyList<AdapterReadModel> ReadAdapters();
}
