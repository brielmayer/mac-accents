using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using MacAccents.Interop;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

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

    public void Show(CaretAnchor anchor, IReadOnlyList<char> variants)
    {
        PopulateOptions(variants);

        // Show first (without activating) so the layout has real dimensions.
        Show();
        UpdateLayout();

        Place(anchor);
    }

    private void PopulateOptions(IReadOnlyList<char> variants)
    {
        _options.Clear();
        for (int i = 0; i < variants.Count; i++)
            _options.Add(new AccentOption(variants[i], i + 1));

        _highlightIndex = 0;
        ApplyHighlight();
    }

    /// <summary>Converts the anchor into this window's units and hands the geometry
    /// to <see cref="PopupPlacement"/>.
    ///
    /// Sizes come from <c>PopupSurface</c> rather than the window. Windows enforces
    /// a minimum width on every top-level window, so the window can be wider than
    /// what it shows, and clamping against that phantom width would push the popup
    /// off the caret near a screen edge.
    ///
    /// Known limitation: the conversion uses the DPI of the monitor this window
    /// currently sits on, which is where it was created, not necessarily the
    /// monitor the caret is on. On a mixed-DPI setup the popup can therefore land
    /// slightly off. Resolving that needs the anchor monitor's own DPI via
    /// GetDpiForMonitor.</summary>
    private void Place(CaretAnchor anchor)
    {
        Point position = PopupPlacement.Compute(
            caret: DeviceToLogical(anchor.BottomLeft),
            lineHeight: ScaleToLogical(anchor.LineHeight),
            anchoredToMouse: anchor.Source == AnchorSource.MousePointer,
            popup: new Size(PopupSurface.ActualWidth, PopupSurface.ActualHeight),
            workArea: DeviceToLogical(WorkAreaAround(anchor.BottomLeft)));

        Left = position.X;
        Top = position.Y;
    }

    /// <summary>Work area (physical pixels) of the monitor containing the anchor.
    /// Floors rather than truncates, so a monitor left of or above the primary one,
    /// where coordinates are negative, probes the right side of the boundary.</summary>
    private static Rect WorkAreaAround(Point screenPixels)
    {
        var probe = new System.Drawing.Point(
            (int)Math.Floor(screenPixels.X), (int)Math.Floor(screenPixels.Y));
        System.Drawing.Rectangle work = System.Windows.Forms.Screen.FromPoint(probe).WorkingArea;
        return new Rect(work.X, work.Y, work.Width, work.Height);
    }

    /// <summary>Converts physical screen pixels to WPF device-independent units.</summary>
    private Point DeviceToLogical(Point pixels) => TransformFromDevice().Transform(pixels);

    private Rect DeviceToLogical(Rect pixels)
    {
        Matrix transform = TransformFromDevice();
        return new Rect(transform.Transform(pixels.TopLeft), transform.Transform(pixels.BottomRight));
    }

    private double ScaleToLogical(double pixels) => pixels * TransformFromDevice().M22;

    private Matrix TransformFromDevice()
    {
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
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
