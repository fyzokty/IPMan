namespace IPMan.Application.Profiles;

/// <summary>Classifies a profile save outcome.</summary>
public enum ProfileSaveStatus
{
    Success = 0,
    InvalidContent = 1,
    AccessDenied = 2,
    IoFailure = 3
}
