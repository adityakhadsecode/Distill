using Distill.Core.Pipeline;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Distill.App.Converters;

public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is PipelineJobStatus status)
        {
            var isBackground = parameter is string p && p.Equals("background", StringComparison.OrdinalIgnoreCase);
            var isBorder = parameter is string b && b.Equals("border", StringComparison.OrdinalIgnoreCase);

            byte alpha = isBackground ? (byte)38 : isBorder ? (byte)90 : (byte)255;

            return status switch
            {
                PipelineJobStatus.Queued => new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, 100, 116, 139)),      // Slate
                PipelineJobStatus.Downloading => new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, 2, 132, 199)),   // Sky
                PipelineJobStatus.Extracting => new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, 147, 51, 234)),   // Purple
                PipelineJobStatus.Formatting => new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, 79, 70, 229)),    // Indigo
                PipelineJobStatus.Done => new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, 16, 185, 129)),          // Emerald
                PipelineJobStatus.Failed => new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, 239, 68, 68)),         // Red
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
