using IPMan.Domain.Profiles;

namespace IPMan.Application.Profiles;

/// <summary>Represents a profile import outcome.</summary>
public sealed record ProfileImportResult(NetworkProfile? Profile, ProfileImportStatus Status)
{
    /// <summary>Gets whether the profile was imported.</summary>
    public bool IsSuccess => Profile is not null && Status == ProfileImportStatus.Success;

    /// <summary>Creates a successful result containing the final imported profile.</summary>
    public static ProfileImportResult Success(NetworkProfile profile) =>
        new(profile, ProfileImportStatus.Success);

    /// <summary>Creates a failed result.</summary>
    public static ProfileImportResult Failed(ProfileImportStatus status) => new(null, status);
}
