using System.ComponentModel;

namespace LaunchMe.UI.Widgets.Models;

/// <summary>
/// Core contract for all LaunchMe widget presentation models, enforcing Pause-on-Hide lifecycle.
/// </summary>
public interface IWidgetViewModel : INotifyPropertyChanged, IDisposable
{
    string WidgetId { get; }
    string Title { get; }
    WidgetFamily Family { get; }
    bool IsActive { get; set; }

    /// <summary>
    /// Enforces Pause-on-Hide GPU Law: stops animation timers, polling intervals, and canvas updates.
    /// </summary>
    void OnLauncherHidden();

    /// <summary>
    /// Resumes widget timers and updates when the launcher becomes visible.
    /// </summary>
    void OnLauncherShown();
}
