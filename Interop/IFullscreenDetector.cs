namespace MacAccents.Interop;

/// <summary>Tells whether a full-screen application currently owns the screen —
/// a game, a video in full screen, a presentation.</summary>
public interface IFullscreenDetector
{
    /// <summary>Must be cheap: it is consulted from the low-level keyboard hook
    /// on every key press that could start a hold. Never throws.</summary>
    bool IsFullscreenAppActive();
}
