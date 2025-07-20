using ArchiveMaster.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using FzLib.IO;

namespace ArchiveMaster.Configs;

public partial class PackingConfig : ConfigBase
{
    [ObservableProperty]
    private string sourceDir;

    [ObservableProperty]
    private string targetDir;

    [ObservableProperty]
    private DateTime earliestTime = DateTime.MinValue;

    [ObservableProperty]
    private FileFilterRule filter=new FileFilterRule();

    [ObservableProperty]
    private PackingType packingType = PackingType.Copy;

    [ObservableProperty]
    private int discSizeMB = 23500;

    [ObservableProperty]
    private int maxDiscCount = 1000;

    public override void Check()
    {
        CheckDir(SourceDir,"源目录");
        CheckEmpty(TargetDir,"目标目录");
        if (DiscSizeMB < 100)
        {
            throw new Exception("单盘容量过小");
        }

        if (MaxDiscCount < 1)
        {
            throw new Exception("盘片数量应大于等于1盘");
        }
    }
}