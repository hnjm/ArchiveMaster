using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ArchiveMaster.Converters;

public class BoolTextWrapConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TextWrapping.Wrap : TextWrapping.NoWrap;
        }
        
        return TextWrapping.NoWrap;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TextWrapping wrapping)
        {
            return wrapping == TextWrapping.Wrap;
        }
        
        return false;
    }
}