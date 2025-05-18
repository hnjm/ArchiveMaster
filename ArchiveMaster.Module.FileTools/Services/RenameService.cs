using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using ArchiveMaster.Configs;
using ArchiveMaster.Enums;
using ArchiveMaster.Helpers;
using ArchiveMaster.ViewModels;
using ArchiveMaster.ViewModels.FileSystem;
using Avalonia.Media;
using FzLib.Avalonia.Converters;
using FzLib.Program;
using RenameFileInfo = ArchiveMaster.ViewModels.FileSystem.RenameFileInfo;

namespace ArchiveMaster.Services;

public class RenameService(AppConfig appConfig)
    : TwoStepServiceBase<RenameConfig>(appConfig)
{
    private static readonly Dictionary<string, Regex> regexes = new Dictionary<string, Regex>();
    public IReadOnlyList<RenameFileInfo> Files { get; private set; }

    public override async Task ExecuteAsync(CancellationToken token = default)
    {
        var processingFiles = Files.Where(p => p.IsMatched && p.IsChecked).ToList();
        var duplicates = processingFiles
            .Select(p => p.GetNewPath())
            .GroupBy(p => p)
            .Where(p => p.Count() > 1)
            .Select(p => p.Key);
        if (duplicates.Any())
        {
            throw new Exception("有一些文件（夹）的目标路径相同：" + string.Join('、', duplicates));
        }

        //重命名为临时文件名，避免有可能新的文件名和其他文件的旧文件名一致导致错误的问题
        //假设直接重命名文件 A -> B，但同时另一个文件正在从 B -> C，可能会导致冲突，因为文件系统在操作过程中会认为目标路径已经存在。
        //临时名称中转确保了重命名操作在一个独立的“空间”中完成，避免了名称重叠的问题。
        await TryForFilesAsync(processingFiles, (file, s) =>
        {
            NotifyMessage($"正在重命名（第一步，共二步）{s.GetFileNumberMessage()}：{file.Name}=>{file.NewName}");
            file.TempPath = Path.Combine(Path.GetDirectoryName(file.Path), Guid.NewGuid().ToString());
            if (file.IsDir)
            {
                Directory.Move(file.Path, file.TempPath);
            }
            else
            {
                File.Move(file.Path, file.TempPath);
            }
        }, token, FilesLoopOptions.Builder().AutoApplyFileNumberProgress().Build());

        //重命名为目标文件名
        await TryForFilesAsync(processingFiles, (file, s) =>
        {
            NotifyMessage($"正在重命名（第二步，共二步）{s.GetFileNumberMessage()}：{file.Name}=>{file.NewName}");
            if (file.IsDir)
            {
                Directory.Move(file.TempPath, file.GetNewPath());
            }
            else
            {
                File.Move(file.TempPath, file.GetNewPath());
            }
        }, token, FilesLoopOptions.Builder().AutoApplyStatus().AutoApplyFileNumberProgress().Build());
    }

    public override async Task InitializeAsync(CancellationToken token = default)
    {
        regexOptions = Config.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
        stringComparison = Config.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        NotifyProgressIndeterminate();
        NotifyMessage("正在查找文件");


        await Task.Run(() =>
        {
            FilePlaceholderReplacer placeholderReplacer = new FilePlaceholderReplacer(Config.ReplacePattern ?? "");

            // 获取所有待处理文件
            IEnumerable<FileSystemInfo> files;
            if (Config.RenameTarget == RenameTargetType.File)
            {
                files = new DirectoryInfo(Config.Dir)
                    .EnumerateFiles("*", FileEnumerateExtension.GetEnumerationOptions());
            }
            else
            {
                files = new DirectoryInfo(Config.Dir)
                    .EnumerateDirectories("*", FileEnumerateExtension.GetEnumerationOptions());
            }

            List<RenameFileInfo> renameFiles = new List<RenameFileInfo>();
            HashSet<string> usedPaths = new HashSet<string>(FileNameHelper.GetStringComparer());


            foreach (var file in files)
            {
                var renameFile = new RenameFileInfo(file, Config.Dir);
                renameFile.IsMatched = IsMatched(renameFile);

                if (renameFile.IsMatched)
                {
                    string originalNewName = Rename(placeholderReplacer, renameFile);
                    renameFile.NewName = originalNewName; // 临时存储原始目标名称
                }
                else
                {
                    usedPaths.Add(file.FullName);
                }

                renameFiles.Add(renameFile);
            }

            //有一种情况：三个文件，abc、abbc、abbbbc，b重命名为bb，
            //实际是无冲突的，但若直接检测会认为有冲突
            //所以采用了一些方法来规避这个问题，但不完美。
            foreach (var renameFile in renameFiles
                         .Where(p => p.IsMatched)
                         .Where(p => p.Name != p.NewName))
            {
                string desiredPath = renameFile.GetNewPath();

                string finalPath = FileNameHelper.GenerateUniquePath(desiredPath, usedPaths);
                if (finalPath != desiredPath)
                {
                    renameFile.HasUniqueNameProcessed = true;
                }

                renameFile.NewName = Path.GetFileName(finalPath);
                usedPaths.Add(finalPath);
            }

            Files = renameFiles.AsReadOnly();
        }, token);
    }

    private Regex GetRegex(string pattern)
    {
        if (regexes.TryGetValue(pattern, out Regex r))
        {
            return r;
        }

        r = new Regex(pattern, regexOptions);
        regexes.Add(pattern, r);
        return r;
    }

    private RegexOptions regexOptions;

    private bool IsMatched(RenameFileInfo file)
    {
        string name = Config.SearchPath ? file.Path : file.Name;

        return Config.SearchMode switch
        {
            SearchMode.Contain => name.Contains(Config.SearchPattern, stringComparison),
            SearchMode.EqualWithExtension => Path.GetExtension(name).Equals(Config.SearchPattern, stringComparison),
            SearchMode.EqualWithName => Path.GetFileNameWithoutExtension(name)
                .Equals(Config.SearchPattern, stringComparison),
            SearchMode.Equal => name.Equals(Config.SearchPattern, stringComparison),
            SearchMode.Regex => GetRegex(Config.SearchPattern).IsMatch(name),
            _ => throw new ArgumentOutOfRangeException()
        };
    }


    private StringComparison stringComparison;

    private string Rename(FilePlaceholderReplacer replacer, RenameFileInfo file)
    {
        string name = file.Name;
        string matched = null;
        if (Config.RenameMode is RenameMode.ReplaceMatched or RenameMode.RetainMatched
            or RenameMode.RetainMatchedExtension)
        {
            matched = Config.SearchMode == SearchMode.Regex
                ? GetRegex(Config.SearchPattern).Match(name).Value
                : Config.SearchPattern;
        }

        return Config.RenameMode switch
        {
            RenameMode.ReplaceMatched => name.Replace(matched, replacer.GetTargetName(file), stringComparison),
            RenameMode.ReplaceExtension => $"{Path.GetFileNameWithoutExtension(name)}.{replacer.GetTargetName(file)}",
            RenameMode.ReplaceName => replacer.GetTargetName(file) + Path.GetExtension(name),
            RenameMode.ReplaceAll => replacer.GetTargetName(file),
            RenameMode.RetainMatched => matched,
            RenameMode.RetainMatchedExtension => $"{matched}{Path.GetExtension(name)}",
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}