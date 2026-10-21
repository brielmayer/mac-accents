using System.Runtime.InteropServices;
using static MacAccents.Interop.NativeMethods;

namespace MacAccents.Interop;

/// <summary>
/// Recognizes a full-screen application by two complementary signals:
///
///   1. SHQueryUserNotificationState: the shell's own notion of "do not disturb".
///      It reports exclusive-fullscreen Direct3D and presentation mode, which is
///      exactly what the accent popup must stay out of — but it says nothing
///      about borderless-fullscreen windows, the way most modern games run.
///   2. Geometry: the foreground window covers its whole monitor and has no
///      title bar. A maximized ordinary window keeps its caption, so it is not
///      mistaken for a game even when the taskbar auto-hides and the work area
///      equals the monitor.
///
/// Both are heuristics, and they are deliberately biased towards staying quiet:
/// a missing accent popup in a full-screen video costs a keystroke, a popup over
/// a game costs the key repeat the game is waiting for.
///
/// Threading: called on the hook/UI thread only, so the cache needs no
/// synchronization.
/// </summary>
public sealed class FullscreenDetector : IFullscreenDetector
{
    /// <summary>How long a verdict is reused. Long enough that holding a key
    /// never pays for the probe twice, short enough that alt-tabbing out of a
    /// game is noticed before the next word is typed.</summary>
    private const long CacheLifetimeMs = 500;

    private IntPtr _cachedWindow;
    private long _cachedAt;
    private bool _cachedVerdict;

    public bool IsFullscreenAppActive()
    {
        IntPtr foreground = GetForegroundWindow();
        long now = Environment.TickCount64;

        if (foreground == _cachedWindow && now - _cachedAt < CacheLifetimeMs)
            return _cachedVerdict;

        _cachedWindow = foreground;
        _cachedAt = now;
        _cachedVerdict = Probe(foreground);
        return _cachedVerdict;
    }

    private static bool Probe(IntPtr foreground)
    {
        if (SHQueryUserNotificationState(out QueryUserNotificationState state) == 0 &&
            state is QueryUserNotificationState.RunningD3dFullScreen
                  or QueryUserNotificationState.PresentationMode)
            return true;

        if (foreground == IntPtr.Zero ||
            foreground == GetShellWindow() ||
            foreground == GetDesktopWindow())
            return false;

        // A title bar means a regular window, however large it has been made.
        if (((long)GetWindowLongPtr(foreground, GWL_STYLE) & WS_CAPTION) == WS_CAPTION)
            return false;

        return CoversWholeMonitor(foreground);
    }

    private static bool CoversWholeMonitor(IntPtr window)
    {
        if (!GetWindowRect(window, out RECT bounds))
            return false;

        var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        IntPtr monitor = MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST);
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref monitorInfo))
            return false;

        RECT screen = monitorInfo.rcMonitor;
        return bounds.Left <= screen.Left
            && bounds.Top <= screen.Top
            && bounds.Right >= screen.Right
            && bounds.Bottom >= screen.Bottom;
    }
}
