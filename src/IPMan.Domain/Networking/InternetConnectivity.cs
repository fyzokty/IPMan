namespace IPMan.Domain.Networking;

/// <summary>The operating system's assessment of IPv4 connectivity for an adapter.</summary>
public enum InternetConnectivity
{
    /// <summary>Windows did not report an assessment, or it could not be read.</summary>
    Unknown,

    /// <summary>Windows reports no IPv4 connectivity.</summary>
    None,

    /// <summary>Windows reports local IPv4 connectivity without internet access.</summary>
    LocalNetwork,

    /// <summary>Windows reports IPv4 internet access.</summary>
    Internet
}
