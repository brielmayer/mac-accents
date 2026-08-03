namespace MacAccents.Interop;

/// <summary>Locates where the accent popup should appear on screen.</summary>
public interface ICaretLocator
{
    /// <summary>Resolves the anchor in physical screen pixels. Never throws and
    /// never runs longer than its own timeout. In the worst case it reports the
    /// mouse pointer. Completes synchronously where the cheap strategy suffices,
    /// so the popup can still open within the same dispatcher turn.</summary>
    Task<CaretAnchor> GetAnchorAsync();
}
