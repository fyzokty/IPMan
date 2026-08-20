namespace IPMan.Application.Settings;

/// <summary>Classifies a settings save outcome.</summary>
public enum SettingsSaveStatus
{
    Success = 0,
    InvalidContent = 1,
    AccessDenied = 2,
    IoFailure = 3
}
