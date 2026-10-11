using System.Drawing;
using System.Windows.Forms;
using IPMan.App.Resources;
using IPMan.Application.Common;

namespace IPMan.App.Presentation;

/// <summary>Owns the notification-area icon and its snapshot-based menu.</summary>
public sealed class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private bool _alwaysVisible;
    private string? _notificationAdapterId;
    private bool _restoreVisibilityAfterNotification;
    private bool _isDisposed;

    /// <summary>Initializes the icon and its minimal context menu.</summary>
    public TrayIconManager(IApplicationVersionProvider versionProvider)
    {
        ArgumentNullException.ThrowIfNull(versionProvider);

        ContextMenuStrip menu = new();
        _menu = menu;
        menu.Opening += OnMenuOpening;
        ToolStripMenuItem productItem = new(Strings.FormatTrayApplicationVersion(versionProvider.Version))
        {
            Enabled = false
        };
        ToolStripMenuItem openItem = new(Strings.TrayOpen);
        ToolStripMenuItem exitItem = new(Strings.TrayExit);
        openItem.Click += OnOpenClick;
        exitItem.Click += OnExitClick;
        menu.Items.Add(productItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(openItem);
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = GetApplicationIcon(),
            Text = Strings.ApplicationName,
            Visible = false
        };
        _notifyIcon.MouseClick += OnMouseClick;
        _notifyIcon.DoubleClick += OnDoubleClick;
        _notifyIcon.BalloonTipClicked += OnBalloonTipClicked;
        _notifyIcon.BalloonTipClosed += OnBalloonTipClosed;
    }

    /// <summary>Raised when the user asks to show the window.</summary>
    public event EventHandler? OpenRequested;

    /// <summary>Raised when the user asks to exit the application.</summary>
    public event EventHandler? ExitRequested;

    public event EventHandler<string?>? SwitchRequested;

    public event EventHandler? UndoRequested;

    public event EventHandler? MoreSettingsRequested;

    /// <summary>Evaluated on the UI thread immediately before the menu opens.</summary>
    public Func<TrayMenuModel>? MenuProvider { get; set; }

    /// <summary>Opt-in persistent icon visibility; defaults to the existing temporary behavior.</summary>
    public bool AlwaysVisible
    {
        get => _alwaysVisible;
        set
        {
            _alwaysVisible = value;
            if (value)
            {
                _restoreVisibilityAfterNotification = false;
                Show();
            }
        }
    }

    internal ContextMenuStrip Menu => _menu;

    /// <summary>Rebuilds menu items on the UI thread without changing icon visibility.</summary>
    public void Update(TrayMenuModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ClearMenu();
        _menu.Items.Add(new ToolStripMenuItem(model.Title) { Enabled = false });
        _menu.Items.Add(new ToolStripLabel(model.StatusText)
        {
            Image = CreateStatusDot(model.StatusColor)
        });
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem(Strings.TraySwitch) { Enabled = false });
        foreach (TraySettingItem setting in model.Settings)
        {
            ToolStripMenuItem item = new(setting.Name)
            {
                Checked = setting.IsActive,
                Enabled = setting.IsEnabled
            };
            item.Click += (_, _) => SwitchRequested?.Invoke(this, setting.ProfileId);
            _menu.Items.Add(item);
        }

        if (model.HasMoreSettings)
        {
            _menu.Items.Add(new ToolStripMenuItem(Strings.TrayMoreSettings, null,
                (_, _) => MoreSettingsRequested?.Invoke(this, EventArgs.Empty)));
        }

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem(Strings.TrayUndo, null,
            (_, _) => UndoRequested?.Invoke(this, EventArgs.Empty)) { Enabled = model.CanUndo });
        _menu.Items.Add(new ToolStripMenuItem(Strings.TrayOpen, null, OnOpenClick));
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem(Strings.TrayExit, null, OnExitClick));
    }

    internal void RefreshMenu()
    {
        if (MenuProvider is not null)
        {
            Update(MenuProvider());
        }
    }

    private void OnMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e) => RefreshMenu();

    private static Bitmap CreateStatusDot(TrayStatusColor color)
    {
        Bitmap bitmap = new(16, 16);
        using Graphics graphics = Graphics.FromImage(bitmap);
        using SolidBrush brush = new(color switch
        {
            TrayStatusColor.Green => Color.SeaGreen,
            TrayStatusColor.Amber => Color.DarkGoldenrod,
            _ => SystemColors.GrayText
        });
        graphics.FillEllipse(brush, 4, 4, 8, 8);
        return bitmap;
    }

    private void ClearMenu()
    {
        while (_menu.Items.Count > 0)
        {
            ToolStripItem item = _menu.Items[0];
            _menu.Items.RemoveAt(0);
            item.Image?.Dispose();
            item.Dispose();
        }
    }

    /// <summary>Allows the safety coordinator to complete keep/restore or cancel before publishing exit.</summary>
    public Func<Task<bool>>? ExitGuard { get; set; }

    /// <summary>Raised when an operation-result notification is clicked.</summary>
    public event EventHandler<string>? NotificationClicked;

    /// <summary>Makes the icon visible.</summary>
    public void Show() => _notifyIcon.Visible = true;

    /// <summary>Hides the icon.</summary>
    public void Hide() => _notifyIcon.Visible = AlwaysVisible;

    /// <summary>Attempts to show a non-critical balloon notification.</summary>
    public void ShowBalloon(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        try
        {
            _notifyIcon.ShowBalloonTip(3000, Strings.ApplicationName, text, ToolTipIcon.Info);
        }
        catch (InvalidOperationException)
        {
            // Windows can reject a notification when notifications are unavailable.
        }
    }

    /// <summary>Shows an operating-system notification associated with an adapter.</summary>
    public void ShowNotification(string title, string text, bool isError, string adapterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterId);

        bool wasVisible = _notifyIcon.Visible;
        _notificationAdapterId = adapterId;
        _restoreVisibilityAfterNotification = !wasVisible;
        try
        {
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(3000, title, text, isError ? ToolTipIcon.Error : ToolTipIcon.Info);
        }
        catch (InvalidOperationException)
        {
            // Windows can reject a notification when notifications are unavailable.
            _notificationAdapterId = null;
            _restoreVisibilityAfterNotification = false;
            _notifyIcon.Visible = wasVisible;
        }
    }

    /// <summary>Shows an operating-system notification that is not tied to an adapter.</summary>
    public void ShowSystemNotification(string title, string text, bool isError)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        bool wasVisible = _notifyIcon.Visible;
        _notificationAdapterId = null;
        _restoreVisibilityAfterNotification = !wasVisible;
        try
        {
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(3000, title, text, isError ? ToolTipIcon.Error : ToolTipIcon.Info);
        }
        catch (InvalidOperationException)
        {
            // Windows can reject a notification when notifications are unavailable.
            _restoreVisibilityAfterNotification = false;
            _notifyIcon.Visible = wasVisible;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _notifyIcon.MouseClick -= OnMouseClick;
        _notifyIcon.DoubleClick -= OnDoubleClick;
        _notifyIcon.BalloonTipClicked -= OnBalloonTipClicked;
        _notifyIcon.BalloonTipClosed -= OnBalloonTipClosed;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Opening -= OnMenuOpening;
        ClearMenu();
        _menu.Dispose();
    }

    private static Icon GetApplicationIcon() =>
        Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? string.Empty) ?? SystemIcons.Application;

    private void OnMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            OpenRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnDoubleClick(object? sender, EventArgs e) =>
        OpenRequested?.Invoke(this, EventArgs.Empty);

    private void OnOpenClick(object? sender, EventArgs e) =>
        OpenRequested?.Invoke(this, EventArgs.Empty);

    private async void OnExitClick(object? sender, EventArgs e)
        => await RequestExitAsync();

    internal async Task RequestExitAsync()
    {
        if (ExitGuard is null || await ExitGuard())
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnBalloonTipClicked(object? sender, EventArgs e)
    {
        try
        {
            if (_notificationAdapterId is not null)
            {
                NotificationClicked?.Invoke(this, _notificationAdapterId);
            }
        }
        finally
        {
            RestoreVisibilityAfterNotification();
        }
    }

    private void OnBalloonTipClosed(object? sender, EventArgs e) => RestoreVisibilityAfterNotification();

    private void RestoreVisibilityAfterNotification()
    {
        _notificationAdapterId = null;
        if (!_restoreVisibilityAfterNotification)
        {
            return;
        }

        _restoreVisibilityAfterNotification = false;
        _notifyIcon.Visible = AlwaysVisible;
    }
}
