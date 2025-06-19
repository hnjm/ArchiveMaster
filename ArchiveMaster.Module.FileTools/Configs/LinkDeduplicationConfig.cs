using ArchiveMaster.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveMaster.Configs;

public partial class LinkDeduplicationConfig : ConfigBase
{
    [ObservableProperty]
    private string dir;

    [ObservableProperty]
    private FileFilterConfig filter = new FileFilterConfig();

    [ObservableProperty]
    private bool allowDifferentTime = true;

    [ObservableProperty]
    private FileHashHelper.HashAlgorithmType hashType = FileHashHelper.HashAlgorithmType.SHA256;

    public override void Check()
    {
        CheckDir(Dir, "目录");
    }
}