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
            return status switch
            {
                PipelineJobStatus.Queued => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),      // #64748B Muted slate
                PipelineJobStatus.Downloading => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 14, 165, 233)),   // #0EA5E9 Sky Blue
                PipelineJobStatus.Extracting => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 168, 85, 247)),   // #A855F7 Purple
                PipelineJobStatus.Formatting => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)),    // #6366F1 Indigo
                PipelineJobStatus.Done => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),          // #10B981 Emerald
                PipelineJobStatus.Failed => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),         // #EF4444 Red
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
