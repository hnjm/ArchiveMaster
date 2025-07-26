using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ArchiveMaster.Enums;
using ArchiveMaster.ViewModels;
using FzLib.IO;

namespace ArchiveMaster.Configs;

public partial class BackupTask : ConfigBase, ICloneable
{
    [ObservableProperty]
    private string id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string backupDir;

    [ObservableProperty]
    private FileFilterRule filter = new FileFilterRule();

    [ObservableProperty]
    private bool byTimeInterval = true;

    [ObservableProperty]
    private bool byWatching = true;

    [ObservableProperty]
    private bool isDefaultVirtualBackup;

    [ObservableProperty]
    private string name = "新备份任务";

    [ObservableProperty]
    private string sourceDir;

    /// <summary>
    /// 定时备份时间间隔
    /// </summary>
    [ObservableProperty]
    private TimeSpan timeInterval = TimeSpan.FromHours(1);

    /// <summary>
    /// 当自动备份的增量备份超过这个值后，将进行一次全量备份
    /// </summary>
    [ObservableProperty]
    private int maxAutoIncrementBackupCount = 100;

    partial void OnMaxAutoIncrementBackupCountChanged(int value)
    {
        if (value < 0)
        {
            MaxAutoIncrementBackupCount = 0;
        }
    }

    #region 临时变量

    [ObservableProperty]
    [property: JsonIgnore]
    private DateTime lastBackupTime;

    [ObservableProperty]
    [property: JsonIgnore]
    private DateTime lastFullBackupTime;

    [ObservableProperty]
    [property: JsonIgnore]
    private int snapshotCount;

    [ObservableProperty]
    [property: JsonIgnore]
    private int validSnapshotCount;

    [ObservableProperty]
    [property: JsonIgnore]
    private BackupTaskStatus status = BackupTaskStatus.Ready;

    [ObservableProperty]
    [property: JsonIgnore]
    private string message;

    #endregion

    public override void Check()
    {
        if(Filter==null)
        {
            Filter = new FileFilterRule();
        }
        CheckDir(SourceDir, "需要备份的目录");
        CheckDir(BackupDir, "备份文件存放目录");
        CheckEmpty(Name, "备份任务名");
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}