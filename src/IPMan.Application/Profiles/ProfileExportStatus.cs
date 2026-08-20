namespace IPMan.Application.Profiles;

/// <summary>Classifies a profile export outcome.</summary>
public enum ProfileExportStatus
{
    Success = 0,
    NotFound = 1,
    AccessDenied = 2,
    IoFailure = 3
}
