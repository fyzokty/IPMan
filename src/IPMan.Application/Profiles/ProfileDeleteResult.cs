namespace IPMan.Application.Profiles;

/// <summary>Represents a profile delete outcome.</summary>
public sealed record ProfileDeleteResult(ProfileDeleteStatus Status)
{
    /// <summary>Gets whether the profile was deleted.</summary>
    public bool IsSuccess => Status == ProfileDeleteStatus.Success;

    /// <summary>Creates a successful result.</summary>
    public static ProfileDeleteResult Success() => new(ProfileDeleteStatus.Success);

    /// <summary>Creates a failed result.</summary>
    public static ProfileDeleteResult Failed(ProfileDeleteStatus status) => new(status);
}
