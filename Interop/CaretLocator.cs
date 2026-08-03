using System.Runtime.InteropServices;
using static MacAccents.Interop.NativeMethods;
using Point = System.Windows.Point;

namespace MacAccents.Interop;

/// <summary>
/// Determines the screen position of the text caret, trying four strategies in
/// order of cost and reliability:
///
///   1. GUITHREADINFO.rcCaret: the Win32 caret. Cheap and exact, but only
///      classic applications (Notepad, Win32 edit controls) report one.
///   2. UI Automation TextPattern: reaches the caret in applications that draw
///      it themselves, such as Chromium/Electron, WPF, WinUI, Office and Qt.
///   3. The focused element's bounding rectangle: no caret, but at least the
///      right control.
///   4. The mouse cursor: last resort.
///
/// Strategies 2 and 3 are cross-process COM calls of unpredictable duration, so
/// they run on the thread pool and the caller awaits them rather than blocking:
/// the calling thread is the one serving the low-level keyboard hook, and
/// stalling it stalls typing system-wide. Resolution happens only when a popup is
/// actually about to open, never speculatively per keystroke.
/// </summary>
public sealed class CaretLocator : ICaretLocator
{
    /// <summary>How long a UI Automation lookup may take before it is abandoned.
    /// Kept short: a popup in the wrong place beats a popup that arrives late.
    /// The abandoned lookup finishes on the thread pool and its result is dropped.
    /// </summary>
    private static readonly TimeSpan ResolveTimeout = TimeSpan.FromMilliseconds(50);

    public async Task<CaretAnchor> GetAnchorAsync()
    {
        // The Win32 caret is cheap and needs no thread hop, so where it works the
        // popup still opens within the same dispatcher turn.
        if (TryGetWin32Caret(out CaretAnchor caret))
            return caret;

        return await ResolveViaUiaAsync().ConfigureAwait(true)
            ?? CaretAnchor.FromMouse(GetCursor());
    }

    private static async Task<CaretAnchor?> ResolveViaUiaAsync()
    {
        Task<CaretAnchor?> lookup = Task.Run(UiaCaretLocator.TryResolve);

        if (await Task.WhenAny(lookup, Task.Delay(ResolveTimeout)).ConfigureAwait(true) != lookup)
            return null;

        if (lookup.IsCompletedSuccessfully)
            return lookup.Result;

        // Observe the failure so it cannot resurface as an unobserved exception,
        // then fall through to the next strategy.
        _ = lookup.Exception;
        return null;
    }

    private static bool TryGetWin32Caret(out CaretAnchor anchor)
    {
        anchor = default;

        IntPtr foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero) return false;

        uint threadId = GetWindowThreadProcessId(foreground, out _);

        var info = new GUITHREADINFO { cbSize = Marshal.SizeOf<GUITHREADINFO>() };
        if (!GetGUIThreadInfo(threadId, ref info) || info.hwndCaret == IntPtr.Zero)
            return false;

        RECT caret = info.rcCaret;

        // An empty caret rectangle means "no real caret" -> fall back.
        if (caret.Bottom == caret.Top && caret.Left == caret.Right)
            return false;

        // Anchor at the caret's bottom-left, in screen coordinates.
        var pt = new POINT { X = caret.Left, Y = caret.Bottom };
        if (!ClientToScreen(info.hwndCaret, ref pt))
            return false;

        anchor = new CaretAnchor(
            new Point(pt.X, pt.Y),
            Math.Max(0, caret.Bottom - caret.Top),
            AnchorSource.WindowsCaret);
        return true;
    }

    private static Point GetCursor()
        => GetCursorPos(out var p) ? new Point(p.X, p.Y) : new Point(0, 0);
}
