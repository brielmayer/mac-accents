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
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _viewModel.Apply();
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
