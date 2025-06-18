using ArchiveMaster.Configs;
using ArchiveMaster.ViewModels;
using System;
using System.Collections.Generic;
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
        public List<SimpleFileInfo> DeletingJpgFiles { get; set; }

        public override Task ExecuteAsync(CancellationToken token)
        {
            var files = DeletingJpgFiles.CheckedOnly().ToList();
            return TryForFilesAsync(files, (file, s) =>
            {
                NotifyMessage($"正在删除{s.GetFileNumberMessage()}：{file.Name}");
                FileDeleteHelper.DeleteByConfig(file.Path);
            }, token, FilesLoopOptions.Builder().AutoApplyStatus().AutoApplyFileNumberProgress().Build());
        }

        public override async Task InitializeAsync(CancellationToken token)
        {
            DeletingJpgFiles = new List<SimpleFileInfo>();
            List<SimpleFileInfo> files = null;
            await Task.Run(() =>
            {
                files = new DirectoryInfo(Config.Dir)
                    .EnumerateFiles("*." + Config.SearchExtension,
                        FileEnumerateExtension.GetEnumerationOptions(matchCasing: MatchCasing.CaseInsensitive))
                    .ApplyFilter(token)
                    .Select(p => new SimpleFileInfo(p, Config.Dir))
                    .ToList();
            }, token);
            await TryForFilesAsync(files, (file, s) =>
            {
                NotifyMessage($"正在查找同名不同后缀的文件{s.GetFileNumberMessage()}");
                var twinFile =
                    $"{Path.Combine(Path.GetDirectoryName(file.Path), Path.GetFileNameWithoutExtension(file.Name))}.{Config.DeletingExtension}";
                if (File.Exists(twinFile))
                {
                    DeletingJpgFiles.Add(new SimpleFileInfo(new FileInfo(twinFile), Config.Dir));
                }
            }, token, FilesLoopOptions.Builder().AutoApplyFileNumberProgress().Build());
        }
    }
}