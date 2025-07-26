using ArchiveMaster.Configs;
using ArchiveMaster.Services;
using ArchiveMaster.ViewModels.FileSystem;
using CommunityToolkit.Mvvm.ComponentModel;
using FzLib.Avalonia.Dialogs;

namespace ArchiveMaster.ViewModels;

public partial class PhotoGeoTaggingViewModel(AppConfig appConfig,IDialogService dialogService)
    : TwoStepViewModelBase<PhotoGeoTaggingService, PhotoGeoTaggingConfig>(appConfig,dialogService)
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