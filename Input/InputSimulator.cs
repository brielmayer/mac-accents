using System.Runtime.InteropServices;
using MacAccents.Interop;
using static MacAccents.Interop.NativeMethods;

namespace MacAccents.Input;

/// <summary>SendInput-based implementation of <see cref="IInputSimulator"/>.</summary>
public sealed class InputSimulator : IInputSimulator
{
    private static readonly int InputSize = Marshal.SizeOf<INPUT>();

    public void DeletePrecedingCharacter()
        => Send(KeyEvent(VK_BACK, up: false), KeyEvent(VK_BACK, up: true));

    public void TypeCharacter(char character)
        => Send(UnicodeEvent(character, up: false), UnicodeEvent(character, up: true));

    private static void Send(params INPUT[] inputs)
    {
        // Injection is best-effort: a short count only happens when input is
        // blocked (e.g. UIPI) — there is no meaningful recovery, so we discard.
        _ = SendInput((uint)inputs.Length, inputs, InputSize);
    }

    private static INPUT KeyEvent(ushort virtualKey, bool up) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = virtualKey,
                dwFlags = up ? KEYEVENTF_KEYUP : 0,
                dwExtraInfo = InjectionMarker.Tag,
            }
        }
    };

    private static INPUT UnicodeEvent(char character, bool up) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wScan = character,
                dwFlags = KEYEVENTF_UNICODE | (up ? KEYEVENTF_KEYUP : 0),
                dwExtraInfo = InjectionMarker.Tag,
            }
        }
    };
}
