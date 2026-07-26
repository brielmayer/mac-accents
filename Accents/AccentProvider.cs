namespace MacAccents.Accents;

/// <summary>
/// Accent table matching the macOS "press &amp; hold" popups exactly, transcribed
/// from the actual macOS menus. Lowercase and uppercase are defined separately
/// because macOS uppercase sets are not just the uppercased lowercase sets
/// (e.g. S: ẞ Ś Š Ş Ș, and I contains İ), so deriving them would be wrong.
/// </summary>
public sealed class AccentProvider : IAccentProvider
{
    private static readonly IReadOnlyDictionary<char, string> LowercaseSets =
        new Dictionary<char, string>
        {
            ['a'] = "àáâäǎæãåā",
            ['c'] = "çćčċ",
            ['d'] = "ďð",
            ['e'] = "èéêëěẽēėę",
            ['g'] = "ğġ",
            ['h'] = "ħ",
            ['i'] = "ìíîïǐĩīıį",
            ['k'] = "ķ",
            ['l'] = "łļľ",
            ['n'] = "ñńņň",
            ['o'] = "òóôöǒœøõō",
            ['r'] = "ř",
            ['s'] = "ßşșśš",
            ['t'] = "ţťþ",
            ['u'] = "ùúûüǔũūűů",
            ['w'] = "ŵ",
            ['y'] = "ýŷÿ",
            ['z'] = "źžż",
        };

    private static readonly IReadOnlyDictionary<char, string> UppercaseSets =
        new Dictionary<char, string>
        {
            ['A'] = "ÀÁÂÄǍÆÃÅĀ",
            ['C'] = "ÇĆČĊ",
            ['D'] = "ĎÐ",
            ['E'] = "ÈÉÊËĚẼĒĖĘ",
            ['G'] = "ĞĠ",
            ['H'] = "Ħ",
            ['I'] = "ÌÍÎÏǏĨĪİĮ",
            ['K'] = "Ķ",
            ['L'] = "ŁĻĽ",
            ['N'] = "ÑŃŅŇ",
            ['O'] = "ÒÓÔÖǑŒØÕŌ",
            ['R'] = "Ř",
            ['S'] = "ẞŚŠŞȘ",
            ['T'] = "ŢŤÞ",
            ['U'] = "ÙÚÛÜǓŨŪŰŮ",
            ['W'] = "Ŵ",
            ['Y'] = "ÝŶŸ",
            ['Z'] = "ŹŽŻ",
        };

    private readonly IReadOnlyDictionary<char, IReadOnlyList<char>> _variants;

    public AccentProvider() : this(LowercaseSets, UppercaseSets) { }

    /// <summary>Creates a provider from custom sets (used for testing).</summary>
    public AccentProvider(
        IReadOnlyDictionary<char, string> lowercaseSets,
        IReadOnlyDictionary<char, string> uppercaseSets)
        => _variants = Build(lowercaseSets, uppercaseSets);

    public IReadOnlyList<char>? GetVariants(char baseCharacter)
        => _variants.TryGetValue(baseCharacter, out var v) ? v : null;

    private static Dictionary<char, IReadOnlyList<char>> Build(
        IReadOnlyDictionary<char, string> lowercaseSets,
        IReadOnlyDictionary<char, string> uppercaseSets)
    {
        var result = new Dictionary<char, IReadOnlyList<char>>();
        foreach (var (baseChar, variants) in lowercaseSets)
            result[baseChar] = variants.ToCharArray();
        foreach (var (baseChar, variants) in uppercaseSets)
            result[baseChar] = variants.ToCharArray();
        return result;
    }
}
