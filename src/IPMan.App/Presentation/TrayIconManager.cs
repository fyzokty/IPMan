using System.Drawing;
using System.Windows.Forms;
using IPMan.App.Resources;
using IPMan.Application.Common;

namespace IPMan.App.Presentation;

/// <summary>Owns the temporary notification-area icon shown while the window is hidden.</summary>
public sealed class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private bool _isDisposed;

    /// <summary>Initializes the icon and its minimal context menu.</summary>
    public TrayIconManager(IApplicationVersionProvider versionProvider)
    {
        ArgumentNullException.ThrowIfNull(versionProvider);

        ContextMenuStrip menu = new();
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
    }

    /// <summary>Raised when the user asks to show the window.</summary>
    public event EventHandler? OpenRequested;

    /// <summary>Raised when the user asks to exit the application.</summary>
    public event EventHandler? ExitRequested;

    /// <summary>Makes the icon visible.</summary>
    public void Show() => _notifyIcon.Visible = true;

    /// <summary>Hides the icon.</summary>
    public void Hide() => _notifyIcon.Visible = false;

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
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
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

    private void OnExitClick(object? sender, EventArgs e) =>
        ExitRequested?.Invoke(this, EventArgs.Empty);
}
