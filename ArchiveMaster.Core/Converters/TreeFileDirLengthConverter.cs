using System.Globalization;
using ArchiveMaster.ViewModels;
using ArchiveMaster.ViewModels.FileSystem;
using Avalonia;
using Avalonia.Data.Converters;
using FzLib;
using TreeFileDirInfo = ArchiveMaster.ViewModels.FileSystem.TreeFileDirInfo;

namespace ArchiveMaster.Converters;

public class TreeFileDirLengthConverter : IValueConverter
{
    public const string DefaultFormat = "{D}个子文件夹，{F}个子文件";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        var fileOrDir = value as TreeFileDirInfo ?? throw new Exception("值必须为TreeFileDirInfo类型");

        if (fileOrDir.IsDir && fileOrDir is TreeDirInfo dir)
        {
            if (parameter != null && parameter is not string)
            {
                throw new ArgumentException($"{nameof(parameter)}应当为字符串或空");
            }

            var str = parameter as string;
            if (str == null)
            {
                str = DefaultFormat;
            }

            return str
                .Replace("{D}", dir.SubFolderCount.ToString("N0", culture))
                .Replace("{F}", dir.SubFileCount.ToString("N0", culture))
                .Replace("{L}", FileDirLength2StringConverter.Convert(dir.Length));
        }

        return NumberConverter.ByteToFitString(fileOrDir.Length, 2, " B", " KB", " MB", " GB", " TB");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}