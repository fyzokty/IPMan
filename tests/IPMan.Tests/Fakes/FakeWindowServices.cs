using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using IPMan.App.Presentation;
using IPMan.App.Services;
using IPMan.Application.Logging;
using IPMan.Application.Settings;
using IPMan.Domain.Settings;

namespace IPMan.Tests.Fakes;

/// <summary>Constructor-free placeholders for sealed services used only for event wiring by unshown windows.</summary>
internal static class FakeWindowServices
{
    // Never invoke tray operations or Dispose: the native NotifyIcon is deliberately absent.
    internal static TrayIconManager CreateTray() =>
        (TrayIconManager)RuntimeHelpers.GetUninitializedObject(typeof(TrayIconManager));

    // AttachWindow only subscribes to SourceInitialized while the window has no HWND.
    internal static WindowsThemeService CreateTheme() =>
        (WindowsThemeService)RuntimeHelpers.GetUninitializedObject(typeof(WindowsThemeService));

    internal static T CreateWindow<T>(Action<Window> initializeResources, params object[] arguments) where T : Window
    {
        // Initialize the WPF base first to seed instance resources before InitializeComponent.
        // The actual constructor then runs with those resources and fake dependencies; the
        // repeated base construction retains Resources. No Application or HWND is created.
        T window = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        typeof(Window).GetConstructor(Type.EmptyTypes)!.Invoke(window, BindingFlags.Default, null, [], null);
        initializeResources(window);
        typeof(T).GetConstructors().Single().Invoke(window, BindingFlags.Default, null, arguments, null);
        return window;
    }
}

internal sealed class FakeWindowSettingsRepository(AppSettings settings) : IAppSettingsRepository
{
    public bool LastLoadFailed => false;

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(settings);

    public Task<SettingsSaveResult> SaveAsync(AppSettings settings, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Window loading must not save settings.");
}

internal sealed class FakeWindowSessionMarker : ISessionMarker
{
    public bool TryConsumeStale() => throw new InvalidOperationException("Window loading must not read a session marker.");

    public void Create() => throw new InvalidOperationException("Window loading must not create a session marker.");

    public void Delete() => throw new InvalidOperationException("Window loading must not delete a session marker.");
}

internal sealed class FakeWindowProfileDialogs : IProfileFileDialogService, IUserTextInputService
{
    public Stream? OpenProfile() => throw new InvalidOperationException("Window loading must not open a file dialog.");

    public Stream? CreateProfile(string suggestedFileName) => throw new InvalidOperationException("Window loading must not open a file dialog.");

    public string? Request(string title, string label, string initialValue) =>
        throw new InvalidOperationException("Window loading must not open a prompt.");
}
