using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveMaster.ViewModels.FileSystem;

public partial class LinkDeduplicationFileInfo(FileSystemInfo file, string topDir) : SimpleFileInfo(file, topDir)
{
    [ObservableProperty]
    private bool canMakeHardLink;

    [ObservableProperty]
    private string hash;
}