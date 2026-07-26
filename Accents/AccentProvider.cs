namespace MacAccents.Accents;

/// <summary>
/// Default accent table, modelled after macOS "press &amp; hold". Maps a base
/// letter to its variants (excluding the base letter itself). Uppercase entries
/// are derived automatically from the lowercase definitions.
/// </summary>
public sealed class AccentProvider : IAccentProvider
{
    // Lowercase definitions. Everything else is derived from these.
    private static readonly IReadOnlyDictionary<char, string> Definitions =
        new Dictionary<char, string>
        {
            ['a'] = "àáâäæãåā",
            ['e'] = "èéêëēėę",
            ['i'] = "îïíīįì",
            ['o'] = "ôöòóœøōõ",
            ['u'] = "ûüùúū",
            ['n'] = "ñń",
            ['c'] = "çćč",
            ['s'] = "ßśš",
            ['y'] = "ÿý",
            ['z'] = "žźż",
            ['l'] = "ł",
            ['g'] = "ğ",
            ['r'] = "ř",
            ['t'] = "ťþ",
            ['d'] = "ðď",
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
            // distinct uppercase form (e.g. 'ß').
            var upperVariants = variants
                .Select(char.ToUpperInvariant)
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray();

            char upperBase = char.ToUpperInvariant(baseChar);
            if (upperBase != baseChar && upperVariants.Length > 0)
                result[upperBase] = upperVariants;
        }

        return result;
    }
}
