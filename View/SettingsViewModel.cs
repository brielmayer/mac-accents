using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
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
    private readonly IUpdateChecker _updateChecker;
    private readonly Version _currentVersion;
    private string? _updateUrl;

    public SettingsViewModel(
        AppOptions options,
        IAutostartService autostart,
        ISettingsStore store,
        IUpdateChecker updateChecker,
        Version currentVersion)
    {
        _options = options;
        _autostart = autostart;
        _store = store;
        _updateChecker = updateChecker;
        _currentVersion = currentVersion;

        _holdDelayMs = options.HoldDelay.TotalMilliseconds;
        _launchAtStartup = autostart.IsEnabled;
        VersionText = $"Version {currentVersion.ToString(3)}";
        CopyrightText = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;
    }

    public string CopyrightText { get; }

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

    // --- Updates ---

    public string VersionText { get; }

    private string _updateStatus = "Checking for updates…";
    public string UpdateStatus
    {
        get => _updateStatus;
        private set => SetField(ref _updateStatus, value);
    }

    private bool _isChecking;
    public bool IsChecking
    {
        get => _isChecking;
        private set
        {
            if (SetField(ref _isChecking, value))
                OnPropertyChanged(nameof(CanCheck));
        }
    }

    public bool CanCheck => !_isChecking;

    private bool _updateAvailable;
    public bool UpdateAvailable
    {
        get => _updateAvailable;
        private set => SetField(ref _updateAvailable, value);
    }

    /// <summary>Queries GitHub for a newer release and updates the status.
    /// <paramref name="minimumDuration"/> keeps the "checking" state visible for
    /// at least that long, so a manual check feels responsive even when the
    /// network answers instantly.</summary>
    public async Task CheckForUpdatesAsync(TimeSpan minimumDuration = default)
    {
        IsChecking = true;
        UpdateAvailable = false;
        UpdateStatus = "Checking for updates…";

        Task<UpdateCheckResult> check = _updateChecker.CheckAsync(_currentVersion);
        await Task.WhenAll(check, Task.Delay(minimumDuration));
        UpdateCheckResult result = await check;

        IsChecking = false;

        switch (result.Status)
        {
            case UpdateCheckStatus.UpdateAvailable:
                _updateUrl = result.ReleaseUrl;
                UpdateAvailable = true;
                UpdateStatus = $"Version {result.Version} is available.";
                break;
            case UpdateCheckStatus.UpToDate:
                UpdateStatus = "You’re on the latest version.";
                break;
            default:
                UpdateStatus = "Couldn’t check for updates.";
                break;
        }
    }

    /// <summary>Opens the release page for the available update.</summary>
    public void DownloadUpdate()
    {
        if (!string.IsNullOrEmpty(_updateUrl))
            Process.Start(new ProcessStartInfo(_updateUrl) { UseShellExecute = true });
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
