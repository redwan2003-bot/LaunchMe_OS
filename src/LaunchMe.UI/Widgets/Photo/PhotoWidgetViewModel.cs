using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.UI.Theming;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Widgets.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace LaunchMe.UI.Widgets.Photo;

public sealed class PhotoWidgetViewModel : IWidgetViewModel
{
    private static readonly Color DefaultFillColor = Color.FromArgb(255, 30, 41, 59); // #1E293B
    private static readonly Color TransparentColor = Color.FromArgb(0, 0, 0, 0);
    private static readonly IReadOnlyList<WidgetPresetSize> Sizes = Enum.GetValues<WidgetPresetSize>();

    private string? _imagePath;
    private bool _isTransparentBackground;
    private string _fillColorHex = "#1E293B";
    private WidgetPresetSize _presetSize = WidgetPresetSize.Medium;
    private bool _isActive = true;
    private bool _disposed;

    public string WidgetId { get; } = "widget-photo";
    public string Title => "Photo";
    public WidgetFamily Family => WidgetFamily.Photo;
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
                OnPropertyChanged(nameof(ImageVisibility));
                OnPropertyChanged(nameof(PlaceholderVisibility));
            }
        }
    }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);
    public Visibility ImageVisibility => HasImage ? Visibility.Visible : Visibility.Collapsed;
    public Visibility PlaceholderVisibility => HasImage ? Visibility.Collapsed : Visibility.Visible;

    public bool IsTransparentBackground
    {
        get => _isTransparentBackground;
        set
        {
            if (_isTransparentBackground != value)
            {
                _isTransparentBackground = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResolvedBackgroundColor));
                OnPropertyChanged(nameof(BackgroundBrush));
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
                OnPropertyChanged(nameof(ResolvedBackgroundColor));
                OnPropertyChanged(nameof(BackgroundBrush));
            }
        }
    }

    public Color ResolvedBackgroundColor =>
        IsTransparentBackground
            ? TransparentColor
            : LiquidGlassTintService.ParseHexColor(FillColorHex, DefaultFillColor);

    public SolidColorBrush? BackgroundBrush => ThemeVisualState.TryCreateBrush(ResolvedBackgroundColor);

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
                OnPropertyChanged(nameof(SizeLabel));
            }
        }
    }

    public double WidgetWidth => PresetSize.GetDimensions().Width;
    public double WidgetHeight => PresetSize.GetDimensions().Height;
    public string SizeLabel => $"{PresetSize} ({(int)WidgetWidth}x{(int)WidgetHeight})";

    public ICommand ToggleTransparentBackgroundCommand { get; }
    public ICommand SetPresetSizeCommand { get; }
    public ICommand CycleSizeCommand { get; }
    public ICommand ClearImageCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PhotoWidgetViewModel()
    {
        ToggleTransparentBackgroundCommand = new RelayCommand(() => IsTransparentBackground = !IsTransparentBackground);
        SetPresetSizeCommand = new RelayCommand<WidgetPresetSize>(size => PresetSize = size);
        CycleSizeCommand = new RelayCommand(CycleSize);
        ClearImageCommand = new RelayCommand(() => ImagePath = null);
    }

    public void CycleSize()
    {
        int next = ((int)PresetSize + 1) % Sizes.Count;
        PresetSize = Sizes[next];
    }

    public void OnLauncherHidden()
    {
        // Photo widget holds static decoded image; no active timers to pause
    }

    public void OnLauncherShown()
    {
        // No timers to resume
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
