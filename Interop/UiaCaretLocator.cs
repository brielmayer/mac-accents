using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using Point = System.Windows.Point;

namespace MacAccents.Interop;

/// <summary>
/// Resolves the caret through UI Automation, which is the only way to reach it in
/// applications that draw their own cursor instead of using the Win32 caret API:
/// Chromium and Electron (VS Code, Slack, Teams), WPF, WinUI, Office, Qt.
///
/// Every call here is a cross-process COM call and may block for a long time, so
/// this type must only ever run on a background thread, never on the thread that
/// serves the keyboard hook.
/// </summary>
internal static class UiaCaretLocator
{
    /// <summary>Returns the caret anchor in physical screen pixels, or null when
    /// the focused element reveals nothing usable.</summary>
    internal static CaretAnchor? TryResolve()
    {
        try
        {
            AutomationElement? focused = AutomationElement.FocusedElement;
            if (focused is null)
                return null;

            return TryFromTextPattern(focused) ?? TryFromBoundingRectangle(focused);
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            return null;
        }
    }

    /// <summary>Queries UI Automation once so Chromium-based applications build
    /// their accessibility tree before the first real lookup. Their very first
    /// response to a new client is typically empty. Costs those applications a
    /// little memory and CPU, which is the price of knowing where their caret is.
    /// </summary>
    internal static void Warmup()
    {
        try
        {
            _ = AutomationElement.FocusedElement;
        }
        catch
        {
            // Deliberately catch-all, unlike TryResolve: this runs fire-and-forget
            // from App.OnStartup, so anything escaping would become an unobserved
            // task exception. A warm-up that fails costs nothing: the first real
            // lookup simply falls back one step further.
        }
    }

    /// <summary>The caret is the start of the current text selection.</summary>
    private static CaretAnchor? TryFromTextPattern(AutomationElement focused)
    {
        if (!focused.TryGetCurrentPattern(TextPattern.Pattern, out object pattern))
            return null;

        TextPatternRange[] selection = ((TextPattern)pattern).GetSelection();
        if (selection is null || selection.Length == 0)
            return null;

        TextPatternRange range = selection[0].Clone();

        // Collapse onto the selection start, which is where the caret sits.
        range.MoveEndpointByRange(TextPatternRangeEndpoint.End, range, TextPatternRangeEndpoint.Start);

        Rect[] rects = range.GetBoundingRectangles();
        if (rects.Length == 0)
        {
            // A degenerate range usually reports no rectangle at all: widen it by
            // one character so there is something to measure.
            range.ExpandToEnclosingUnit(TextUnit.Character);
            rects = range.GetBoundingRectangles();
        }

        return rects.Length == 0 ? null : ToAnchor(rects[0], AnchorSource.UiaTextRange);
    }

    /// <summary>No text pattern, but the focused control still tells us its box,
    /// which is far better than the mouse pointer.</summary>
    private static CaretAnchor? TryFromBoundingRectangle(AutomationElement focused)
        => ToAnchor(focused.Current.BoundingRectangle, AnchorSource.UiaElementBounds);

    private static CaretAnchor? ToAnchor(Rect rect, AnchorSource source)
    {
        if (rect.IsEmpty || !double.IsFinite(rect.Left) || !double.IsFinite(rect.Bottom))
            return null;

        // Some providers report offscreen elements at coordinates nowhere near a
        // monitor. Rejecting those falls through to the mouse, which is honest;
        // accepting them would clamp the popup into an arbitrary screen corner.
        // VirtualScreen is in physical pixels, the same unit as UI Automation's.
        System.Drawing.Rectangle screens = System.Windows.Forms.SystemInformation.VirtualScreen;
        if (!screens.Contains((int)Math.Floor(rect.Left), (int)Math.Floor(rect.Bottom)))
            return null;

        return new CaretAnchor(new Point(rect.Left, rect.Bottom), Math.Max(0, rect.Height), source);
    }

    /// <summary>UI Automation throws routinely when the target window closes, is
    /// busy, or simply does not implement what we asked for. All of that is normal
    /// operation and means "try the next strategy".</summary>
    private static bool IsExpected(Exception ex)
        => ex is ElementNotAvailableException
            or ElementNotEnabledException
            or InvalidOperationException
            or NotSupportedException
            or TimeoutException
            or COMException;
}
