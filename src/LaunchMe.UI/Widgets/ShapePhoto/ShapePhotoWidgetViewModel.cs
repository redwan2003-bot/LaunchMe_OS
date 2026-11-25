using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.UI.Theming;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Widgets.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace LaunchMe.UI.Widgets.ShapePhoto;

public enum ShapeMaskType
{
    Circle,
    Squircle,
    Hexagon,
    Diamond,
    Star,
    Heart
}

public sealed class ShapePhotoWidgetViewModel : IWidgetViewModel
{
    private static readonly Color DefaultFillColor = Color.FromArgb(255, 30, 41, 59); // #1E293B
    private static readonly IReadOnlyList<ShapeMaskType> Shapes = Enum.GetValues<ShapeMaskType>();
    private static readonly IReadOnlyList<WidgetPresetSize> Sizes = Enum.GetValues<WidgetPresetSize>();

    private string? _imagePath;
    private ShapeMaskType _selectedShape = ShapeMaskType.Squircle;
    private WidgetPresetSize _presetSize = WidgetPresetSize.Medium;
    private string _fillColorHex = "#1E293B";
    private bool _isActive = true;
    private bool _disposed;

    public string WidgetId { get; } = "widget-shape-photo";
    public string Title => "Shape Photo";
    public WidgetFamily Family => WidgetFamily.ShapePhoto;
    public IReadOnlyList<ShapeMaskType> AvailableShapes => Shapes;
    public IReadOnlyList<WidgetPresetSize> AvailableSizes => Sizes;

    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
    }

    public string? ImagePath
    {
        get => _imagePath;
        set
        {
            if (_imagePath != value)
            {
                _imagePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasImage));
                OnPropertyChanged(nameof(PlaceholderVisibility));
            }
        }
    }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);
    public Visibility PlaceholderVisibility => HasImage ? Visibility.Collapsed : Visibility.Visible;

    public ShapeMaskType SelectedShape
    {
        get => _selectedShape;
        set
        {
            if (_selectedShape != value)
            {
                _selectedShape = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShapeLabel));
            }
        }
    }

    public WidgetPresetSize PresetSize
    {
        get => _presetSize;
        set
        {
            if (_presetSize != value)
            {
                _presetSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WidgetWidth));
                OnPropertyChanged(nameof(WidgetHeight));
                OnPropertyChanged(nameof(ShapeLabel));
            }
        }
    }

    public string FillColorHex
    {
        get => _fillColorHex;
        set
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? "#1E293B" : value.Trim();
            if (!string.Equals(_fillColorHex, normalized, StringComparison.Ordinal))
            {
                _fillColorHex = normalized;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BackgroundBrush));
            }
        }
    }

    public SolidColorBrush? BackgroundBrush =>
        ThemeVisualState.TryCreateBrush(LiquidGlassTintService.ParseHexColor(FillColorHex, DefaultFillColor));

    public double WidgetWidth => PresetSize.GetDimensions().Width;
    public double WidgetHeight => PresetSize.GetDimensions().Height;
    public string ShapeLabel => $"{SelectedShape} ({PresetSize})";

    public ICommand SelectShapeCommand { get; }
    public ICommand NextShapeCommand { get; }
    public ICommand SetPresetSizeCommand { get; }
    public ICommand CycleSizeCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ShapePhotoWidgetViewModel()
    {
        SelectShapeCommand = new RelayCommand<ShapeMaskType>(shape => SelectedShape = shape);
        NextShapeCommand = new RelayCommand(NextShape);
        SetPresetSizeCommand = new RelayCommand<WidgetPresetSize>(size => PresetSize = size);
        CycleSizeCommand = new RelayCommand(CycleSize);
    }

    public void NextShape()
    {
        int idx = ((int)SelectedShape + 1) % Shapes.Count;
        SelectedShape = Shapes[idx];
    }

    public void CycleSize()
    {
        int idx = ((int)PresetSize + 1) % Sizes.Count;
        PresetSize = Sizes[idx];
    }

    public void OnLauncherHidden()
    {
        // Static masked photo; no active timers
    }

    public void OnLauncherShown()
    {
        // No active timers
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
