using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveMaster.ViewModels.FileSystem;

public partial class TwinFileInfo : SimpleFileInfo
{
    [ObservableProperty]
    private SimpleFileInfo masterFile;

    public TwinFileInfo(FileInfo auxiliaryFile, SimpleFileInfo masterFile) : base(auxiliaryFile,
        masterFile.TopDirectory)
    {
        MasterFile = masterFile;
    }
}