using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using LaunchMe.Contracts.Spaces;

namespace LaunchMe.UI.Theming;

public sealed partial class LiquidGlassOverlayControl : UserControl
{
    private static readonly LiquidGlassTintService TintService = new();

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

    public LiquidGlassOverlayControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Applies theme settings directly to update the visual state.
    /// </summary>
    public void ApplyTheme(ThemeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        TintService.UpdateVisualState(VisualState, settings);
    }
}
