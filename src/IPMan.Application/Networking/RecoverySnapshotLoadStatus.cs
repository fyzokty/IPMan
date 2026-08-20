namespace IPMan.Application.Networking;

/// <summary>Outcomes from loading an adapter's latest recovery snapshot.</summary>
public enum RecoverySnapshotLoadStatus
{
    /// <summary>A supported snapshot was loaded.</summary>
    Success = 0,

    /// <summary>No supported snapshot exists for the requested adapter.</summary>
    NotFound = 1,

    /// <summary>The snapshot directory or a candidate file could not be read.</summary>
    IoFailure = 2,

    /// <summary>Access to the snapshot directory or a candidate file was denied.</summary>
    AccessDenied = 3
}
