using System.Globalization;
using ArchiveMaster.ViewModels.FileSystem;
using Avalonia.Data.Converters;
using FzLib;

namespace ArchiveMaster.Converters;

public class FileDirLength2StringConverter : IValueConverter
{
    public string DirString { get; set; } = "文件夹";

    public static string Convert(long length)
    {
        return NumberConverter.ByteToFitString(length, 2, " B", " KB", " MB", " GB", " TB");
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        var fileOrDir = value as SimpleFileInfo ?? throw new Exception("值必须为SimpleFileDirInfo类型");
        if (fileOrDir.IsDir)
        {
            return DirString;
        }

        return Convert(fileOrDir.Length);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}