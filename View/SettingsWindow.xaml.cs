using System.Windows;

namespace MacAccents.View;

/// <summary>Settings dialog. Pure view: all logic lives in
/// <see cref="SettingsViewModel"/>.</summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // Check for updates as soon as the dialog opens.
        Loaded += async (_, _) => await _viewModel.CheckForUpdatesAsync();
    }

    private async void OnCheckForUpdates(object sender, RoutedEventArgs e)
        => await _viewModel.CheckForUpdatesAsync();

    private void OnDownloadUpdate(object sender, RoutedEventArgs e)
        => _viewModel.DownloadUpdate();

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _viewModel.Apply();
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
