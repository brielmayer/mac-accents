using Point = System.Windows.Point;

namespace MacAccents.Interop;

/// <summary>Which strategy produced an anchor. Ordered from most to least
/// precise; see <see cref="CaretLocator"/> for the cascade.</summary>
public enum AnchorSource
{
    /// <summary>The Win32 caret, reported by the application itself.</summary>
    WindowsCaret,

    /// <summary>A UI Automation text range collapsed onto the caret.</summary>
    UiaTextRange,

    /// <summary>The bounding box of the focused element: the right control, but
    /// not the caret within it.</summary>
    UiaElementBounds,

    /// <summary>Nothing was known about the caret; the mouse pointer stood in.</summary>
    MousePointer,
}

/// <summary>
/// Where the accent popup should be anchored, in physical screen pixels.
/// </summary>
/// <param name="BottomLeft">Bottom-left corner of the text caret (or of whatever
/// stood in for it).</param>
/// <param name="LineHeight">Height of the text line the caret sits on. Zero when
/// only a bare point is known.</param>
/// <param name="Source">Which strategy produced this anchor.</param>
public readonly record struct CaretAnchor(Point BottomLeft, double LineHeight, AnchorSource Source)
{
    public static CaretAnchor FromMouse(Point point) => new(point, 0, AnchorSource.MousePointer);
}
