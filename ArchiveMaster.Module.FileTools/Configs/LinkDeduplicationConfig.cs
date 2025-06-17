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

    public override void Check()
    {
        CheckDir(Dir, "目录");
    }
}