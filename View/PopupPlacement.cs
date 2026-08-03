using System.Windows;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace MacAccents.View;

/// <summary>
/// The arithmetic that decides where the popup goes. Pure and free of any WPF
/// window, so it can be read (and tested) on its own; <see cref="AccentPopup"/>
/// keeps only the parts that genuinely need a window (DPI conversion, work area,
/// setting Left/Top).
///
/// Every value is in device-independent units.
/// </summary>
internal static class PopupPlacement
{
    /// <summary>Breathing room between the popup and the text line it belongs to.</summary>
    private const double VerticalGap = 4;

    /// <summary>Used instead of <see cref="VerticalGap"/> when the anchor is the
    /// mouse pointer, so the popup does not end up under the cursor bitmap.</summary>
    private const double MouseCursorClearance = 24;

    /// <summary>Returns the popup's top-left corner: just above the caret's text
    /// line, dropped below it when there is no room, and always inside
    /// <paramref name="workArea"/>.</summary>
    /// <param name="caret">Bottom-left of the caret.</param>
    /// <param name="lineHeight">Height of the caret's text line; zero when unknown.</param>
    /// <param name="anchoredToMouse">Whether <paramref name="caret"/> is really
    /// just the mouse pointer, which needs more clearance.</param>
    internal static Point Compute(
        Point caret, double lineHeight, bool anchoredToMouse, Size popup, Rect workArea)
    {
        double left = Clamp(caret.X, workArea.Left, workArea.Right - popup.Width);

        double above = caret.Y - lineHeight - popup.Height - VerticalGap;
        double below = caret.Y + (anchoredToMouse ? MouseCursorClearance : VerticalGap);
        double top = Clamp(
            above >= workArea.Top ? above : below,
            workArea.Top,
            workArea.Bottom - popup.Height);

        return new Point(left, top);
    }

    /// <summary>Keeps a value inside [min, max]. Unlike <see cref="Math.Clamp(double, double, double)"/>
    /// it tolerates an inverted range, which happens when the popup is larger than
    /// the work area. In that case the lower bound wins.</summary>
    private static double Clamp(double value, double min, double max)
        => Math.Max(min, Math.Min(value, max));
}
