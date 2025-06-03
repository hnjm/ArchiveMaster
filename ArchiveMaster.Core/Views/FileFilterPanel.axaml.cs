using ArchiveMaster.Configs;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Mapster;

namespace ArchiveMaster.Views;

public partial class FileFilterPanel : UserControl
{
    public static readonly StyledProperty<FileFilterConfig> FilterProperty =
        AvaloniaProperty.Register<FileFilterControl, FileFilterConfig>(
            nameof(Filter), defaultBindingMode: BindingMode.TwoWay);


    public FileFilterPanel()
    {
        InitializeComponent();
    }

   
    public FileFilterConfig Filter
    {
        get => GetValue(FilterProperty);
        set => SetValue(FilterProperty, value);
    }

    private void ResetButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Filter == null)
        {
            Filter = new FileFilterConfig();
            return;
        }
        
        var newObj = new FileFilterConfig();
        newObj.UseRegex = Filter.UseRegex;
        newObj.Adapt(Filter);
    }
}