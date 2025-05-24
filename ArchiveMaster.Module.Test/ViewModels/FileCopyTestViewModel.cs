using System.Collections.ObjectModel;
using ArchiveMaster.Configs;
using ArchiveMaster.Helpers;
using ArchiveMaster.Services;
using ArchiveMaster.ViewModels.FileSystem;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArchiveMaster.ViewModels;

public partial class FileCopyTestViewModel(AppConfig appConfig) : TwoStepViewModelBase<FileCopyTestService,FileCopyTestConfig>(appConfig)
{
    [ObservableProperty]
    private ObservableCollection<CopyingFile> files;

    protected override Task OnInitializedAsync()
    {
        Files = new ObservableCollection<CopyingFile>(Service.Files);
        return base.OnInitializedAsync();
    }
}