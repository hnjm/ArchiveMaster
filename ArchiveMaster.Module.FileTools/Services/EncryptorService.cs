using ArchiveMaster.Configs;
using ArchiveMaster.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArchiveMaster.Enums;
using ArchiveMaster.Helpers;
using EncryptorFileInfo = ArchiveMaster.ViewModels.FileSystem.EncryptorFileInfo;

namespace ArchiveMaster.Services
{
    public class EncryptorService(AppConfig appConfig) : TwoStepServiceBase<EncryptorConfig>(appConfig)
    {
        public const string EncryptedFileExtension = ".ept";
        public const string DirectoryStructureFile = "$files$.txt";
        public List<EncryptorFileInfo> ProcessingFiles { get; set; }
        public int BufferSize { get; set; } = 1024 * 1024;
        private Aes aes;

        public override async Task ExecuteAsync(CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(ProcessingFiles, nameof(ProcessingFiles));

            await Task.Run(() =>
            {
                int index = 0;
                aes = GetAes();

                bool isEncrypting = IsEncrypting();

                //初始化文件结构加密字典
                Dictionary<string, string> dirStructureDic = CreateDirStructureDic();

                //初始化进度通知
                var files = ProcessingFiles.CheckedOnly().ToList();
                int count = files.Count;

                var progressReport = new Progress<FileCopyProgress>(
                    p =>
                    {
                        string baseMessage = isEncrypting ? "正在加密文件" : "正在解密文件";
                        NotifyMessage(baseMessage +
                                      $"（{index}/{count}，当前文件{1.0 * p.BytesCopied / 1024 / 1024:0}MB/{1.0 * p.TotalBytes / 1024 / 1024:0}MB），当前文件：{Path.GetFileName(p.SourceFilePath)}");
                    });

                TryForFiles(files, (file, s) =>
                {
                    index++;

                    ProcessFileNames(file, dirStructureDic);
                    if (!CheckFileAndDirectoryExists(file))
                    {
                        return;
                    }

                    if (isEncrypting)
                    {
                        aes.EncryptFile(file.Path, file.TargetPath, BufferSize, progressReport, token);
                        file.IsFileNameEncrypted = Config.EncryptFileNames;
                    }
                    else
                    {
                        aes.DecryptFile(file.Path, file.TargetPath, BufferSize, progressReport, token);
                        file.IsFileNameEncrypted = false;
                    }

                    file.IsEncrypted = isEncrypting;
                    File.SetLastWriteTime(file.TargetPath, File.GetLastWriteTime(file.Path));

                    if (Config.DeleteSourceFiles)
                    {
                        if (File.GetAttributes(file.Path).HasFlag(FileAttributes.ReadOnly))
                        {
                            File.SetAttributes(file.Path, FileAttributes.Normal);
                        }

                        File.Delete(file.Path);
                    }
                }, token, FilesLoopOptions.Builder().AutoApplyStatus().AutoApplyFileLengthProgress().Build());

                //文件结构加密，输出文件名对应关系
                if (Config.EncryptDirectoryStructure && isEncrypting)
                {
                    using var fs = File.CreateText(Path.Combine(Config.EncryptedDir, DirectoryStructureFile));
                    foreach (var kv in dirStructureDic)
                    {
                        fs.WriteLine($"{kv.Key}\t{kv.Value}");
                    }

                    fs.Close();
                }
            }, token);
        }

        /// <summary>
        /// 检查文件是否存在，并根据策略作出操作
        /// </summary>
        /// <param name="file"></param>
        /// <returns>是否继续加密或解密</returns>
        private bool CheckFileAndDirectoryExists(EncryptorFileInfo file)
        {
            string path = file.TargetPath;
            if (File.Exists(path))
            {
                switch (Config.FilenameDuplicationPolicy)
                {
                    case FilenameDuplicationPolicy.Overwrite:
                        if (File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly))
                        {
                            File.SetAttributes(path, FileAttributes.Normal);
                        }

                        File.Delete(path);
                        break;

                    case FilenameDuplicationPolicy.Skip:
                        file.Warn("目标文件已存在");
                        return false;

                    case FilenameDuplicationPolicy.Throw:
                        file.Error("目标文件已存在");
                        return false;
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return true;
        }


        private Dictionary<string, string> CreateDirStructureDic()
        {
            Dictionary<string, string> dirStructureDic = null;
            if (Config.EncryptDirectoryStructure)
            {
                dirStructureDic = new Dictionary<string, string>();
                if (!IsEncrypting())
                {
                    var fileListFile = Path.Combine(Config.EncryptedDir, DirectoryStructureFile);
                    if (!File.Exists(fileListFile))
                    {
                        throw new Exception("目录结构文件不存在");
                    }

                    foreach (var line in File.ReadLines(fileListFile))
                    {
                        var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length != 2)
                        {
                            throw new Exception("目录结构文件内容不符合规范");
                        }

                        dirStructureDic.Add(parts[0], parts[1]);
                    }
                }
            }

            return dirStructureDic;
        }

        private void ProcessFileNames(EncryptorFileInfo file, Dictionary<string, string> longNames)
        {
            var isEncrypting = IsEncrypting();
            ArgumentNullException.ThrowIfNull(file);
            if (Config.EncryptDirectoryStructure)
            {
                ArgumentNullException.ThrowIfNull(longNames);
                if (isEncrypting)
                {
                    string relativePath = Path.GetRelativePath(GetSourceDir(), file.Path);
                    string encryptedFileName =
                        Convert.ToBase64String(aes.Encrypt(Encoding.UTF8.GetBytes(relativePath)));
                    string hash = Hash(encryptedFileName);
                    longNames.Add(hash, encryptedFileName);
                    file.TargetName = hash;
                    file.TargetPath = Path.Combine(GetDistDir(), hash);
                }
                else
                {
                    if (!longNames.TryGetValue(file.Name, out string encryptedFileName))
                    {
                        throw new Exception("在文件名字典文件中没有找到对应文件的原文件名");
                    }

                    string rawRelativePath =
                        Encoding.UTF8.GetString(aes.Decrypt(Convert.FromBase64String(encryptedFileName)));
                    file.TargetName = Path.GetFileName(rawRelativePath);
                    file.TargetPath = Path.Combine(GetDistDir(), rawRelativePath);
                }
            }
            else
            {
                string targetName = isEncrypting
                    ? (Config.EncryptFileNames ? EncryptFileName(file.Name) : $"{file.Name}{EncryptedFileExtension}")
                    : DecryptFileName(file.Name);

                string relativeDir = Path.GetDirectoryName(Path.GetRelativePath(GetSourceDir(), file.Path));
                if (isEncrypting && Config.EncryptFolderNames)
                {
                    relativeDir = EncryptFoldersNames(relativeDir);
                }
                else if (!isEncrypting &&
                         relativeDir.EndsWith(EncryptedFileExtension, StringComparison.InvariantCultureIgnoreCase))
                {
                    relativeDir = DecryptFoldersNames(relativeDir);
                }

                file.TargetPath = Path.Combine(GetDistDir(), relativeDir, targetName);
                file.TargetName = targetName;
            }

            file.TargetRelativePath = Path.GetRelativePath(GetDistDir(), file.TargetPath);
        }

        private static string Hash(string input)
        {
            return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(input)));
        }

        public override async Task InitializeAsync(CancellationToken token)
        {
            List<EncryptorFileInfo> files = new List<EncryptorFileInfo>();

            var sourceDir = GetSourceDir();
            if (!Directory.Exists(sourceDir))
            {
                throw new Exception("源目录不存在");
            }

            NotifyProgressIndeterminate();
            NotifyMessage("正在枚举文件");

            await TryForFilesAsync(new DirectoryInfo(sourceDir)
                .EnumerateFiles("*", FileEnumerateExtension.GetEnumerationOptions())
                .ApplyFilter(token)
                .Select(p => new EncryptorFileInfo(p, sourceDir)), (file, s) =>
            {
                var isEncrypted = IsEncryptedFile(file.Path);
                file.IsFileNameEncrypted = isEncrypted && IsNameEncrypted(file.Name);
                file.IsEncrypted = isEncrypted;
                file.RelativePath = Path.GetRelativePath(sourceDir, file.Path);
                if (file.Name != DirectoryStructureFile)
                {
                    NotifyMessage($"正在加入{s.GetFileNumberMessage()}：{file.Name}");
                    files.Add(file);
                }
            }, token, FilesLoopOptions.DoNothing());

            ProcessingFiles = files;
        }

        private static string Base64ToFileNameSafeString(string base64)
        {
            if (string.IsNullOrEmpty(base64))
            {
                throw new ArgumentException("Base64 string cannot be null or empty");
            }

            string safeString = base64.Replace('+', '-')
                .Replace('/', '_')
                .Replace('=', '~');

            return safeString;
        }

        private static string FileNameSafeStringToBase64(string safeString)
        {
            if (string.IsNullOrEmpty(safeString))
            {
                throw new ArgumentException("Safe string cannot be null or empty");
            }

            string base64 = safeString.Replace('-', '+')
                .Replace('_', '/')
                .Replace('~', '=');

            return base64;
        }

        private static bool IsEncryptedFile(string fileName)
        {
            if (fileName.EndsWith(EncryptedFileExtension, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static bool IsNameEncrypted(string fileName)
        {
            ArgumentException.ThrowIfNullOrEmpty(fileName);
            if (!IsEncryptedFile(fileName))
            {
                throw new ArgumentException("文件未被加密");
            }

            string base64 = FileNameSafeStringToBase64(Path.GetFileNameWithoutExtension(fileName));
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out _);
        }

        /// <summary>
        /// 解密文件名，可包括或不包括后缀
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string DecryptFileName(string fileName)
        {
            ArgumentException.ThrowIfNullOrEmpty(fileName);
            bool isNameEncrypted = IsNameEncrypted(fileName);
            if (fileName.EndsWith(EncryptedFileExtension, StringComparison.InvariantCultureIgnoreCase))
            {
                fileName = Path.GetFileNameWithoutExtension(fileName);
            }

            if (isNameEncrypted)
            {
                string base64 = FileNameSafeStringToBase64(fileName);
                var bytes = Convert.FromBase64String(base64);
                bytes = aes.Decrypt(bytes);
                return Encoding.Default.GetString(bytes);
            }

            return fileName;
        }

        /// <summary>
        /// 加密文件名，不包含后缀
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private string EncryptFileName(string fileName)
        {
            ArgumentException.ThrowIfNullOrEmpty(fileName);

            if (IsEncryptedFile(fileName))
            {
                throw new ArgumentException("文件已被加密");
            }

            byte[] bytes = Encoding.Default.GetBytes(fileName);
            bytes = aes.Encrypt(bytes);
            string base64 = Convert.ToBase64String(bytes);
            string safeFileName = Base64ToFileNameSafeString(base64);
            return safeFileName + EncryptedFileExtension;
        }

        private Aes GetAes()
        {
            Aes aes = Aes.Create();
            aes.Mode = Config.CipherMode;
            aes.Padding = Config.PaddingMode;
            aes.SetStringKey(Config.Password);
            aes.IV = MD5.HashData(Encoding.UTF8.GetBytes(Config.Password));
            return aes;
        }

        private string GetDistDir()
        {
            if (IsEncrypting())
            {
                return Config.EncryptedDir;
            }

            return Config.RawDir;
        }

        private string GetSourceDir()
        {
            if (IsEncrypting())
            {
                return Config.RawDir;
            }

            return Config.EncryptedDir;
        }

        private bool IsEncrypting()
        {
            ArgumentNullException.ThrowIfNull(Config);
            return Config.Type == EncryptorConfig.EncryptorTaskType.Encrypt;
        }

        private string EncryptFoldersNames(string relativePath)
        {
            var parts = relativePath.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = EncryptFileName(parts[i]);
            }

            return string.Join(Path.DirectorySeparatorChar, parts);
        }

        private string DecryptFoldersNames(string relativePath)
        {
            var parts = relativePath.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = DecryptFileName(parts[i]);
            }

            return string.Join(Path.DirectorySeparatorChar, parts);
        }

        private void EncryptFolders(string dir, bool includeSelf = true)
        {
            foreach (var subDir in Directory.EnumerateDirectories(dir))
            {
                EncryptFolders(subDir);
            }

            if (includeSelf)
            {
                string newName = EncryptFileName(Path.GetFileName(dir));
                string newPath = Path.Combine(Path.GetDirectoryName(dir), newName);
                Directory.Move(dir, newPath);
            }
        }
    }
}