using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MacAccents.View;

/// <summary>
/// Notification-area (tray) presence: the application icon plus a context menu.
/// Wraps the WinForms <see cref="NotifyIcon"/> and exposes intent as events,
/// keeping the composition root free of UI plumbing.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private static readonly Uri IconUri = new("pack://application:,,,/Assets/app.ico");

    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _icon;

    public event Action? SettingsRequested;
    public event Action? ExitRequested;

    public TrayIcon()
    {
        _icon = LoadIcon();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings…", null, (_, _) => SettingsRequested?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke());

        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Visible = true,
            Text = "MacAccents – hold a key for accents (ä ö ü …)",
            ContextMenuStrip = menu,
        };

        // Double-click opens settings, matching common tray conventions.
        _notifyIcon.DoubleClick += (_, _) => SettingsRequested?.Invoke();
    }

    public void ShowStartupHint()
        => _notifyIcon.ShowBalloonTip(3000, "MacAccents is running",
            "Hold e.g. 'a' to choose ä / á / à …", ToolTipIcon.Info);

    private static Icon LoadIcon()
    {
        using Stream stream = System.Windows.Application.GetResourceStream(IconUri)!.Stream;
        return new Icon(stream);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon.Dispose();
    }
}
