using ArchiveMaster.Configs;
using ArchiveMaster.Helpers;
using ArchiveMaster.ViewModels.FileSystem;

namespace ArchiveMaster.Services;

public class LinkDeduplicationService(AppConfig appConfig)
    : TwoStepServiceBase<LinkDeduplicationConfig>(appConfig)
{
    public TreeDirInfo TreeRoot { get; private set; }

    public override async Task ExecuteAsync(CancellationToken token)
    {
        // await Task.Run(() =>
        // {
        //     NotifyMessage("正在检查问题");
        //     if (Config.CleaningDir == Config.ReferenceDir) //删除自身
        //     {
        //         foreach (var group in DuplicateGroups.SubDirs)
        //         {
        //             if (group.SubFiles.Count(p => p.IsChecked) == group.SubFileCount) //如果所有文件均被勾选
        //             {
        //                 throw new InvalidOperationException($"文件{group.Name}的所有相同文件均被勾选待删除，会造成数据丢失");
        //             }
        //         }
        //     }
        //
        //     NotifyMessage("正在将文件移动到回收站");
        //     int index = 0;
        //     foreach (var group in DuplicateGroups.SubDirs)
        //     {
        //         NotifyMessage($"正在删除与“{group.Name}”相同的文件");
        //         foreach (var file in group.SubFiles.CheckedOnly())
        //         {
        //             try
        //             {
        //                 var distPath = Path.Combine(Config.RecycleBin, file.RelativePath);
        //                 Directory.CreateDirectory(Path.GetDirectoryName(distPath));
        //                 File.Move(file.Path, distPath);
        //                 file.Complete();
        //             }
        //             catch (Exception ex)
        //             {
        //                 file.Error(ex);
        //             }
        //         }
        //
        //         NotifyProgress(1.0 * index++ / DuplicateGroups.SubFolderCount);
        // }
        // }, token);
    }

    public override Task InitializeAsync(CancellationToken token)
    {
        List<LinkDeduplicationFileInfo> files = new List<LinkDeduplicationFileInfo>();
        Dictionary<string, LinkDeduplicationFileInfo> hash2File = new Dictionary<string, LinkDeduplicationFileInfo>();
        return Task.Run(async () =>
        {
            NotifyMessage("正在枚举文件");

            files = new DirectoryInfo(Config.Dir)
                .EnumerateFiles("*", FileEnumerateExtension.GetEnumerationOptions())
                .Select(p => new LinkDeduplicationFileInfo(p, Config.Dir))
                .ApplyFilter(token, Config.Filter)
                .ToList();

            long totalLength = files.Sum(p => p.Length);
            long length = 0;
            NotifyMessage("正在计算文件Hash");
            await TryForFilesAsync(files, async (f, s) =>
            {
                string numMsg = s.GetFileNumberMessage("{0}/{1}");

                Progress<FileCopyProgress> progress = new Progress<FileCopyProgress>(p =>
                {
                    NotifyProgress(1.0 * (length + p.BytesCopied) / totalLength);
                    NotifyMessage(
                        $"正在计算Hash（{numMsg}，本文件{1.0 * p.BytesCopied / 1024 / 1024:0}MB/{1.0 * p.TotalBytes / 1024 / 1024:0}MB）：{f.RelativePath}");
                });
                string hash = await FileHashHelper.ComputeHashAsync(f.Path, cancellationToken: token, progress: progress);
                f.Hash = hash;
                if (!hash2File.TryAdd(hash, f))
                {
                    var sameFile = hash2File[hash];
                    sameFile.CanMakeHardLink = true;
                    f.CanMakeHardLink = true;
                }

                length += f.Length;
                NotifyProgress(1.0 * length / totalLength);
            }, token, FilesLoopOptions.DoNothing());

            NotifyMessage("正在统计相同文件");
            var tree = TreeDirInfo.CreateEmptyTree();
            foreach (var sameHashFiles in files
                         .Where(p => p.CanMakeHardLink)
                         .GroupBy(p => p.Hash))
            {
                var sameHashFileList = sameHashFiles.ToList();
                var group = tree.AddSubDir($"{sameHashFileList.Count}个相同文件");
                if (sameHashFiles.DistinctBy(p => p.Length).Count() == 1)
                {
                    group.Length = sameHashFileList[0].Length;
                    if (sameHashFiles.DistinctBy(p => p.Time).Count() == 1)
                    {
                        group.Time = sameHashFileList[0].Time;
                        group.IsChecked = true;
                    }
                    else
                    {
                        group.Warn("文件哈希相同，但修改日期不同");
                        if (Config.AllowDifferentTime)
                        {
                            group.IsChecked = true;
                        }
                    }
                }
                else
                {
                    group.Error("文件哈希相同，但大小不同");
                }

                foreach (var file in sameHashFileList)
                {
                    group.AddSubFile(file);
                }
            }

            TreeRoot = tree;
        }, token);
    }
}