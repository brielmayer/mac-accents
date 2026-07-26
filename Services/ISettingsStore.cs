namespace MacAccents.Services;

/// <summary>Persists and restores user options across sessions.</summary>
public interface ISettingsStore
{
    /// <summary>Loads saved options, or defaults when nothing is stored.</summary>
    AppOptions Load();

    /// <summary>Persists the given options.</summary>
    void Save(AppOptions options);
}
