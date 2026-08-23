using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Distill.App.Converters;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            if (parameter is string p && p.Equals("invert", StringComparison.OrdinalIgnoreCase))
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }

        return false;
    }
}
