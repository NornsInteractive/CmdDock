using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace CmdDock_App.Converters;

public class BoolToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush SuccessBrush = new(Colors.ForestGreen);
    private static readonly SolidColorBrush FailedBrush = new(Colors.Crimson);

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b && b)
        {
            return SuccessBrush;
        }
        return FailedBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
