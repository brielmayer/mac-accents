using System.Runtime.InteropServices;
using static MacAccents.Interop.NativeMethods;
using Point = System.Windows.Point;

namespace MacAccents.Interop;

/// <summary>
/// Determines the screen position of the text caret in the foreground window.
/// Falls back to the mouse cursor when no caret is available — not every
/// application reports its caret position to Windows.
/// </summary>
public sealed class CaretLocator : ICaretLocator
{
    public Point GetAnchorPoint()
        => TryGetCaret(out var caret) ? caret : GetCursor();

    private static bool TryGetCaret(out Point point)
    {
        point = default;

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

        point = new Point(pt.X, pt.Y);
        return true;
    }

    private static Point GetCursor()
        => GetCursorPos(out var p) ? new Point(p.X, p.Y) : new Point(0, 0);
}
