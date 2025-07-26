using System.Diagnostics;
using System.Globalization;
using ArchiveMaster.ViewModels;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MatchingFileInfo = ArchiveMaster.ViewModels.FileSystem.MatchingFileInfo;

namespace ArchiveMaster.Converters;

public class DirStructureSyncTypeDescriptionConverter : IValueConverter
{
    public static readonly DirStructureSyncTypeDescriptionConverter Instance = new();
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var data = value as MatchingFileInfo;
        if (value == null)
        {
            return null;
        }
        Debug.Assert(data != null);
        switch (parameter as string)
        {
            case nameof(TextBlock.Text):
                if (data.RightPosition)
                {
                    return "\ue930";
                }

                return data.MultipleMatches ? "\ue7ba" : "\ue783";

            case nameof(TextBlock.Foreground):
                if (data.RightPosition)
                {
                    return Brushes.Green;
                }

                return data.MultipleMatches ? Brushes.Red : Brushes.Yellow;

            case nameof(ToolTip):
                if (data.RightPosition)
                {
                    return "文件已经位于正确的位置，无需进行处理";
                }

                return data.MultipleMatches ? "源文件对应多个模板文件，请仔细甄别" : "文件位置不匹配";
            default:
                throw new ArgumentException("未知的返回类型");
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}