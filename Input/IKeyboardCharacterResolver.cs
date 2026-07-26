namespace MacAccents.Input;

/// <summary>Resolves the character a virtual key would produce under the
/// current keyboard layout.</summary>
public interface IKeyboardCharacterResolver
{
    /// <summary>Returns the produced character, or <c>null</c> if the key is not
    /// a letter (or cannot be mapped).</summary>
    char? Resolve(int virtualKey);
}
