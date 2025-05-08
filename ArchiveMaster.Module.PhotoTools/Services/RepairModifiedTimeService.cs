using FzLib;
using MetadataExtractor;
using ArchiveMaster.Configs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ArchiveMaster.Helpers;
using ArchiveMaster.ViewModels;
using ArchiveMaster.ViewModels.FileSystem;

namespace ArchiveMaster.Services
{
    public class RepairModifiedTimeService(AppConfig appConfig)
        : TwoStepServiceBase<RepairModifiedTimeConfig>(appConfig)
    {
        public string[] Extensions = { "jpg", "jpeg", "heif", "heic", "dng" };

        public ConcurrentBag<ExifTimeFileInfo> Files { get; } = new ConcurrentBag<ExifTimeFileInfo>();

        private Regex rRepairTime;

        public override Task ExecuteAsync(CancellationToken token)
        {
            return TryForFilesAsync(Files, (file, s) =>
            {
                if (!file.ExifTime.HasValue)
                {
                    return;
                }

                NotifyMessage($"正在处理{s.GetFileNumberMessage()}：{file.Name}");
                File.SetLastWriteTime(file.Path, file.ExifTime.Value);
            }, token, FilesLoopOptions.Builder().AutoApplyStatus().AutoApplyFileNumberProgress().Build());
        }

        public override async Task InitializeAsync(CancellationToken token)
        {
            rRepairTime = new Regex(@$"\.({string.Join('|', Extensions)})$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
            NotifyProgressIndeterminate();
            NotifyMessage("正在查找文件");
            List<ExifTimeFileInfo> files = null;
            await Task.Run(() =>
            {
                files = new DirectoryInfo(Config.Dir)
                    .EnumerateFiles("*", FileEnumerateExtension.GetEnumerationOptions())
                    .ApplyFilter(token)
                    .Select(p => new ExifTimeFileInfo(p, Config.Dir))
                    .ToList();
            });
            await TryForFilesAsync(files, (file, s) =>
                {
                    NotifyMessage($"正在扫描照片日期{s.GetFileNumberMessage()}");
                    if (rRepairTime.IsMatch(file.Name))
                    {
                        DateTime? exifTime = FindExifTime(file.Path);

                        if (exifTime.HasValue)
                        {
                            var fileTime = file.Time;
                            var duration = (exifTime.Value - fileTime).Duration();
                            if (duration > Config.MaxDurationTolerance)
                            {
                                file.ExifTime = exifTime.Value;
                                Files.Add(file);
                            }
                        }
                    }
                }, token,
                FilesLoopOptions.Builder()
                    .AutoApplyFileNumberProgress()
                    .WithMultiThreads(Config.ThreadCount)
                    .Catch((file, ex) => { Files.Add(file as ExifTimeFileInfo); }).Build());
        }

        private DateTime? FindExifTime(string file)
        {
            IReadOnlyList<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(file);

            foreach (var dir in directories.Where(p => p.Name == "Exif SubIFD"))
            {
                if (dir.TryGetDateTime(36867, out DateTime time1))
                {
                    return time1;
                }

                if (dir.TryGetDateTime(36868, out DateTime time2))
                {
                    return time2;
                }
            }

            MetadataExtractor.Directory dir2 = null;
            if ((dir2 = directories.FirstOrDefault(p => p.Name == "Exif IFD0")) != null)
            {
                if (dir2.TryGetDateTime(306, out DateTime time))
                {
                    return time;
                }
            }

            return null;
        }
    }
}