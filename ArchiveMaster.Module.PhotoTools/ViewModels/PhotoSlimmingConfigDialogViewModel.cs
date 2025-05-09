using CommunityToolkit.Mvvm.ComponentModel;
using Mapster;
using ArchiveMaster.Configs;

namespace ArchiveMaster.ViewModels;

public partial class PhotoSlimmingConfigViewModel : ObservableObject
{
    [ObservableProperty]
    private PhotoSlimmingConfig config = new PhotoSlimmingConfig();

    public PhotoSlimmingConfigViewModel(PhotoSlimmingConfig config)
    {
        config.Adapt(Config);
    }
    public PhotoSlimmingConfigViewModel()
    {
    }
}
