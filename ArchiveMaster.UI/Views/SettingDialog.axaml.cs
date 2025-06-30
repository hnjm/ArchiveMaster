using ArchiveMaster.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using FzLib.Avalonia.Dialogs;

namespace ArchiveMaster.Views;

public partial class SettingDialog : DialogHost
{
    public SettingDialog()
    {
        InitializeComponent();
    }

    protected override void OnPrimaryButtonClick()
    {
        Close();
    }
}