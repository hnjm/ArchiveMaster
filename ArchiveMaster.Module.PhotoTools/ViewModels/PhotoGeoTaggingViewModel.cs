using ArchiveMaster.Configs;
using ArchiveMaster.Services;
using ArchiveMaster.ViewModels.FileSystem;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveMaster.ViewModels;

public partial class PhotoGeoTaggingViewModel(AppConfig appConfig)
    : TwoStepViewModelBase<PhotoGeoTaggingService, PhotoGeoTaggingConfig>(appConfig)
{
    [ObservableProperty]
    private List<GpsFileInfo> files = new List<GpsFileInfo>();

    protected override Task OnInitializedAsync()
    {
        Files = [.. Service.Files];
        return base.OnInitializedAsync();
    }

    protected override void OnReset()
    {
        Files = new List<GpsFileInfo>();
    }
}