using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Point = System.Windows.Point;

namespace MacAccents.View;

/// <summary>
/// Focus-less overlay showing the accent variants. It must never steal focus,
/// so that Backspace and the replacement character reach the target
/// application. Selection is driven by the controller (keyboard) and by direct
/// mouse interaction here.
/// </summary>
public partial class AccentPopup : Window, IAccentPopup
{
    private const int GwlExStyle = -20;
    private const int WsExNoActivate = 0x0800_0000;
    private const int WsExToolWindow = 0x0000_0080;

    private const double VerticalGap = 4;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int newLong);

    private readonly ObservableCollection<AccentOption> _options = new();
    private int _highlightIndex;

    public event Action<char>? VariantChosen;

    public AccentPopup()
    {
        InitializeComponent();
        OptionsHost.ItemsSource = _options;
        SourceInitialized += (_, _) => MarkAsNonActivating();
    }

    /// <summary>Marks the window as a non-activating tool window so it cannot
    /// take focus away from the target application.</summary>
    private void MarkAsNonActivating()
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        int style = GetWindowLong(handle, GwlExStyle);
        SetWindowLong(handle, GwlExStyle, style | WsExNoActivate | WsExToolWindow);
    }

    public void Show(Point screenAnchor, IReadOnlyList<char> variants)
    {
        PopulateOptions(variants);

        // Show first (without activating) so the layout has real dimensions.
        Show();
        UpdateLayout();

        PositionAbove(screenAnchor);
    }

    private void PopulateOptions(IReadOnlyList<char> variants)
    {
        _options.Clear();
        for (int i = 0; i < variants.Count; i++)
            _options.Add(new AccentOption(variants[i], i + 1));

        _highlightIndex = 0;
        ApplyHighlight();
    }

    /// <summary>Places the popup just above the anchor, flipping below if it
    /// would leave the screen.</summary>
    private void PositionAbove(Point screenAnchorPixels)
    {
        Point anchor = DeviceToLogical(screenAnchorPixels);

        Left = anchor.X;
        double above = anchor.Y - ActualHeight - VerticalGap;
        Top = above >= 0 ? above : anchor.Y + VerticalGap * 6;
    }

    /// <summary>Converts physical screen pixels to WPF device-independent units.</summary>
    private Point DeviceToLogical(Point pixels)
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null)
            return pixels;

        return source.CompositionTarget.TransformFromDevice.Transform(pixels);
    }

    // --- IAccentPopup control surface (called by the controller) ---

    public void MoveHighlight(int delta)
    {
        if (_options.Count == 0) return;
        int count = _options.Count;
        _highlightIndex = ((_highlightIndex + delta) % count + count) % count;
        ApplyHighlight();
    }

    public void ChooseHighlighted()
    {
        if (_highlightIndex >= 0 && _highlightIndex < _options.Count)
            VariantChosen?.Invoke(_options[_highlightIndex].Character);
    }

    public bool ChooseByNumber(int oneBasedIndex)
    {
        int index = oneBasedIndex - 1;
        if (index < 0 || index >= _options.Count) return false;

        VariantChosen?.Invoke(_options[index].Character);
        return true;
    }

    private void ApplyHighlight()
    {
        for (int i = 0; i < _options.Count; i++)
            _options[i].IsHighlighted = i == _highlightIndex;
    }

    // --- Direct mouse interaction ---

    private void OnCellClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AccentOption option })
            VariantChosen?.Invoke(option.Character);
    }

    private void OnCellHovered(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AccentOption option })
        {
            _highlightIndex = _options.IndexOf(option);
            ApplyHighlight();
        }
    }
}
