using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MacAccents.View;

/// <summary>
/// View model for a single accent variant in the popup. Raises change
/// notifications so the highlight is data-bound rather than mutated imperatively.
/// </summary>
public sealed class AccentOption : INotifyPropertyChanged
{
    public AccentOption(char character, int number)
    {
        Character = character;
        Number = number;
    }

    /// <summary>The accent character to insert.</summary>
    public char Character { get; }

    /// <summary>Display text for the character.</summary>
    public string Display => Character.ToString();

    /// <summary>1-based shortcut number shown under the character.</summary>
    public int Number { get; }

    private bool _isHighlighted;
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set
        {
            if (_isHighlighted == value) return;
            _isHighlighted = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
