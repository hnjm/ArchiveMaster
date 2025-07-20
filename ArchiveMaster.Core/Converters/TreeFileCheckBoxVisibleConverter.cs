using System.Globalization;
using ArchiveMaster.ViewModels.FileSystem;
using ArchiveMaster.Views;
using Avalonia.Data.Converters;

namespace ArchiveMaster.Converters;

public class TreeFileCheckBoxVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return false;
        }
        if (parameter is not TreeFileDataGrid td)
        {
            throw new Exception($"参数应当为{nameof(TreeFileDataGrid)}");
        }

        if (value is not SimpleFileInfo f)
        {
            throw new Exception($"绑定对象应当为{nameof(SimpleFileInfo)}");
        }

        if (f.IsDir && td.IsDirCheckBoxVisible)
        {
            return true;
        }

        if (!f.IsDir && td.IsFileCheckBoxVisible)
        {
            return true;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}