using System.Windows;

namespace MacAccents.View;

/// <summary>Settings dialog. Pure view: all logic lives in
/// <see cref="SettingsViewModel"/>.</summary>
public partial class SettingsWindow : Window
{
    // A manual check keeps "Checking…" visible at least this long, so the click
    // clearly registers even when the network answers instantly.
    private static readonly TimeSpan ManualCheckMinimum = TimeSpan.FromSeconds(1);

    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // Check for updates as soon as the dialog opens (no artificial delay).
        Loaded += async (_, _) => await _viewModel.CheckForUpdatesAsync();
    }

    private async void OnCheckForUpdates(object sender, RoutedEventArgs e)
        => await _viewModel.CheckForUpdatesAsync(ManualCheckMinimum);

    private void OnDownloadUpdate(object sender, RoutedEventArgs e)
        => _viewModel.DownloadUpdate();

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _viewModel.Apply();
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
