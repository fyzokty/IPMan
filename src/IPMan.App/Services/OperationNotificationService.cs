using System.Collections.ObjectModel;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Application.Common;
using IPMan.Application.Settings;
using IPMan.Domain.Settings;

namespace IPMan.App.Services;

/// <summary>Records adapter mutation outcomes and optionally presents them through Windows notifications.</summary>
public sealed class OperationNotificationService
{
    private const int MaximumHistoryCount = 50;

    private readonly IAppSettingsRepository _settingsRepository;
    private readonly TrayIconManager? _trayIconManager;
    private readonly IClock _clock;
    private Func<NotificationWindowState> _windowStateProvider = static () => new(false, false, false);

    /// <summary>Creates the notification history and operating-system notification coordinator.</summary>
    public OperationNotificationService(
        IAppSettingsRepository settingsRepository,
        TrayIconManager trayIconManager,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(settingsRepository);
        ArgumentNullException.ThrowIfNull(trayIconManager);
        ArgumentNullException.ThrowIfNull(clock);

        _settingsRepository = settingsRepository;
        _trayIconManager = trayIconManager;
        _clock = clock;
    }

    /// <summary>Creates a history-only service for hosts where no tray icon is available.</summary>
    public OperationNotificationService(IAppSettingsRepository settingsRepository, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(settingsRepository);
        ArgumentNullException.ThrowIfNull(clock);

        _settingsRepository = settingsRepository;
        _clock = clock;
    }

    /// <summary>Gets the newest-first, session-only operation notification history.</summary>
    public ObservableCollection<OperationNotificationRecord> History { get; } = new();

    /// <summary>Sets the source used to decide whether the main window currently has focus.</summary>
    public void SetWindowStateProvider(Func<NotificationWindowState> windowStateProvider)
    {
        ArgumentNullException.ThrowIfNull(windowStateProvider);
        _windowStateProvider = windowStateProvider;
    }

    /// <summary>Records a relevant operation outcome and shows an OS notification when its mode permits it.</summary>
    public void Report(string operation, string adapterId, string adapterName, bool succeeded)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterId);
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterName);

        string result = succeeded ? Strings.NotificationSucceeded : Strings.NotificationFailed;
        string text = Strings.FormatOperationNotification(operation, adapterName, result);
        History.Insert(0, new OperationNotificationRecord(_clock.UtcNow, operation, adapterId, adapterName, succeeded, text));
        if (History.Count > MaximumHistoryCount)
        {
            History.RemoveAt(History.Count - 1);
        }

        AppSettings settings = _settingsRepository.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        NotificationWindowState windowState = _windowStateProvider();
        if (!ShouldShow(settings.NotificationMode, windowState))
        {
            return;
        }

        _trayIconManager?.ShowNotification(Strings.ApplicationName, text, !succeeded, adapterId);
    }

    /// <summary>Removes all session-only notification history entries.</summary>
    public void Clear() => History.Clear();

    /// <summary>Returns whether the selected notification mode permits a notification.</summary>
    public static bool ShouldShow(NotificationMode mode, NotificationWindowState windowState) =>
        mode == NotificationMode.Always ||
        (mode == NotificationMode.WhenUnfocused &&
            (!windowState.IsActive || windowState.IsMinimized || !windowState.IsVisible));

    /// <summary>Describes the state of the application window relevant to notification display.</summary>
    public readonly record struct NotificationWindowState(bool IsActive, bool IsMinimized, bool IsVisible);

    /// <summary>Represents one session-only adapter operation outcome.</summary>
    public sealed record OperationNotificationRecord(
        DateTimeOffset Timestamp,
        string Operation,
        string AdapterId,
        string AdapterName,
        bool Succeeded,
        string Text);
}
