using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace ArchiveMaster.Converters;

/// <summary>
/// 根据值是否为 null，返回不同的 <see cref="GridLength"/>。
/// 若 <see cref="Invert"/> 为 false：null → 0，其它 → 参数；
/// 若 <see cref="Invert"/> 为 true：null → 参数，其它 → 0。
/// </summary>
public class ConditionalGridLengthConverter : IValueConverter
{
    public static readonly ConditionalGridLengthConverter NullMeansZero = new() { Invert = false };
    public static readonly ConditionalGridLengthConverter NullMeansParameter = new() { Invert = true };

    /// <summary>
    /// 控制 null 和非 null 的含义是否互换。
    /// </summary>
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string str)
        {
            throw new ArgumentException("参数必须为字符串", nameof(parameter));
        }

        bool isNull = value is null;
        return isNull ^ Invert ? new GridLength(0) : GridLength.Parse(str);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}