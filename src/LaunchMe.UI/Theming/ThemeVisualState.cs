using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;
using Microsoft.UI.Xaml.Media;

namespace LaunchMe.UI.Theming;

/// <summary>
/// Resolved visual state derived from ThemeSettings for Liquid Glass rendering.
/// </summary>
public sealed class ThemeVisualState : INotifyPropertyChanged
{
    private Color _tintColor = Color.FromArgb(255, 30, 64, 175);
    private Color _backdropColor = Color.FromArgb(165, 30, 64, 175);
    private Color _effectiveTitleColor = Color.FromArgb(255, 255, 255, 255);
    private string _effectiveTitleHex = "#FFFFFF";
    private double _tintOpacity = 0.65;
    private bool _isLiquidGlass = true;
    private bool _isTinted = true;
    private double _blurIntensity = 30.0;
    private SolidColorBrush? _tintBrush;
    private SolidColorBrush? _titleBrush;
    private SolidColorBrush? _backdropBrush;

    public Color TintColor
    {
        get => _tintColor;
        set
        {
            if (_tintColor != value)
            {
                _tintColor = value;
                _tintBrush = TryCreateBrush(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(TintBrush));
            }
        }
    }

    public Color BackdropColor
    {
        get => _backdropColor;
        set
        {
            if (_backdropColor != value)
            {
                _backdropColor = value;
                _backdropBrush = TryCreateBrush(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(BackdropBrush));
            }
        }
    }

    public double TintOpacity
    {
        get => _tintOpacity;
        set { if (Math.Abs(_tintOpacity - value) > 0.001) { _tintOpacity = value; OnPropertyChanged(); } }
    }

    public Color EffectiveTitleColor
    {
        get => _effectiveTitleColor;
        set
        {
            if (_effectiveTitleColor != value)
            {
                _effectiveTitleColor = value;
                _titleBrush = TryCreateBrush(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(TitleBrush));
            }
        }
    }

    public string EffectiveTitleHex
    {
        get => _effectiveTitleHex;
        set { if (_effectiveTitleHex != value) { _effectiveTitleHex = value; OnPropertyChanged(); } }
    }

    public bool IsLiquidGlass
    {
        get => _isLiquidGlass;
        set { if (_isLiquidGlass != value) { _isLiquidGlass = value; OnPropertyChanged(); } }
    }

    public bool IsTinted
    {
        get => _isTinted;
        set { if (_isTinted != value) { _isTinted = value; OnPropertyChanged(); } }
    }

    public double BlurIntensity
    {
        get => _blurIntensity;
        set { if (Math.Abs(_blurIntensity - value) > 0.001) { _blurIntensity = value; OnPropertyChanged(); } }
    }

    public SolidColorBrush? TintBrush
    {
        get => _tintBrush ??= TryCreateBrush(TintColor);
        set { if (_tintBrush != value) { _tintBrush = value; OnPropertyChanged(); } }
    }

    public SolidColorBrush? TitleBrush
    {
        get => _titleBrush ??= TryCreateBrush(EffectiveTitleColor);
        set { if (_titleBrush != value) { _titleBrush = value; OnPropertyChanged(); } }
    }

    public SolidColorBrush? BackdropBrush
    {
        get => _backdropBrush ??= TryCreateBrush(BackdropColor);
        set { if (_backdropBrush != value) { _backdropBrush = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static SolidColorBrush? TryCreateBrush(Color color)
    {
        try
        {
            return new SolidColorBrush(color);
        }
        catch
        {
            // Null in headless/non-WinUI process environments
            return null;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
