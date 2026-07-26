using System.IO;
using System.Text.Json;

namespace MacAccents.Services;

/// <summary>
/// JSON-file settings store under %AppData%\MacAccents. Autostart is not stored
/// here — the registry is its single source of truth (see
/// <see cref="IAutostartService"/>).
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    // Serialized shape. Kept separate from AppOptions so the on-disk format is
    // explicit and stable (e.g. store a plain millisecond number, not TimeSpan).
    private sealed record SettingsDto(double HoldDelayMs);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public JsonSettingsStore()
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MacAccents");
        _filePath = Path.Combine(dir, "settings.json");
    }

    public AppOptions Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new AppOptions();

            var dto = JsonSerializer.Deserialize<SettingsDto>(File.ReadAllText(_filePath));
            if (dto is null)
                return new AppOptions();

            double clamped = Math.Clamp(dto.HoldDelayMs, AppOptions.MinHoldDelayMs, AppOptions.MaxHoldDelayMs);
            return new AppOptions { HoldDelay = TimeSpan.FromMilliseconds(clamped) };
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Corrupt or unreadable settings should never prevent startup.
            return new AppOptions();
        }
    }

    public void Save(AppOptions options)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var dto = new SettingsDto(options.HoldDelay.TotalMilliseconds);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(dto, JsonOptions));
    }
}
