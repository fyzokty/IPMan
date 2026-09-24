using IPMan.Domain.Settings;
using Xunit;

namespace IPMan.Tests.Settings;

public sealed class ThemeResolverTests
{
    [Theory]
    [InlineData(AppTheme.Light, false, EffectiveTheme.Light)]
    [InlineData(AppTheme.Light, true, EffectiveTheme.Light)]
    [InlineData(AppTheme.Dark, false, EffectiveTheme.Dark)]
    [InlineData(AppTheme.Dark, true, EffectiveTheme.Dark)]
    public void Resolve_WhenThemeIsExplicit_IgnoresSystemTheme(
        AppTheme preference,
        bool systemIsLight,
        EffectiveTheme expected)
    {
        EffectiveTheme result = ThemeResolver.Resolve(preference, systemIsLight, highContrast: false);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, EffectiveTheme.Light)]
    [InlineData(false, EffectiveTheme.Dark)]
    public void Resolve_WhenThemeIsSystem_FollowsSystemTheme(bool systemIsLight, EffectiveTheme expected)
    {
        EffectiveTheme result = ThemeResolver.Resolve(AppTheme.System, systemIsLight, highContrast: false);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_WhenSystemThemeCannotBeRead_UsesLightTheme()
    {
        EffectiveTheme result = ThemeResolver.Resolve(AppTheme.System, systemIsLight: null, highContrast: false);

        Assert.Equal(EffectiveTheme.Light, result);
    }

    [Theory]
    [InlineData(AppTheme.System)]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.Dark)]
    public void Resolve_WhenHighContrastIsEnabled_PrefersHighContrast(AppTheme preference)
    {
        EffectiveTheme result = ThemeResolver.Resolve(preference, systemIsLight: false, highContrast: true);

        Assert.Equal(EffectiveTheme.HighContrast, result);
    }
}
