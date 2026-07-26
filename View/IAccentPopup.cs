using Point = System.Windows.Point;

namespace MacAccents.View;

/// <summary>
/// Abstraction of the accent selection popup, so the controller does not depend
/// on a concrete WPF window.
/// </summary>
public interface IAccentPopup
{
    /// <summary>Raised when the user picks a variant (keyboard or mouse).</summary>
    event Action<char>? VariantChosen;

    /// <summary>Shows the popup near the given screen anchor (physical pixels).</summary>
    void Show(Point screenAnchor, IReadOnlyList<char> variants);

    /// <summary>Moves the highlight by <paramref name="delta"/> positions.</summary>
    void MoveHighlight(int delta);

    /// <summary>Chooses the currently highlighted variant.</summary>
    void ChooseHighlighted();

    /// <summary>Chooses a variant by its 1-based number; returns false if out of
    /// range.</summary>
    bool ChooseByNumber(int oneBasedIndex);

    /// <summary>Closes the popup without raising a choice.</summary>
    void Close();
}
