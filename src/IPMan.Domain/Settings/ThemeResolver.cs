namespace IPMan.Domain.Settings;

/// <summary>Resolves a persisted theme preference without depending on Windows APIs.</summary>
public static class ThemeResolver
{
    /// <summary>Gets the effective theme for a user preference and system state.</summary>
    public static EffectiveTheme Resolve(AppTheme preference, bool? systemIsLight, bool highContrast)
    {
        if (highContrast)
        {
            return EffectiveTheme.HighContrast;
        }

        return preference switch
        {
            AppTheme.Dark => EffectiveTheme.Dark,
            AppTheme.Light => EffectiveTheme.Light,
            _ => systemIsLight == false ? EffectiveTheme.Dark : EffectiveTheme.Light
        };
    }
}
