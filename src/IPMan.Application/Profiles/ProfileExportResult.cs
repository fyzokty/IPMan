namespace IPMan.Application.Profiles;

/// <summary>Represents a profile export outcome.</summary>
public sealed record ProfileExportResult(ProfileExportStatus Status)
{
    /// <summary>Gets whether the profile was exported.</summary>
    public bool IsSuccess => Status == ProfileExportStatus.Success;

    /// <summary>Creates a successful result.</summary>
    public static ProfileExportResult Success() => new(ProfileExportStatus.Success);

    /// <summary>Creates a failed result.</summary>
    public static ProfileExportResult Failed(ProfileExportStatus status) => new(status);
}
