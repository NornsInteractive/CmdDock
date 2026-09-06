using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;

namespace CmdDock_App.Converters;

/// <summary>
/// Converts a local image file path or URI string to a WinUI 3 ImageSource (BitmapImage or SvgImageSource).
/// </summary>
public class ImagePathConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, string language)
    {
        if (value is string path && !string.IsNullOrWhiteSpace(path))
        {
            try
            {
                if (path.StartsWith("ms-appx://", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    return new BitmapImage(new Uri(path));
                }

                if (path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    return new SvgImageSource(new Uri(path));
                }

                if (File.Exists(path))
                {
                    return new BitmapImage(new Uri(path));
                }

                if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
                {
                    return new BitmapImage(uri);
                }
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
