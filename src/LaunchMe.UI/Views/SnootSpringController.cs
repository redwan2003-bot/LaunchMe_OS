using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace LaunchMe.UI.Views;

/// <summary>
/// Drives spring-physics expansion and collapse transitions for the Snoot HUD
/// using WinUI Composition <see cref="SpringVector3NaturalMotionAnimation"/>.
/// Guarded safely for headless unit test environments.
/// </summary>
public sealed class SnootSpringController
{
    public const float ExpandDampingRatio = 0.82f;
    public const int ExpandPeriodMs = 220;

    public const float CollapseDampingRatio = 0.88f;
    public const int CollapsePeriodMs = 180;

    private UIElement? _targetElement;

    public bool IsAttached => _targetElement != null;
    public bool IsLastExpanded { get; private set; }

    /// <summary>
    /// Attaches the controller to the Snoot HUD container element.
    /// </summary>
    public void Attach(UIElement? targetElement)
    {
        _targetElement = targetElement;
    }

    /// <summary>
    /// Plays the spring expand transition (DampingRatio 0.82f, Period 220ms).
    /// </summary>
    public bool AnimateExpand(UIElement? overrideTarget = null)
    {
        IsLastExpanded = true;
        return StartScaleSpring(
            overrideTarget ?? _targetElement,
            initialScale: new Vector3(0.94f, 0.90f, 1.0f),
            finalScale: Vector3.One,
            dampingRatio: ExpandDampingRatio,
            periodMs: ExpandPeriodMs);
    }

    /// <summary>
    /// Plays the spring collapse transition (DampingRatio 0.88f, Period 180ms).
    /// </summary>
    public bool AnimateCollapse(UIElement? overrideTarget = null)
    {
        IsLastExpanded = false;
        return StartScaleSpring(
            overrideTarget ?? _targetElement,
            initialScale: new Vector3(1.03f, 1.04f, 1.0f),
            finalScale: Vector3.One,
            dampingRatio: CollapseDampingRatio,
            periodMs: CollapsePeriodMs);
    }

    /// <summary>
    /// Executes a <see cref="SpringVector3NaturalMotionAnimation"/> on the element's Composition Visual Scale.
    /// Returns false safely when running headless without a WinUI Visual/Compositor.
    /// </summary>
    public static bool StartScaleSpring(
        UIElement? target,
        Vector3 initialScale,
        Vector3 finalScale,
        float dampingRatio,
        int periodMs)
    {
        if (target == null)
        {
            return false;
        }

        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(target);
            if (visual == null)
            {
                return false;
            }

            var compositor = visual.Compositor;
            if (compositor == null)
            {
                return false;
            }

            if (target is FrameworkElement fe)
            {
                visual.CenterPoint = new Vector3((float)(fe.ActualWidth * 0.5), 0f, 0f);
            }

            SpringVector3NaturalMotionAnimation spring = compositor.CreateSpringVector3Animation();
            spring.InitialValue = initialScale;
            spring.FinalValue = finalScale;
            spring.DampingRatio = Math.Clamp(dampingRatio, 0.05f, 1.0f);
            spring.Period = TimeSpan.FromMilliseconds(Math.Max(16, periodMs));

            visual.StartAnimation("Scale", spring);
            return true;
        }
        catch
        {
            // Headless / test environment without WinUI Compositor
            return false;
        }
    }
}
