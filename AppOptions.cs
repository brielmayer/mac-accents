namespace MacAccents;

/// <summary>
/// User-configurable options. Kept as a small, immutable-by-convention value so
/// it can be passed to the controller and surfaced in a settings dialog.
/// </summary>
public sealed class AppOptions
{
    /// <summary>How long a key must be held before the accent popup appears.</summary>
    public TimeSpan HoldDelay { get; set; } = TimeSpan.FromMilliseconds(400);

    /// <summary>Whether the app registers itself to start with Windows.</summary>
    public bool LaunchAtStartup { get; set; }

    public const double MinHoldDelayMs = 150;
    public const double MaxHoldDelayMs = 1000;
}
