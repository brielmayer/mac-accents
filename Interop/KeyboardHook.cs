using System.Runtime.InteropServices;
using static MacAccents.Interop.NativeMethods;

namespace MacAccents.Interop;

/// <summary>Arguments for an intercepted key event.</summary>
public sealed class KeyboardHookEventArgs(int virtualKey) : EventArgs
{
    /// <summary>Virtual-key code (VK_*).</summary>
    public int VirtualKey { get; } = virtualKey;

    /// <summary>Set to true to swallow the key (do not pass it on to the target
    /// application).</summary>
    public bool Suppress { get; set; }
}

/// <summary>
/// Wraps a global low-level keyboard hook and surfaces key events as .NET
/// events. Holds no domain logic — pure mechanism.
///
/// Threading: the callback runs on the thread that installed the hook (that
/// thread must pump a message loop — the UI thread in WPF). Event handlers
/// therefore also run on the UI thread.
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    // Kept in a field so the delegate is not collected by the GC.
    private readonly LowLevelKeyboardProc _callback;
    private IntPtr _handle;

    public event EventHandler<KeyboardHookEventArgs>? KeyDown;
    public event EventHandler<KeyboardHookEventArgs>? KeyUp;

    public KeyboardHook() => _callback = HookCallback;

    public void Install()
    {
        if (_handle != IntPtr.Zero) return;

        _handle = SetWindowsHookEx(WH_KEYBOARD_LL, _callback, GetModuleHandle(null), 0);
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException(
                "Failed to install the global keyboard hook.",
                new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()));
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
            return CallNextHookEx(_handle, nCode, wParam, lParam);

        var info = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

        // Ignore our own synthesized input -> avoids recursion.
        if (info.dwExtraInfo == InjectionMarker.Tag)
            return CallNextHookEx(_handle, nCode, wParam, lParam);

        int message = (int)wParam;
        var handler = message is WM_KEYDOWN or WM_SYSKEYDOWN ? KeyDown
                    : message is WM_KEYUP or WM_SYSKEYUP ? KeyUp
                    : null;

        if (handler is not null)
        {
            var args = new KeyboardHookEventArgs((int)info.vkCode);
            handler(this, args);
            if (args.Suppress)
                return 1; // swallow the key
        }

        return CallNextHookEx(_handle, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_handle == IntPtr.Zero) return;
        UnhookWindowsHookEx(_handle);
        _handle = IntPtr.Zero;
    }
}
