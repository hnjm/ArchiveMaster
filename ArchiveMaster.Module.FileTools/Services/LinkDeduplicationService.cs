﻿using ArchiveMaster.Configs;
using ArchiveMaster.Helpers;
using ArchiveMaster.ViewModels.FileSystem;
using FzLib.IO;

namespace ArchiveMaster.Services;

public class LinkDeduplicationService(AppConfig appConfig)
    : TwoStepServiceBase<LinkDeduplicationConfig>(appConfig)
{
    public TreeDirInfo TreeRoot { get; private set; }

    public override IEnumerable<SimpleFileInfo> GetInitializedFiles()
    {
        return TreeRoot.Flatten();
    }
    public override async Task ExecuteAsync(CancellationToken token)
    {
        await Task.Run(() =>
        {
            var groups = TreeRoot.SubDirs.CheckedOnly().ToList();
            TryForFiles(groups, (group, s) =>
            {
                var sourceFile = group.SubFiles[0];
                sourceFile.Complete();
                foreach (var file in group.SubFiles.Skip(1))
                {
                    NotifyMessage($"正在创建硬链接{s.GetFileNumberMessage()}：{file.RelativePath}");
                    FileHelper.DeleteByConfig(file.Path);
                    HardLinkCreator.CreateHardLink(file.Path, sourceFile.Path);
                    file.Complete();
                }
            }, token, FilesLoopOptions.Builder().AutoApplyStatus().AutoApplyFileNumberProgress().Build());
        }, token);
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

                Progress<FileProcessProgress> progress = new Progress<FileProcessProgress>(p =>
                {
                    NotifyProgress(1.0 * (length + p.ProcessedBytes) / totalLength);
                    NotifyMessage(
                        $"正在计算Hash（{numMsg}，本文件{1.0 * p.ProcessedBytes / 1024 / 1024:0}MB/{1.0 * p.TotalBytes / 1024 / 1024:0}MB）：{f.RelativePath}");
                });
                string hash = await FileHashHelper.ComputeHashAsync(f.Path, Config.HashType, cancellationToken: token,
                    progress: progress);
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
                group.SetRelativePath(sameHashFiles.Key);
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