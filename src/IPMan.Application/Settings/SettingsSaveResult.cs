namespace IPMan.Application.Settings;

/// <summary>Represents a settings save outcome.</summary>
public sealed record SettingsSaveResult(SettingsSaveStatus Status)
{
    /// <summary>Gets whether the settings were saved.</summary>
    public bool IsSuccess => Status == SettingsSaveStatus.Success;

    /// <summary>Creates a successful result.</summary>
    public static SettingsSaveResult Success() => new(SettingsSaveStatus.Success);

    /// <summary>Creates a failed result.</summary>
    public static SettingsSaveResult Failed(SettingsSaveStatus status) => new(status);
}
