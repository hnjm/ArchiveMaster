using ArchiveMaster.Basic;
using ArchiveMaster.Configs;
using ArchiveMaster.Services;
using ArchiveMaster.ViewModels.FileSystem;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveMaster.ViewModels;

public partial class LinkDeduplicationViewModel(AppConfig appConfig)
    : TwoStepViewModelBase<LinkDeduplicationService, LinkDeduplicationConfig>(appConfig)
{
    [ObservableProperty]
    private BulkObservableCollection<SimpleFileInfo> groups;

    protected override Task OnInitializedAsync()
    {
        Groups = new BulkObservableCollection<SimpleFileInfo>(Service.TreeRoot.SubDirs);
        return base.OnInitializedAsync();
    }

    protected override void OnReset()
    {
        Groups = null;
    }
}