using System.Collections.ObjectModel;
using ArchiveMaster.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveMaster.Configs;

public partial class RenameConfig : ConfigBase
{
    [ObservableProperty]
    private bool manual;

    [ObservableProperty]
    private string manualMaps = "";

    [ObservableProperty]
    private string dir;

    [ObservableProperty]
    private string searchPattern;

    [ObservableProperty]
    private string replacePattern;

    [ObservableProperty]
    private RenameTargetType renameTarget = RenameTargetType.File;

    [ObservableProperty]
    private SearchMode searchMode = SearchMode.Contain;

    [ObservableProperty]
    private RenameMode renameMode = RenameMode.ReplaceMatched;

    [ObservableProperty]
    private bool searchPath;

    [ObservableProperty]
    private bool ignoreCase = true;
    
    [ObservableProperty]
    private bool includeSubDirs = true;

    public override void Check()
    {
        CheckDir(Dir, "操作目录");
        CheckEmpty(SearchPattern, "搜索关键词");
        if (RenameTarget == RenameTargetType.Folder)
        {
            if (RenameMode is RenameMode.ReplaceExtension or RenameMode.ReplaceName)
            {
                throw new Exception("当枚举类型为文件夹时，只能替换关键词或替换全部");
            }
        }
    }
}