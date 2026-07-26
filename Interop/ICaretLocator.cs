using Point = System.Windows.Point;

namespace MacAccents.Interop;

/// <summary>Locates where the accent popup should appear on screen.</summary>
public interface ICaretLocator
{
    /// <summary>Returns the text caret position in physical screen pixels, or
    /// falls back to the mouse cursor when no caret is reported.</summary>
    Point GetAnchorPoint();
}
