using static MacAccents.Interop.NativeMethods;

namespace MacAccents.Input;

/// <summary>
/// Maps virtual keys to letters in a layout-correct way.
///
/// Deliberately uses <see cref="MapVirtualKeyEx"/> rather than
/// <c>ToUnicodeEx</c>: MapVirtualKey does NOT mutate the target application's
/// dead-key state, yet still respects the foreground window's active layout
/// (e.g. QWERTZ vs. QWERTY). Case is derived from Shift/CapsLock.
/// </summary>
public sealed class KeyboardCharacterResolver : IKeyboardCharacterResolver
{
    private const uint DeadKeyFlag = 0x8000_0000;

    public char? Resolve(int virtualKey)
    {
        IntPtr layout = GetActiveLayout();
        uint mapped = MapVirtualKeyEx((uint)virtualKey, MAPVK_VK_TO_CHAR, layout);

        // No character, or a dead key -> ignore.
        if (mapped == 0 || (mapped & DeadKeyFlag) != 0)
            return null;

        char c = (char)(mapped & 0xFFFF);
        if (!char.IsLetter(c))
            return null;

        return IsUppercaseActive() ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c);
    }

    /// <summary>Keyboard layout of the current foreground window (not our own
    /// app — the target application is what matters).</summary>
    private static IntPtr GetActiveLayout()
    {
        IntPtr foreground = GetForegroundWindow();
        uint threadId = foreground == IntPtr.Zero
            ? 0
            : GetWindowThreadProcessId(foreground, out _);
        return GetKeyboardLayout(threadId);
    }

    private static bool IsUppercaseActive()
    {
        bool shift = (GetKeyState(VK_SHIFT) & 0x8000) != 0;
        bool capsLock = (GetKeyState(VK_CAPITAL) & 0x0001) != 0;
        return shift ^ capsLock;
    }
}
