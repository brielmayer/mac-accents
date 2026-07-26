using Microsoft.Win32;

namespace MacAccents.Services;

/// <summary>
/// Autostart via the per-user "Run" registry key. Per-user (HKCU) is chosen
/// deliberately: it needs no administrator rights and matches the app's
/// asInvoker manifest.
/// </summary>
public sealed class RegistryAutostartService : IAutostartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MacAccents";

    private readonly string _executablePath;

    public RegistryAutostartService(string executablePath)
        => _executablePath = executablePath;

    public bool IsEnabled
    {
        get
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string path
                && string.Equals(path.Trim('"'), _executablePath, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
            key.SetValue(ValueName, $"\"{_executablePath}\"");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
