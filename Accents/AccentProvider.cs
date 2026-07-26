namespace MacAccents.Accents;

/// <summary>
/// Default accent table, modelled after macOS "press &amp; hold". Maps a base
/// letter to its variants (excluding the base letter itself). Uppercase entries
/// are derived automatically from the lowercase definitions.
/// </summary>
public sealed class AccentProvider : IAccentProvider
{
    private static readonly IReadOnlyDictionary<char, string> Definitions =
        new Dictionary<char, string>
        {
            ['a'] = "àáâäǎæãåā",
            ['e'] = "èéêëěẽēėę",
            ['i'] = "ìíîïǐĩīıį",
            ['o'] = "òóôöǒœøõō",
            ['u'] = "ùúûüǔũūűů",
            ['c'] = "çćčċ",
            ['d'] = "ďð",
            ['g'] = "ğġ",
            ['h'] = "ħ",
            ['k'] = "ķ",
            ['l'] = "łļľ",
            ['n'] = "ñńņň",
            ['r'] = "ř",
            ['s'] = "ßşșśš",
            ['t'] = "ţťþ",
            ['w'] = "ŵ",
            ['y'] = "ýŷÿ",
            ['z'] = "źžż",
        };

    private readonly IReadOnlyDictionary<char, IReadOnlyList<char>> _variants;

    public AccentProvider() : this(Definitions) { }

    /// <summary>Creates a provider from a custom lowercase definition table
    /// (used for configuration/testing).</summary>
    public AccentProvider(IReadOnlyDictionary<char, string> definitions)
        => _variants = Build(definitions);

    public IReadOnlyList<char>? GetVariants(char baseCharacter)
        => _variants.TryGetValue(baseCharacter, out var v) ? v : null;

    private static Dictionary<char, IReadOnlyList<char>> Build(
        IReadOnlyDictionary<char, string> definitions)
    {
        var result = new Dictionary<char, IReadOnlyList<char>>();

        foreach (var (baseChar, variants) in definitions)
        {
            result[baseChar] = variants.ToCharArray();

            // Derive the uppercase counterpart. Skip variants that have no
            // distinct uppercase form (e.g. 'ß'), matching macOS (S -> Ś Š).
            var upperVariants = variants
                .Select(char.ToUpperInvariant)
                .Where(char.IsUpper)
                .ToArray();

            char upperBase = char.ToUpperInvariant(baseChar);
            if (upperBase != baseChar && upperVariants.Length > 0)
                result[upperBase] = upperVariants;
        }

        return result;
    }
}
