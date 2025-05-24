using ArchiveMaster.ViewModels.FileSystem;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveMaster.ViewModels;

public partial class CopyingFile(FileInfo file, string topDir) : SimpleFileInfo(file, topDir)
{
    [ObservableProperty]
    private string destinationPath;
}