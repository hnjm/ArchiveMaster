﻿using ArchiveMaster.Enums;
using ArchiveMaster.Models;
using ArchiveMaster.ViewModels;
using FzLib.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using ArchiveMaster.Configs;
using ArchiveMaster.Helpers;
using ArchiveMaster.Services;
using ArchiveMaster.ViewModels.FileSystem;
using SyncFileInfo = ArchiveMaster.ViewModels.FileSystem.SyncFileInfo;

namespace ArchiveMaster.Services
{
    public class Step3Service(AppConfig appConfig) : TwoStepServiceBase<OfflineSyncStep3Config>(appConfig)
    {
        private readonly DateTime createTime = DateTime.Now;
        private Aes aes;
        public List<SyncFileInfo> DeletingDirectories { get; private set; }
        public Dictionary<string, List<string>> LocalDirectories { get; private set; }
        public List<SyncFileInfo> UpdateFiles { get; private set; }

        public static string GetNoDuplicateDirectory(string path, string suffixFormat = " ({i})")
        {
            if (!Directory.Exists(path))
            {
                return path;
            }

            if (!suffixFormat.Contains("{i}"))
            {
                throw new ArgumentException("后缀应包含“{i}”以表示索引");
            }

            int num = 2;
            string directoryName = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            string text;
            while (true)
            {
                text = Path.Combine(directoryName,
                    fileNameWithoutExtension + suffixFormat.Replace("{i}", num.ToString()) + extension);
                if (!Directory.Exists(text))
                {
                    break;
                }

                num++;
            }

            return text;
        }

        public static string GetNoDuplicateFile(string path, string suffixFormat = " ({i})")
        {
            if (!File.Exists(path))
            {
                return path;
            }

            if (!suffixFormat.Contains("{i}"))
            {
                throw new ArgumentException("后缀应包含“{i}”以表示索引");
            }

            int num = 2;
            string directoryName = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            string text;
            while (true)
            {
                text = Path.Combine(directoryName,
                    fileNameWithoutExtension + suffixFormat.Replace("{i}", num.ToString()) + extension);
                if (!File.Exists(text))
                {
                    break;
                }

                num++;
            }

            return text;
        }

        public Task DeleteEmptyDirectoriesAsync()
        {
            return Task.Run(() =>
            {
                foreach (var dir in DeletingDirectories)
                {
                    Delete(dir.TopDirectory, dir.Path);
                }
            });
        }

        public override async Task ExecuteAsync(CancellationToken token = default)
        {
            if (!string.IsNullOrWhiteSpace(Config.Password))
            {
                aes = Services.AesExtension.GetDefault(Config.Password);
            }

            long totalLength = 0;
            List<SyncFileInfo> files = null;
            await Task.Run(async () =>
            {
                var updateFiles = UpdateFiles.Where(p => p.IsChecked).ToList();
                totalLength = updateFiles
                    .Where(p => p.UpdateType is not (FileUpdateType.Delete or FileUpdateType.Move))
                    .Sum(p => p.Length);
                files = updateFiles.OrderByDescending(p => p.UpdateType).ToList();

                long length = 0;
                await TryForFilesAsync(files, async (file, s) =>
                {
                    //先处理移动，然后处理修改，这样能避免一些问题（2022-12-17）
                    string numMsg = s.GetFileNumberMessage("{0}/{1}");
                    NotifyMessage($"正在处理（{numMsg}）：{file.RelativePath}");
                    string patch = file.TempName == null ? null : Path.Combine(Config.PatchDir, file.TempName);
                    if (file.UpdateType is not (FileUpdateType.Delete or FileUpdateType.Move) &&
                        !File.Exists(patch))
                    {
                        throw new Exception("补丁文件不存在");
                    }

                    string target = file.Path;
                    string oldPath = file.OldRelativePath == null
                        ? null
                        : Path.Combine(file.TopDirectory, file.OldRelativePath);
                    if (!Directory.Exists(Path.GetDirectoryName(target)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                    }

                    Progress<FileProcessProgress> progress = null;

                    async Task CopyThisFileAsync()
                    {
                        progress = new Progress<FileProcessProgress>(p =>
                        {
                            NotifyProgress(1.0 * (length + p.ProcessedBytes) / totalLength);
                            NotifyMessage(
                                $"正在复制（{numMsg}，本文件{1.0 * p.ProcessedBytes / 1024 / 1024:0}MB/{1.0 * p.TotalBytes / 1024 / 1024:0}MB）：{file.RelativePath}");
                        });
                        await CopyFileAsync(patch, target, file.Time, progress, token);
                    }

                    switch (file.UpdateType)
                    {
                        case FileUpdateType.Add:
                            if (File.Exists(target))
                            {
                                Delete(file.TopDirectory, target);
                            }

                            await CopyThisFileAsync();
                            break;
                        case FileUpdateType.Modify:
                            if (File.Exists(target))
                            {
                                Delete(file.TopDirectory, target);
                            }

                            await CopyThisFileAsync();
                            break;
                        case FileUpdateType.Delete:
                            if (!File.Exists(target))
                            {
                                throw new Exception("应当为待删除文件，但文件不存在");
                            }

                            Delete(file.TopDirectory, target);
                            break;

                        case FileUpdateType.Move:
                            if (!File.Exists(oldPath))
                            {
                                throw new Exception("应当为移动后文件，但源文件不存在");
                            }
                            else if (File.Exists(target))
                            {
                                throw new Exception("应当为移动后文件，但目标文件已存在");
                            }

                            File.Move(oldPath, target);
                            break;
                        default:
                            throw new InvalidEnumArgumentException();
                    }
                }, token, FilesLoopOptions.Builder().AutoApplyStatus().AutoApplyFileNumberProgress().Finally(file =>
                {
                    var f = file as SyncFileInfo;
                    if (f.UpdateType is FileUpdateType.Add or FileUpdateType.Modify)
                    {
                        length += f.Length;
                    }

                    NotifyProgress(1.0 * length / totalLength);
                }).Build());

                NotifyMessage($"正在查找空目录");
                AnalyzeEmptyDirectories(token);
            }, token);
        }

        public override IEnumerable<SimpleFileInfo> GetInitializedFiles()
        {
            return UpdateFiles.Cast<SimpleFileInfo>();
        }
        public override async Task InitializeAsync(CancellationToken token = default)
        {
            var patchFile = Path.Combine(Config.PatchDir, "file.os2");
            if (!File.Exists(patchFile))
            {
                throw new FileNotFoundException("目录中不存在os2文件");
            }

            await Task.Run(() =>
            {
                var step2 = ZipService.ReadFromZip<Step2Model>(patchFile);

                UpdateFiles = step2.Files;
                LocalDirectories = step2.LocalDirectories;
                if (string.IsNullOrWhiteSpace(Config.Password))
                {
                    if (UpdateFiles.Any(p =>
                            p.TempName != null && p.TempName.EndsWith(Step2Service.EncryptionFileSuffix)))
                    {
                        throw new ArgumentException("备份文件已加密，但没有提供密码");
                    }
                }

                TryForFiles(UpdateFiles, (file, s) =>
                {
                    string patch = file.TempName == null ? null : Path.Combine(Config.PatchDir, file.TempName);

                    string target = file.Path;
                    string oldPath = file.OldRelativePath == null
                        ? null
                        : Path.Combine(file.TopDirectory, file.OldRelativePath);
                    if (file.UpdateType is not (FileUpdateType.Delete or FileUpdateType.Move) && !File.Exists(patch))
                    {
                        file.Warn("补丁文件不存在");
                        file.IsChecked = false;
                    }
                    else
                    {
                        NotifyMessage($"正在处理{s.GetFileNumberMessage()}：{file.Path}");
                        switch (file.UpdateType)
                        {
                            case FileUpdateType.Add:
                                if (File.Exists(target))
                                {
                                    file.Warn("应当为新增文件，但文件已存在");
                                    file.IsChecked = false;
                                }

                                break;
                            case FileUpdateType.Modify:
                                if (!File.Exists(target))
                                {
                                    file.Warn("应当为修改后文件，但文件不存在");
                                }

                                break;
                            case FileUpdateType.Delete:
                                if (!File.Exists(target))
                                {
                                    file.Warn("应当为待删除文件，但文件不存在");
                                    file.IsChecked = false;
                                }

                                break;
                            case FileUpdateType.Move:
                                if (!File.Exists(oldPath))
                                {
                                    file.Warn("应当为移动后文件，但源文件不存在");
                                    file.IsChecked = false;
                                }
                                else if (File.Exists(target))
                                {
                                    file.Warn("应当为移动后文件，但目标文件已存在");
                                    file.IsChecked = false;
                                }

                                break;
                            default:
                                throw new InvalidEnumArgumentException();
                        }
                    }
                }, token, FilesLoopOptions.Builder().AutoApplyFileNumberProgress().Build());
            }, token);
        }

        private static bool IsDirectory(string path)
        {
            FileAttributes attr = File.GetAttributes(path);
            return attr.HasFlag(FileAttributes.Directory);
        }

        private void AnalyzeEmptyDirectories(CancellationToken token)
        {
            DeletingDirectories = new List<SyncFileInfo>();

            foreach (var topDir in LocalDirectories.Keys)
            {
                if (!Directory.Exists(topDir))
                {
                    continue;
                }

                HashSet<string> deletingDirsInThisTopDir = new HashSet<string>();
                foreach (var offsiteSubDir in Directory
                             .EnumerateDirectories(topDir, "*", SearchOption.AllDirectories)
                             .ToList())
                {
                    token.ThrowIfCancellationRequested();
                    if (!LocalDirectories[topDir]
                            .Contains(Path.GetRelativePath(topDir, offsiteSubDir))) //本地已经没有远程的这个目录了
                    {
                        if (!Directory.EnumerateFiles(offsiteSubDir).Any()) //并且远程的这个目录是空的
                        {
                            deletingDirsInThisTopDir.Add(offsiteSubDir);
                        }
                        else if (!Directory.EnumerateFiles(offsiteSubDir).Skip(1).Any() //目录里只有缩略图
                                 && Path.GetFileName(Directory.EnumerateFiles(offsiteSubDir).First()).ToLower() ==
                                 "thumbs.db")
                        {
                            deletingDirsInThisTopDir.Add(offsiteSubDir);
                        }
                    }
                }


                //通过两层循环，删除位于空目录下的空目录
                foreach (var dir1 in deletingDirsInThisTopDir.ToList()) //外层循环，dir1为内层空目录
                {
                    token.ThrowIfCancellationRequested();
                    foreach (var dir2 in deletingDirsInThisTopDir) //内曾循环，dir2为外层空目录
                    {
                        if (dir1 == dir2)
                        {
                            continue;
                        }

                        if (dir1.StartsWith(dir2)) //如果dir2位于dir1的外层，那么dir1就不需要单独删除
                        {
                            deletingDirsInThisTopDir.Remove(dir1);
                            break;
                        }
                    }
                }

                DeletingDirectories.AddRange(deletingDirsInThisTopDir.Select(p => new SyncFileInfo()
                    { Path = p, TopDirectory = topDir }));
            }
        }
        private async Task CopyFileAsync(string source, string destination,
            DateTime fileTime,
            Progress<FileProcessProgress> progress,
            CancellationToken cancellationToken)
        {
            if (source.EndsWith(Step2Service.EncryptionFileSuffix))
            {
                if (string.IsNullOrWhiteSpace(Config.Password))
                {
                    throw new ArgumentException("备份文件已加密，但没有提供密码");
                }

                await aes.DecryptFileAsync(source, destination, progress: progress,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await FileCopyHelper.CopyFileAsync(source, destination, progress: progress,
                    cancellationToken: cancellationToken);
            }

            File.SetLastWriteTime(destination, fileTime);
        }

        private void Delete(string rootDir, string filePath)
        {
            Debug.Assert(IsDirectory(filePath) || true);
            if (!filePath.StartsWith(rootDir))
            {
                throw new ArgumentException("文件不在目录中");
            }

            switch (Config.DeleteMode)
            {
                case DeleteMode.Delete:
                    if (IsDirectory(filePath))
                    {
                        Directory.Delete(filePath, true);
                    }
                    else
                    {
                        File.Delete(filePath);
                    }

                    break;
                case DeleteMode.MoveToDeletedFolder:
                    string relative = Path.GetRelativePath(rootDir, filePath);
                    string deletedFolder = Path.Combine(Path.GetPathRoot(filePath), Config.DeleteDir,
                        createTime.ToString("yyyyMMdd-HHmmss"),
                        rootDir.Replace(":\\", "#").Replace('\\', '#').Replace('/', '#'));
                    string target = Path.Combine(deletedFolder, relative);
                    string dir = Path.GetDirectoryName(target);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    if (IsDirectory(filePath))
                    {
                        Directory.Move(filePath, GetNoDuplicateDirectory(target));
                    }
                    else
                    {
                        File.Move(filePath, GetNoDuplicateFile(target));
                    }
                    break;
                
                case DeleteMode.RecycleBinPrefer:
                    FileHelper.DeleteByConfig(filePath);
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }
    }
}