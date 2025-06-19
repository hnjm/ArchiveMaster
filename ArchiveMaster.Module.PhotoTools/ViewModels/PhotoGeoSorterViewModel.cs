using ArchiveMaster.Configs;
using ArchiveMaster.Services;
using ArchiveMaster.ViewModels.FileSystem;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ArchiveMaster.ViewModels;

public partial class PhotoGeoSorterViewModel(AppConfig appConfig)
    : TwoStepViewModelBase<PhotoGeoSorterService, PhotoGeoSorterConfig>(appConfig)
{
    [ObservableProperty]
    public ObservableCollection<GpsFileInfo> files;

    protected override Task OnInitializedAsync()
    {
        Files = new ObservableCollection<GpsFileInfo>(Service.Files);
        return base.OnInitializedAsync();
    }
}