using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Distill.App.Converters;

/// <summary>
/// Converts a boolean readiness state into a Segoe Fluent Icon glyph.
/// True -> \uE73E (Accept checkmark)
/// False -> \uE7BA (Important warning triangle) or \uEA39 (Critical cross)
/// </summary>
public class BoolToGlyphConverter : IValueConverter
{
    public string TrueGlyph { get; set; } = "\uE73E"; // Accept Checkmark
    public string FalseGlyph { get; set; } = "\uE7BA"; // Important Warning

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isReady && isReady)
        {
            return TrueGlyph;
        }

        if (parameter is string paramStr && paramStr.Equals("critical", StringComparison.OrdinalIgnoreCase))
        {
            return "\uEA39"; // Error cross
        }

        return FalseGlyph;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a boolean readiness state into a semantic theme brush.
/// True -> SystemFillColorSuccessBrush
/// False -> SystemFillColorCautionBrush (or SystemFillColorCriticalBrush if parameter="critical")
/// </summary>
public class BoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isReady = value is bool b && b;
        var isCritical = parameter is string p && p.Equals("critical", StringComparison.OrdinalIgnoreCase);

        var resourceKey = isReady
            ? "SystemFillColorSuccessBrush"
            : (isCritical ? "SystemFillColorCriticalBrush" : "SystemFillColorCautionBrush");

        if (Application.Current.Resources.TryGetValue(resourceKey, out var brushObj) && brushObj is Brush brush)
        {
            return brush;
        }

        // Fallback colors if theme resource is missing
        return isReady
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129))
            : (isCritical 
                ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68))
                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
