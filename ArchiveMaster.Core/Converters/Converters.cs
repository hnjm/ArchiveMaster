using FzLib.Avalonia.Converters;

namespace ArchiveMaster.Converters;

public static class Converters
{
    public static readonly Number2AlignmentConverter Alignment = FzLib.Avalonia.Converters.Converters.Alignment;

    public static readonly BoolLogicConverter AndLogic = FzLib.Avalonia.Converters.Converters.AndLogic;

    public static readonly Bool2FontWeightConverter
        BoldFontWeight = FzLib.Avalonia.Converters.Converters.BoldFontWeight;

    public static readonly Bool2OpacityStyleConverter BoldOpacity = FzLib.Avalonia.Converters.Converters.BoldOpacity;

    public static readonly Count2BoolConverter CountGreaterThanZero =
        FzLib.Avalonia.Converters.Converters.CountGreaterThanZero;

    public static readonly Count2BoolConverter CountIsZero = FzLib.Avalonia.Converters.Converters.CountIsZero;

    public static readonly DescriptionConverter Description = FzLib.Avalonia.Converters.Converters.Description;

    public static readonly Equal2BoolConverter EqualWithParameter = FzLib.Avalonia.Converters.Converters.EqualWithParameter;

    public static readonly FileLengthConverter FileLength = FzLib.Avalonia.Converters.Converters.FileLength;

    public static readonly FilePickerFilterConverter FilePickerFilter =
        FzLib.Avalonia.Converters.Converters.FilePickerFilter;

    public static readonly InverseBoolConverter InverseBool = FzLib.Avalonia.Converters.Converters.InverseBool;

    public static readonly Null2BoolConverter IsNotNull = FzLib.Avalonia.Converters.Converters.IsNotNull;

    public static readonly Null2BoolConverter IsNull = FzLib.Avalonia.Converters.Converters.IsNull;

    public static readonly Bool2FontStyleConverter ItalicFontStyle =
        FzLib.Avalonia.Converters.Converters.ItalicFontStyle;

    public static readonly Bool2FontWeightConverter LightFontWeight =
        FzLib.Avalonia.Converters.Converters.LightFontWeight;

    public static readonly Equal2BoolConverter NotEqualWithParameter = FzLib.Avalonia.Converters.Converters.NotEqualWithParameter;

    public static readonly BoolLogicConverter OrLogic = FzLib.Avalonia.Converters.Converters.OrLogic;

    public static readonly StringListConverter StringList = FzLib.Avalonia.Converters.Converters.StringList;

    public static readonly Bool2TextWrappingConverter TextWrapping = FzLib.Avalonia.Converters.Converters.TextWrapping;

    public static readonly Number2ThicknessConverter Thickness = FzLib.Avalonia.Converters.Converters.Thickness;

    public static readonly TimeSpanConverter TimeSpan = FzLib.Avalonia.Converters.Converters.TimeSpan;

    public static readonly TimeSpanNumberConverter TimeSpanNumber = FzLib.Avalonia.Converters.Converters.TimeSpanNumber;

    public static readonly Bool2TextDecorationConverter UnderlineTextDecoration =
        FzLib.Avalonia.Converters.Converters.UnderlineTextDecoration;

    public static readonly Bool2TextDecorationConverter OverlineTextDecoration =
        FzLib.Avalonia.Converters.Converters.OverlineTextDecoration;

    public static readonly BitmapAssetValueConverter BitmapAssetValue = new();

    public static readonly DateTimeConverter DateTime = new();
}