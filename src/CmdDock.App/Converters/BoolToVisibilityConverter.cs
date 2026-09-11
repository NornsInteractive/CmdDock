using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace CmdDock_App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        bool isTrue = value is true;
        if (Invert) isTrue = !isTrue;
        return isTrue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        bool isVisible = value is Visibility v && v == Visibility.Visible;
        return Invert ? !isVisible : isVisible;
    }
}
