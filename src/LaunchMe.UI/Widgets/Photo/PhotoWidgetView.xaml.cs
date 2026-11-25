using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace LaunchMe.UI.Widgets.Photo;

public sealed partial class PhotoWidgetView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(PhotoWidgetViewModel),
            typeof(PhotoWidgetView),
            new PropertyMetadata(null, OnViewModelChanged));

    public PhotoWidgetViewModel? ViewModel
    {
        get => (PhotoWidgetViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public PhotoWidgetView()
    {
        InitializeComponent();
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PhotoWidgetView view) return;

        if (e.OldValue is PhotoWidgetViewModel oldVm)
        {
            oldVm.PropertyChanged -= view.OnViewModelPropertyChanged;
        }

        if (e.NewValue is PhotoWidgetViewModel newVm)
        {
            newVm.PropertyChanged += view.OnViewModelPropertyChanged;
            view.UpdatePhotoSource();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PhotoWidgetViewModel.ImagePath))
        {
            DispatcherQueue?.TryEnqueue(UpdatePhotoSource);
        }
    }

    private void UpdatePhotoSource()
    {
        string? path = ViewModel?.ImagePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            PhotoImage.Source = null;
            return;
        }

        try
        {
            PhotoImage.Source = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
        }
        catch
        {
            PhotoImage.Source = null;
        }
    }
}
