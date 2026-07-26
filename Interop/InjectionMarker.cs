namespace MacAccents.Interop;

/// <summary>
/// Signature stamped into <c>dwExtraInfo</c> of input events we generate
/// ourselves. It lets the keyboard hook recognize and ignore our own
/// injections — otherwise typing a replacement variant would re-trigger the
/// hook (infinite loop).
/// </summary>
internal static class InjectionMarker
{
    /// <summary>Arbitrary but unique value ("MACC" as hex).</summary>
    public static readonly IntPtr Tag = new(0x4D414343);
}
