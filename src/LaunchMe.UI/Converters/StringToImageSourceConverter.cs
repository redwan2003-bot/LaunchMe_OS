using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace LaunchMe.UI.Converters;

/// <summary>
/// Safely converts a file path string to a <see cref="BitmapImage"/> only when
/// the path is non-empty and exists on disk; returns null otherwise to prevent
/// WinUI 3 XamlBindingHelper.ConvertValue ImageSource ArgumentExceptions.
/// </summary>
public sealed class StringToImageSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string path && !string.IsNullOrWhiteSpace(path))
        {
            try
            {
                if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
                {
                    if (!uri.IsFile || File.Exists(uri.LocalPath))
                    {
                        return new BitmapImage(uri);
                    }
                }
                else if (File.Exists(path))
                {
                    return new BitmapImage(new Uri(Path.GetFullPath(path)));
                }
            }
            catch
            {
            }
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
