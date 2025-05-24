using System.Text.RegularExpressions;
using System.Xml.Linq;
using ArchiveMaster.Configs;
using ArchiveMaster.Helpers;
using ArchiveMaster.ViewModels;
using ArchiveMaster.ViewModels.FileSystem;

namespace ArchiveMaster.Services
{
    public class FileCopyTestService(AppConfig appConfig) : TwoStepServiceBase<FileCopyTestConfig>(appConfig)
    {
        public override Task ExecuteAsync(CancellationToken token = default)
        {
            var files = Files.Where(p => p.IsChecked).ToList();
            var totalLength = files.Sum(p => p.Length);
            long currentLength = 0;
            return TryForFilesAsync(files,
                async (file, state) =>
                {
                    NotifyMessage($"正在复制{file.Name}");
                    await FileIOHelper.CopyFileAsync(file.Path, file.DestinationPath,
                        progress: new Progress<FileCopyProgress>(
                            p => { NotifyProgress(1.0 * (currentLength + p.BytesCopied) / totalLength); }),
                        cancellationToken: token);
                    File.SetLastWriteTimeUtc(file.DestinationPath, file.Time);
                    currentLength += file.Length;
                },
                token, FilesLoopOptions.Builder().AutoApplyFileLengthProgress().AutoApplyStatus().Build());
        }

        public List<CopyingFile> Files { get; private set; }

        public override async Task InitializeAsync(CancellationToken token = default)
        {
            var files = new DirectoryInfo(Config.SourceDir)
                .EnumerateFiles("*", FileEnumerateExtension.GetEnumerationOptions())
                .ApplyFilter(token)
                .Select(f => new CopyingFile(f, Config.SourceDir));
            Files = new List<CopyingFile>();
            await TryForFilesAsync(files,
                (f, s) =>
                {
                    f.DestinationPath = Path.Combine(Config.DestinationDir,
                        Path.GetRelativePath(Config.SourceDir, f.Path));
                    Files.Add(f);
                }, token, FilesLoopOptions.DoNothing());
        }
    }
}