using System.ComponentModel;
using System.Runtime.CompilerServices;
using MacAccents.Services;

namespace MacAccents.View;

/// <summary>
/// Editable view model for the settings dialog. Applies changes to the shared
/// <see cref="AppOptions"/> (which the controller reads live), the autostart
/// registration, and the persistent store in one step.
/// </summary>
public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly AppOptions _options;
    private readonly IAutostartService _autostart;
    private readonly ISettingsStore _store;

    public SettingsViewModel(AppOptions options, IAutostartService autostart, ISettingsStore store)
    {
        _options = options;
        _autostart = autostart;
        _store = store;

        _holdDelayMs = options.HoldDelay.TotalMilliseconds;
        _launchAtStartup = autostart.IsEnabled;
    }

    public double MinHoldDelayMs => AppOptions.MinHoldDelayMs;
    public double MaxHoldDelayMs => AppOptions.MaxHoldDelayMs;

    private double _holdDelayMs;
    public double HoldDelayMs
    {
        get => _holdDelayMs;
        set
        {
            if (SetField(ref _holdDelayMs, Math.Round(value)))
                OnPropertyChanged(nameof(HoldDelayLabel));
        }
    }

    public string HoldDelayLabel => $"{_holdDelayMs:0} ms";

    private bool _launchAtStartup;
    public bool LaunchAtStartup
    {
        get => _launchAtStartup;
        set => SetField(ref _launchAtStartup, value);
    }

    /// <summary>Commits the edited values to options, autostart and disk.</summary>
    public void Apply()
    {
        _options.HoldDelay = TimeSpan.FromMilliseconds(_holdDelayMs);
        _options.LaunchAtStartup = _launchAtStartup;

        _autostart.SetEnabled(_launchAtStartup);
        _store.Save(_options);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
