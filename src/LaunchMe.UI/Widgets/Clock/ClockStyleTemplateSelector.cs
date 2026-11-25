using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LaunchMe.UI.Widgets.Clock;

/// <summary>
/// Selects one of the 12 luxury clock skin DataTemplates based on
/// <see cref="ClockWidgetViewModel.Style"/>.
/// </summary>
public sealed class ClockStyleTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DigitalMinimalTemplate { get; set; }
    public DataTemplate? DigitalBoldTemplate { get; set; }
    public DataTemplate? DigitalSquaredTemplate { get; set; }
    public DataTemplate? AnalogClassicTemplate { get; set; }
    public DataTemplate? AnalogMinimalTemplate { get; set; }
    public DataTemplate? AnalogChronoTemplate { get; set; }
    public DataTemplate? FlipSplitFlapTemplate { get; set; }
    public DataTemplate? SwissRailTemplate { get; set; }
    public DataTemplate? EditorialSerifTemplate { get; set; }
    public DataTemplate? ConcentricRingTemplate { get; set; }
    public DataTemplate? NeonBinaryTemplate { get; set; }
    public DataTemplate? WorldDualTimeTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item) =>
        item is ClockWidgetViewModel vm ? Pick(vm.Style) : base.SelectTemplateCore(item);

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) =>
        item is ClockWidgetViewModel vm ? Pick(vm.Style) : base.SelectTemplateCore(item, container);

    private DataTemplate Pick(ClockStyle style) => style switch
    {
        ClockStyle.DigitalMinimal  => DigitalMinimalTemplate  ?? FallbackTemplate,
        ClockStyle.DigitalBold     => DigitalBoldTemplate     ?? FallbackTemplate,
        ClockStyle.DigitalSquared  => DigitalSquaredTemplate  ?? FallbackTemplate,
        ClockStyle.AnalogClassic   => AnalogClassicTemplate   ?? FallbackTemplate,
        ClockStyle.AnalogMinimal   => AnalogMinimalTemplate   ?? FallbackTemplate,
        ClockStyle.AnalogChrono    => AnalogChronoTemplate    ?? FallbackTemplate,
        ClockStyle.FlipSplitFlap   => FlipSplitFlapTemplate   ?? FallbackTemplate,
        ClockStyle.SwissRail       => SwissRailTemplate       ?? FallbackTemplate,
        ClockStyle.EditorialSerif  => EditorialSerifTemplate  ?? FallbackTemplate,
        ClockStyle.ConcentricRing  => ConcentricRingTemplate  ?? FallbackTemplate,
        ClockStyle.NeonBinary      => NeonBinaryTemplate      ?? FallbackTemplate,
        ClockStyle.WorldDualTime   => WorldDualTimeTemplate   ?? FallbackTemplate,
        _                          => FallbackTemplate,
    };

    // Lazily-constructed inline fallback so null-templates never crash.
    private static DataTemplate? _fallback;
    private static DataTemplate FallbackTemplate =>
        _fallback ??= new DataTemplate();
}
