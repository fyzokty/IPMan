using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using IPMan.Domain.Settings;
using WpfApplication = System.Windows.Application;
using WpfBrush = System.Windows.Media.Brush;
using WpfSystemColors = System.Windows.SystemColors;

namespace IPMan.App.Services;

/// <summary>Applies the selected theme and follows relevant Windows appearance changes.</summary>
public sealed class WindowsThemeService : IDisposable
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValue = "AppsUseLightTheme";
    private static readonly Action<ILogger, Exception?> ThemeReadFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(1, nameof(ThemeReadFailed)),
        "Windows tema tercihi okunamadı; açık tema kullanılacak.");
    private readonly ILogger<WindowsThemeService> _logger;
    private AppTheme _preference = AppTheme.System;
    private bool _isDisposed;

    /// <summary>Creates a Windows appearance observer.</summary>
    public WindowsThemeService(ILogger<WindowsThemeService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    /// <summary>Applies a persisted preference immediately.</summary>
    public void Apply(AppTheme preference)
    {
        _preference = Enum.IsDefined(preference) ? preference : AppTheme.System;
        ApplyCurrentTheme();
    }

    /// <summary>Applies title-bar appearance after a window has an HWND.</summary>
    public void AttachWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.SourceInitialized += OnWindowSourceInitialized;
        if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
        {
            ApplyTitleBar(window, GetEffectiveTheme());
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _isDisposed = true;
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (_isDisposed)
        {
            return;
        }

        WpfApplication? application = WpfApplication.Current;
        if (application is not null)
        {
            _ = application.Dispatcher.BeginInvoke(ApplyCurrentTheme);
        }
    }

    private void ApplyCurrentTheme()
    {
        if (_isDisposed || WpfApplication.Current is not WpfApplication application)
        {
            return;
        }

        EffectiveTheme effectiveTheme = GetEffectiveTheme();
        ResourceDictionary dictionary = CreateThemeDictionary(effectiveTheme);
        System.Collections.ObjectModel.Collection<ResourceDictionary> dictionaries = application.Resources.MergedDictionaries;
        if (dictionaries.Count == 0)
        {
            dictionaries.Add(dictionary);
        }
        else
        {
            // Replacing the dictionary keeps controls bound through DynamicResource intact.
            dictionaries[0] = dictionary;
        }

        foreach (Window window in application.Windows)
        {
            ApplyTitleBar(window, effectiveTheme);
        }
    }

    private EffectiveTheme GetEffectiveTheme() =>
        ThemeResolver.Resolve(_preference, ReadSystemIsLight(), SystemParameters.HighContrast);

    private bool? ReadSystemIsLight()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            object? value = key?.GetValue(AppsUseLightThemeValue);
            return value switch
            {
                int integer => integer != 0,
                long integer => integer != 0,
                _ => null
            };
        }
        catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            ThemeReadFailed(_logger, exception);
            return null;
        }
    }

    private static ResourceDictionary CreateThemeDictionary(EffectiveTheme effectiveTheme)
    {
        if (effectiveTheme != EffectiveTheme.HighContrast)
        {
            string source = effectiveTheme == EffectiveTheme.Dark ? "Themes/Dark.xaml" : "Themes/Light.xaml";
            return new ResourceDictionary { Source = new Uri(source, UriKind.Relative) };
        }

        WpfBrush background = WpfSystemColors.WindowBrush;
        WpfBrush foreground = WpfSystemColors.WindowTextBrush;
        WpfBrush border = WpfSystemColors.WindowFrameBrush;
        return new ResourceDictionary
        {
            ["WindowBackgroundBrush"] = background,
            ["SurfaceBrush"] = background,
            ["BorderBrush"] = border,
            ["PrimaryTextBrush"] = foreground,
            ["SecondaryTextBrush"] = foreground,
            ["ControlBackgroundBrush"] = WpfSystemColors.ControlBrush,
            ["ControlHoverBrush"] = WpfSystemColors.HighlightBrush,
            ["ControlDisabledBrush"] = WpfSystemColors.GrayTextBrush,
            ["AccentBrush"] = WpfSystemColors.HighlightBrush,
            ["FocusBrush"] = WpfSystemColors.HighlightBrush,
            ["ConnectedBrush"] = foreground,
            ["DisconnectedBrush"] = foreground,
            ["WarningBackgroundBrush"] = background,
            ["WarningBorderBrush"] = border,
            ["ErrorTextBrush"] = foreground,
            ["SuccessBackgroundBrush"] = background,
            ["SuccessBorderBrush"] = border,
            ["InfoBackgroundBrush"] = background,
            ["InfoBorderBrush"] = border,
            ["ErrorBackgroundBrush"] = background,
            ["ErrorBorderBrush"] = border
        };
    }

    private void OnWindowSourceInitialized(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            ApplyTitleBar(window, GetEffectiveTheme());
        }
    }

    private static void ApplyTitleBar(Window window, EffectiveTheme effectiveTheme)
    {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        int useDarkMode = effectiveTheme == EffectiveTheme.Dark ? 1 : 0;
        if (DwmSetWindowAttribute(handle, 20, ref useDarkMode, sizeof(int)) != 0)
        {
            _ = DwmSetWindowAttribute(handle, 19, ref useDarkMode, sizeof(int));
        }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int attributeValue,
        int attributeSize);
}
