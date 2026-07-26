namespace MacAccents.Accents;

/// <summary>Supplies the accent variants offered for a given base character.</summary>
public interface IAccentProvider
{
    /// <summary>Returns the accent variants for <paramref name="baseCharacter"/>,
    /// or <c>null</c> if none are defined. The base character itself is not
    /// included — it has already been typed.</summary>
    IReadOnlyList<char>? GetVariants(char baseCharacter);
}
