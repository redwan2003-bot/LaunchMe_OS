using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using LaunchMe.Contracts.Spaces;

namespace LaunchMe.UI.Theming;

public sealed partial class LiquidGlassOverlayControl : UserControl
{
    private static readonly LiquidGlassTintService TintService = new();
    private readonly LiquidGlassCompositor _compositor = new();

    public static readonly DependencyProperty VisualStateProperty =
        DependencyProperty.Register(
            nameof(VisualState),
            typeof(ThemeVisualState),
            typeof(LiquidGlassOverlayControl),
            new PropertyMetadata(new ThemeVisualState()));

    public ThemeVisualState VisualState
    {
        get => (ThemeVisualState)GetValue(VisualStateProperty);
        set => SetValue(VisualStateProperty, value);
    }

    public bool IsCompositorActive => _compositor.IsActive;

    public LiquidGlassOverlayControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Attach();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Enforce Pause-on-Hide GPU Law when control leaves the visual tree
        Detach();
    }

    /// <summary>
    /// Attaches the Win2D Composition liquid glass backdrop effect when visible.
    /// </summary>
    public void Attach()
    {
        _compositor.TryAttachToElement(CompositorSurface, (float)ActualWidth, (float)ActualHeight);
    }

    /// <summary>
    /// Releases the Win2D Composition effect brush and visual resources when hidden or unloaded.
    /// </summary>
    public void Detach()
    {
        _compositor.Detach(CompositorSurface);
    }

    /// <summary>
    /// Applies theme settings directly to update the visual state.
    /// </summary>
    public void ApplyTheme(ThemeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        TintService.UpdateVisualState(VisualState, settings);
    }

    /// <summary>
    /// Updates the Win2D compositor refraction tint from a sampled wallpaper/accent color.
    /// </summary>
    public Windows.UI.Color ApplyWallpaperRefraction(Windows.UI.Color sampledColor)
    {
        return _compositor.ComputeWallpaperRefractionTint(sampledColor);
    }
}
