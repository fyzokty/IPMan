namespace IPMan.Application.Profiles;

/// <summary>Classifies a profile delete outcome.</summary>
public enum ProfileDeleteStatus
{
    Success = 0,
    NotFound = 1,
    AccessDenied = 2,
    IoFailure = 3
}
