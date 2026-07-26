namespace MacAccents.Input;

/// <summary>Injects keyboard input into the active application.</summary>
public interface IInputSimulator
{
    /// <summary>Deletes the most recently typed character (Backspace).</summary>
    void DeletePrecedingCharacter();

    /// <summary>Types a Unicode character, independent of the keyboard layout.</summary>
    void TypeCharacter(char character);
}
