using System.Windows.Threading;
using MacAccents.Accents;
using MacAccents.Input;
using MacAccents.Interop;
using MacAccents.View;

namespace MacAccents;

/// <summary>
/// Orchestrates the macOS-style press-and-hold behaviour as an explicit state
/// machine:
///
///   Idle      -- accent key down --> Holding    (character types normally)
///   Holding   -- hold timer -------> PopupOpen  (accent popup appears)
///   Holding   -- key up / other ---> Idle       (short tap: nothing extra)
///   PopupOpen -- selection --------> Idle        (base char replaced by variant)
///
/// Key repeat of the held key is swallowed throughout, so holding never yields
/// "aaaa". All members run on the UI thread (see <see cref="KeyboardHook"/>),
/// so no synchronization is required.
/// </summary>
public sealed class AccentController : IDisposable
{
    private enum State { Idle, Holding, PopupOpen }

    // Selection keys used while the popup is open.
    private const int VkEscape = 0x1B;
    private const int VkReturn = 0x0D;
    private const int VkSpace = 0x20;
    private const int VkTab = 0x09;
    private const int VkLeft = 0x25;
    private const int VkRight = 0x27;
    private const int VkDigit1 = 0x31;
    private const int VkDigit9 = 0x39;

    private readonly KeyboardHook _hook;
    private readonly IKeyboardCharacterResolver _resolver;
    private readonly IAccentProvider _accents;
    private readonly IInputSimulator _input;
    private readonly ICaretLocator _caret;
    private readonly Func<IAccentPopup> _popupFactory;
    private readonly AppOptions _options;
    private readonly DispatcherTimer _holdTimer;

    private State _state = State.Idle;
    private int _activeKey;
    private char _activeChar;
    private IReadOnlyList<char> _activeVariants = Array.Empty<char>();
    private IAccentPopup? _popup;

    // After a selection, the base key may still be physically held. Swallow its
    // auto-repeats until it is released, so no stray base characters are typed.
    private int _swallowKeyUntilRelease;

    public AccentController(
        KeyboardHook hook,
        IKeyboardCharacterResolver resolver,
        IAccentProvider accents,
        IInputSimulator input,
        ICaretLocator caret,
        Func<IAccentPopup> popupFactory,
        AppOptions options,
        Dispatcher dispatcher)
    {
        _hook = hook;
        _resolver = resolver;
        _accents = accents;
        _input = input;
        _caret = caret;
        _popupFactory = popupFactory;
        _options = options;

        _holdTimer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher);
        _holdTimer.Tick += (_, _) => OnHoldElapsed();

        _hook.KeyDown += OnKeyDown;
        _hook.KeyUp += OnKeyUp;
    }

    private void OnKeyDown(object? sender, KeyboardHookEventArgs e)
    {
        // A key selected from the popup but still held down: swallow its repeats.
        if (_swallowKeyUntilRelease != 0 && e.VirtualKey == _swallowKeyUntilRelease)
        {
            e.Suppress = true;
            return;
        }

        switch (_state)
        {
            case State.Idle:
                e.Suppress = HandleIdleKeyDown(e.VirtualKey);
                break;
            case State.Holding:
                e.Suppress = HandleHoldingKeyDown(e.VirtualKey);
                break;
            case State.PopupOpen:
                e.Suppress = HandlePopupKeyDown(e.VirtualKey);
                break;
        }
    }

    private void OnKeyUp(object? sender, KeyboardHookEventArgs e)
    {
        if (_swallowKeyUntilRelease != 0 && e.VirtualKey == _swallowKeyUntilRelease)
        {
            _swallowKeyUntilRelease = 0;
            return;
        }

        if (e.VirtualKey != _activeKey) return;

        switch (_state)
        {
            case State.Holding:
                // Short tap: the character was already typed, nothing else to do.
                ReturnToIdle();
                break;
            case State.PopupOpen:
                // The popup stays open after release; the user can still pick
                // with a number or the mouse. Only stop tracking key repeat.
                _activeKey = 0;
                break;
        }
    }

    /// <summary>Idle: start tracking an accent-capable key; let the character
    /// through so it types normally.</summary>
    private bool HandleIdleKeyDown(int virtualKey)
    {
        if (!TryBeginHold(virtualKey))
            ReturnToIdle();
        return false;
    }

    /// <summary>Holding: swallow key repeat; any other key ends the hold.</summary>
    private bool HandleHoldingKeyDown(int virtualKey)
    {
        if (virtualKey == _activeKey)
            return true; // swallow auto-repeat

        // A different key: end tracking, then re-evaluate it as a fresh press so
        // typing stays fluid.
        ReturnToIdle();
        TryBeginHold(virtualKey);
        return false;
    }

    private static bool IsShiftDown
        => (NativeMethods.GetKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;

    /// <summary>PopupOpen: interpret navigation/selection keys, swallow repeat,
    /// dismiss on anything else.</summary>
    private bool HandlePopupKeyDown(int virtualKey)
    {
        if (virtualKey is >= VkDigit1 and <= VkDigit9)
        {
            _popup?.ChooseByNumber(virtualKey - VkDigit1 + 1);
            return true;
        }

        switch (virtualKey)
        {
            case VkLeft: _popup?.MoveHighlight(-1); return true;
            case VkRight: _popup?.MoveHighlight(+1); return true;
            case VkTab: _popup?.MoveHighlight(IsShiftDown ? -1 : +1); return true;
            case VkReturn or VkSpace: _popup?.ChooseHighlighted(); return true;
            case VkEscape: DismissPopup(); return true;
        }

        if (virtualKey == _activeKey)
            return true; // swallow auto-repeat of the still-held key

        // Any other key dismisses the popup and is passed through.
        DismissPopup();
        return false;
    }

    /// <summary>Starts hold tracking if the key maps to a character with
    /// accents. Returns false if it does not.</summary>
    private bool TryBeginHold(int virtualKey)
    {
        char? resolved = _resolver.Resolve(virtualKey);
        if (resolved is not char c)
            return false;

        IReadOnlyList<char>? variants = _accents.GetVariants(c);
        if (variants is null || variants.Count == 0)
            return false;

        _activeKey = virtualKey;
        _activeChar = c;
        _activeVariants = variants;
        _state = State.Holding;

        _holdTimer.Interval = _options.HoldDelay;
        _holdTimer.Start();
        return true;
    }

    private void OnHoldElapsed()
    {
        _holdTimer.Stop();
        if (_state != State.Holding) return;

        _popup = _popupFactory();
        _popup.VariantChosen += OnVariantChosen;
        _popup.Show(_caret.GetAnchorPoint(), _activeVariants);

        _state = State.PopupOpen;
    }

    private void OnVariantChosen(char variant)
    {
        // If the base key is still held (nonzero), swallow its repeats until
        // release so we don't retype the base character afterwards.
        int heldKey = _activeKey;

        DismissPopup();

        if (heldKey != 0)
            _swallowKeyUntilRelease = heldKey;

        // Replace the base character that was already typed with the variant.
        _input.DeletePrecedingCharacter();
        _input.TypeCharacter(variant);
    }

    private void DismissPopup()
    {
        if (_popup is not null)
        {
            _popup.VariantChosen -= OnVariantChosen;
            _popup.Close();
            _popup = null;
        }
        ReturnToIdle();
    }

    private void ReturnToIdle()
    {
        _holdTimer.Stop();
        _state = State.Idle;
        _activeKey = 0;
        _activeChar = '\0';
        _activeVariants = Array.Empty<char>();
    }

    public void Dispose()
    {
        _hook.KeyDown -= OnKeyDown;
        _hook.KeyUp -= OnKeyUp;
        _holdTimer.Stop();
        _popup?.Close();
    }
}
