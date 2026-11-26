using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LaunchMe.UI.Widgets.Music;

/// <summary>Selects one of the 6 Music widget visual skins.</summary>
public sealed class MusicStyleTemplateSelector : DataTemplateSelector
{
    public DataTemplate? MinimalTemplate { get; set; }
    public DataTemplate? FullCoverTemplate { get; set; }
    public DataTemplate? VinylTemplate { get; set; }
    public DataTemplate? DynamicAccentTemplate { get; set; }
    public DataTemplate? DynamicSplitTemplate { get; set; }
    public DataTemplate? CompactIdleTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item) =>
        item is MusicWidgetViewModel vm ? Pick(vm.Style) : base.SelectTemplateCore(item);

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) =>
        item is MusicWidgetViewModel vm ? Pick(vm.Style) : base.SelectTemplateCore(item, container);

    private DataTemplate Pick(MusicWidgetStyle style) => style switch
    {
        MusicWidgetStyle.Minimal       => MinimalTemplate       ?? Fallback,
        MusicWidgetStyle.FullCover     => FullCoverTemplate     ?? Fallback,
        MusicWidgetStyle.Vinyl         => VinylTemplate         ?? Fallback,
        MusicWidgetStyle.DynamicAccent => DynamicAccentTemplate ?? Fallback,
        MusicWidgetStyle.DynamicSplit  => DynamicSplitTemplate  ?? Fallback,
        MusicWidgetStyle.CompactIdle   => CompactIdleTemplate   ?? Fallback,
        _                              => MinimalTemplate       ?? Fallback
    };

    private static DataTemplate? _fallback;
    private static DataTemplate Fallback => _fallback ??= new DataTemplate();
}
