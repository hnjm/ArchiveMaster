using ArchiveMaster.Configs;
using ArchiveMaster.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ArchiveMaster.Helpers;
using ArchiveMaster.ViewModels.FileSystem;

namespace ArchiveMaster.Services
{
    public class TwinFileCleanerService(AppConfig appConfig)
        : TwoStepServiceBase<TwinFileCleanerConfig>(appConfig)
    {
        public List<TwinFileInfo> DeletingFiles { get; set; }

        public override Task ExecuteAsync(CancellationToken token)
        {
            var files = DeletingFiles.CheckedOnly().ToList();
            return TryForFilesAsync(files, (file, s) =>
            {
                NotifyMessage($"正在删除{s.GetFileNumberMessage()}：{file.Name}");
                FileDeleteHelper.DeleteByConfig(file.Path);
            }, token, FilesLoopOptions.Builder().AutoApplyStatus().AutoApplyFileNumberProgress().Build());
        }

        public override IEnumerable<SimpleFileInfo> GetInitializedFiles()
        {
            return DeletingFiles.Cast<SimpleFileInfo>();
        }
        public override async Task InitializeAsync(CancellationToken token)
        {
            DeletingFiles = new List<TwinFileInfo>();
            List<SimpleFileInfo> masterFiles = null;
            Dictionary<string, List<FileInfo>> dir2AllFiles = new Dictionary<string, List<FileInfo>>();
            await Task.Run(() =>
            {
                masterFiles = Config.MasterExtensions.Select(e => new DirectoryInfo(Config.Dir)
                        .EnumerateFiles($"*.{e}", FileEnumerateExtension.GetEnumerationOptions())
                        .ApplyFilter(token)
                        .Select(p => new SimpleFileInfo(p, Config.Dir)))
                    .SelectMany(p => p)
                    .ToList();
                var allFiles =
                    new DirectoryInfo(Config.Dir).EnumerateFiles("*", FileEnumerateExtension.GetEnumerationOptions());
                dir2AllFiles = allFiles.GroupBy(p => p.DirectoryName)
                    .ToDictionary(p => p.Key, p => p.ToList());
            }, token);
            await TryForFilesAsync(masterFiles, (masterFile, s) =>
            {
                NotifyMessage($"正在查找同名不同后缀的文件{s.GetFileNumberMessage()}");
                var dir = Path.GetDirectoryName(masterFile.Path);
                Debug.Assert(dir2AllFiles.ContainsKey(dir));
                var dirFiles = dir2AllFiles[dir];
                foreach (var pattern in Config.DeletingPatterns)
                {
                    var tempPattern = pattern.Replace("{Name}", Path.GetFileNameWithoutExtension(masterFile.Path));
                    var auxiliaryFiles = dirFiles.Where(p => FileFilterHelper.IsMatchedByPattern(p.Name, tempPattern));
                    DeletingFiles.AddRange(auxiliaryFiles.Select(p => new TwinFileInfo(p, masterFile)));
                }
            }, token, FilesLoopOptions.Builder().AutoApplyFileNumberProgress().Build());
        }
    }
}