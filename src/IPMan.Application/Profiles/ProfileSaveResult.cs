using IPMan.Domain.Profiles;

namespace IPMan.Application.Profiles;

/// <summary>Represents a profile save outcome.</summary>
public sealed record ProfileSaveResult(NetworkProfile? Profile, ProfileSaveStatus Status)
{
    /// <summary>Gets whether the profile was saved.</summary>
    public bool IsSuccess => Profile is not null && Status == ProfileSaveStatus.Success;

    /// <summary>Creates a successful result containing the final saved profile.</summary>
    public static ProfileSaveResult Success(NetworkProfile profile) =>
        new(profile, ProfileSaveStatus.Success);

    /// <summary>Creates a failed result.</summary>
    public static ProfileSaveResult Failed(ProfileSaveStatus status) => new(null, status);
}
