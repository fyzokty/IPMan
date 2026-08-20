namespace IPMan.Application.Profiles;

/// <summary>Classifies a profile import outcome.</summary>
public enum ProfileImportStatus
{
    Success = 0,
    InvalidContent = 1,
    AccessDenied = 2,
    IoFailure = 3
}
