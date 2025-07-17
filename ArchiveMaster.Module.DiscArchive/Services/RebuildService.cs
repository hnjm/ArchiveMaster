using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using ArchiveMaster.Configs;
using ArchiveMaster.Services;
using ArchiveMaster.ViewModels;
using ArchiveMaster.ViewModels.FileSystem;
using DiscFile = ArchiveMaster.ViewModels.FileSystem.DiscFile;

namespace ArchiveMaster.Services
{
    public class RebuildService(AppConfig appConfig) : DiscServiceBase<RebuildConfig>(appConfig)
    {
        public List<RebuildError> rebuildErrors;
        private Dictionary<string, List<DiscFile>> files;

        public FileSystemTree FileTree { get; private set; }
        public IReadOnlyList<RebuildError> RebuildErrors => rebuildErrors.AsReadOnly();

        public override Task ExecuteAsync(CancellationToken token)
        {
            rebuildErrors = new List<RebuildError>();
            long length = 0;
            int count = 0;
            return Task.Run(() =>
            {
                int count = files.Sum(p => p.Value.Count);
                int index = 0;
                long currentLength = 0;
                long totalLength = files.Values.Sum(p => p.Sum(q => q.Length));

                foreach (var dir in files.Keys)
                {
                    token.ThrowIfCancellationRequested();
                    FilesLoopOptions options = FilesLoopOptions.Builder()
                        .SetCount(index, count)
                        .SetLength(currentLength, totalLength)
                        .AutoApplyFileLengthProgress()
                        .AutoApplyStatus()
                        .Catch((file, ex) => { rebuildErrors.Add(new RebuildError(file as DiscFile, ex.Message)); })
                        .Build();

                    var states = TryForFiles(files[dir], (file, s) =>
                    {
                        length += file.Length;
                        var srcPath = Path.Combine(dir, file.DiscName);
                        var distPath = Path.Combine(Config.TargetDir, file.Path);
                        var distFileDir = Path.GetDirectoryName(distPath);
                        NotifyMessage($"正在重建{s.GetFileNumberMessage()}：{file.Path}");
                        if (!Directory.Exists(distFileDir) && !Config.CheckOnly)
                        {
                            Directory.CreateDirectory(distFileDir);
                        }

                        if (File.Exists(distPath) && Config.SkipIfExisted)
                        {
                            throw new Exception("文件已存在");
                        }

                        string md5;
                        md5 = Config.CheckOnly ? GetMD5(srcPath) : CopyAndGetHash(srcPath, distPath);

                        if (md5 != file.Md5)
                        {
                            throw new Exception("MD5验证失败");
                        }

                        if ((File.GetLastWriteTime(srcPath) - file.Time).Duration().TotalSeconds >
                            Config.MaxTimeToleranceSecond)
                        {
                            throw new Exception("修改时间不一致");
                        }
                    }, token, options);
                    index = states.FileIndex;
                    currentLength = states.AccumulatedLength;
                }
            }, token);
        }

        public override IEnumerable<SimpleFileInfo> GetInitializedFiles()
        {
            return null;
        }

        public override async Task InitializeAsync(CancellationToken token)
        {
            NotifyProgressIndeterminate();
            NotifyMessage("正在建立文件树");
            FileSystemTree tree = FileSystemTree.CreateRoot();
            await Task.Run(() =>
            {
                files = ReadFileList(Config.DiscDirs);
                int count = files.Sum(p => p.Value.Count);
                int index = 0;
                foreach (var dir in files.Keys)
                {
                    FilesLoopOptions options = FilesLoopOptions.Builder()
                        .SetCount(index, count)
                        .AutoApplyFileNumberProgress()
                        .Build();
                    var states = TryForFiles(files[dir], (file, s) =>
                    {
                        NotifyMessage($"正在列举目录{dir}中的文件：{file.Name}");
                        string filePath = Path.Combine(dir, file.DiscName);
                        if (!File.Exists(filePath))
                        {
                            throw new FileNotFoundException(filePath);
                        }

                        var pathParts = file.Path.Split('\\', '/');
                        var current = tree;
                        for (int i = 0; i < pathParts.Length - 1; i++)
                        {
                            var part = pathParts[i];
                            if (current.Directories.Any(p => p.Name == part))
                            {
                                current = current.Directories.First(p => p.Name == part);
                            }
                            else
                            {
                                current = current.AddChild(part);
                            }
                        }

                        var treeFile = current.AddFile(file.Name);
                        treeFile.File = file;
                    }, token, options);
                    index = states.FileIndex;
                }
            }, token);
            FileTree = tree;
        }
    }
}