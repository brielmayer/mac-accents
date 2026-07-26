using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;
using MacAccents.Accents;
using MacAccents.Input;
using MacAccents.Interop;
using MacAccents.Services;
using MacAccents.View;

namespace MacAccents;

/// <summary>
/// Composition root: constructs and wires the object graph by hand (a small app
/// does not need a DI container) and owns the top-level lifetime.
/// </summary>
public partial class App : System.Windows.Application
{
    // Also referenced by the installer (Inno Setup 'AppMutex') to detect a
    // running instance during install/upgrade. Keep the two names in sync.
    private const string SingleInstanceMutexName = "MacAccents_SingleInstance";

    private Mutex? _singleInstanceMutex;
    private AppOptions _options = new();
    private ISettingsStore _settingsStore = null!;
    private IAutostartService _autostart = null!;
    private KeyboardHook _hook = null!;
    private AccentController _controller = null!;
    private TrayIcon _tray = null!;
    private IUpdateChecker _updateChecker = null!;
    private SettingsWindow? _settingsWindow;
    private string? _pendingUpdateUrl;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        // Enforce a single running instance — two hooks would double every keypress.
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            Shutdown();
            return;
        }

        _settingsStore = new JsonSettingsStore();
        _options = _settingsStore.Load();

        _autostart = new RegistryAutostartService(Environment.ProcessPath ?? string.Empty);
        _options.LaunchAtStartup = _autostart.IsEnabled;

        _hook = new KeyboardHook();
        _controller = new AccentController(
            _hook,
            new KeyboardCharacterResolver(),
            new AccentProvider(),
            new InputSimulator(),
            new CaretLocator(),
            popupFactory: () => new AccentPopup(),
            _options,
            Dispatcher);

        if (!TryInstallHook())
            return;

        _tray = new TrayIcon();
        _tray.SettingsRequested += ShowSettings;
        _tray.ExitRequested += Shutdown;
        _tray.UpdateClicked += OpenDownloadPage;
        _tray.ShowStartupHint();

        _updateChecker = new GitHubUpdateChecker();
        _ = CheckForUpdatesAsync();
    }

    private static Version CurrentVersion
        => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    private async Task CheckForUpdatesAsync()
    {
        // Runs on startup; the async continuation resumes on the UI thread.
        UpdateCheckResult result = await _updateChecker.CheckAsync(CurrentVersion);
        if (result.Status != UpdateCheckStatus.UpdateAvailable)
            return;

        _pendingUpdateUrl = result.ReleaseUrl;
        _tray.ShowUpdateAvailable(result.Version!);
    }

    private void OpenDownloadPage()
    {
        if (!string.IsNullOrEmpty(_pendingUpdateUrl))
            Process.Start(new ProcessStartInfo(_pendingUpdateUrl) { UseShellExecute = true });
    }

    private bool TryInstallHook()
    {
        try
        {
            _hook.Install();
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "MacAccents",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return false;
        }
    }

    private void ShowSettings()
    {
        // Reuse a single dialog instance instead of stacking windows.
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(
            new SettingsViewModel(_options, _autostart, _settingsStore, _updateChecker, CurrentVersion));
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _controller?.Dispose();
        _hook?.Dispose();
        _tray?.Dispose();
        _singleInstanceMutex?.Dispose();
    }
}
