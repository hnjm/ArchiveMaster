using System.Collections.ObjectModel;
using ArchiveMaster.Basic;
using ArchiveMaster.Configs;
using ArchiveMaster.Services;
using ArchiveMaster.ViewModels.FileSystem;
using CommunityToolkit.Mvvm.ComponentModel;
using FzLib.Avalonia.Dialogs;

namespace ArchiveMaster.ViewModels;

public partial class
    DirStructureCloneViewModel(AppConfig appConfig, IDialogService dialogService)
    : TwoStepViewModelBase<DirStructureCloneService, DirStructureCloneConfig>(appConfig, dialogService)
{
    [ObservableProperty]
    private BulkObservableCollection<SimpleFileInfo> treeFiles;

    protected override Task OnInitializedAsync()
    {
        var files = new BulkObservableCollection<SimpleFileInfo>();
        files.AddRange(Service.RootDir.Subs);
        TreeFiles = files;
        return base.OnInitializedAsync();
    }


    protected override void OnReset()
    {
        TreeFiles = null;
    }
}