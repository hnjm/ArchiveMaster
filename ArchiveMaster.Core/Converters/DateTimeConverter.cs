using System.Globalization;
using Avalonia.Data.Converters;

namespace ArchiveMaster.Converters;

using System;
using System.Globalization;
using System.Windows;

public class DateTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        switch (value)
        {
            case null:
            case DateTime { Ticks: 0 }:
                return "";
            case DateTime dt:
                return dt.ToString(CultureInfo.CurrentCulture.DateTimeFormat);
            default:
                throw new ArgumentException("值不为DateTime类型", nameof(value));
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        /*
原本是写了ConvertBack方法的，但是20241027花了一个小时才修好了一个坑爹的BUG，罪魁祸首就在这儿。
起因是我在写删除快照的代码，需要判断是否存在后续的Full快照，并且删除到下一个Full快照之前：
       var nextFullSnapshot = await GetValidSnapshots()
            .Where(p => p.BeginTime > snapshot.BeginTime)
            .Where(p => p.Type == SnapshotType.Full || p.Type == SnapshotType.VirtualFull)
            .OrderBy(p => p.BeginTime)
            .FirstOrDefaultAsync();
但是即使后面没有Full快照，nextFullSnapshot还是不为null，而是等于snapshot。
用ToQueryString发现，snapshot.BeginTime的精度被截断了，到秒就没了。
然后一路排查，一开始以为是EF Core的问题，用了个DateTimeToTicksConverter，发现还是有问题。
一步步检查，发现在LoadSnapshots的时候精度是正常的，说明EF读的数据库时间是对的，没有精度截断。
最后就查到这里，原来是因为DateGrid的TextColumn做了个ConvertBack导致精度被截断。
太坑爹了。
         */
        throw new InvalidOperationException($"不支持{nameof(ConvertBack)}");
    }
}