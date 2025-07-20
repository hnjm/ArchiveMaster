using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using ArchiveMaster.Basic;
using ArchiveMaster.Configs;
using ArchiveMaster.Enums;
using ArchiveMaster.Helpers;
using ArchiveMaster.Models;
using ArchiveMaster.Services;
using ArchiveMaster.ViewModels.FileSystem;
using ArchiveMaster.Views;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FzLib.Avalonia.Messages;

namespace ArchiveMaster.ViewModels;

public partial class BackupManageCenterViewModel
{
    [ObservableProperty]
    private ObservableCollection<BackupFile> createdFiles;

    [ObservableProperty]
    private ObservableCollection<BackupFile> deletedFiles;

    [ObservableProperty]
    private ObservableCollection<BackupFile> fileHistory;

    [ObservableProperty]
    private ObservableCollection<BackupFile> modifiedFiles;

    [ObservableProperty]
    private SimpleFileInfo selectedFile;

    [ObservableProperty]
    private BulkObservableCollection<SimpleFileInfo> treeFiles;

    private async Task LoadFileChangesAsync()
    {
        await using var db = new DbService(SelectedTask);
        var (created, modified, deleted) = await db.GetSnapshotChanges(SelectedSnapshot.Id);
        CreatedFiles = new ObservableCollection<BackupFile>(created.Select(p => new BackupFile(p)));
        ModifiedFiles = new ObservableCollection<BackupFile>(modified.Select(p => new BackupFile(p)));
        DeletedFiles = new ObservableCollection<BackupFile>(deleted.Select(p => new BackupFile(p)));
    }

    private async Task LoadFileHistoryAsync(SimpleFileInfo file)
    {
        Debug.Assert(file is BackupFile);
        await using var db = new DbService(SelectedTask);
        var history = await db.GetFileHistory(file.RelativePath);
        FileHistory = new ObservableCollection<BackupFile>(history.Select(p => new BackupFile(p)));
    }

    private async Task LoadFilesAsync()
    {
        var Service = new RestoreService(SelectedTask);
        var tree = await Service.GetSnapshotFileTreeAsync(SelectedSnapshot.Id);
        tree.Reorder();
        tree.Name = $"快照{SelectedSnapshot.BeginTime}";

        TreeFiles = new BulkObservableCollection<SimpleFileInfo>();
        TreeFiles.Add(tree);
    }

    // [RelayCommand]

    async partial void OnSelectedFileChanged(SimpleFileInfo value)
    {
        if (value is BackupFile)
        {
            await TryDoAsync("加载文件历史记录", async () => await LoadFileHistoryAsync(value));
        }
    }

    [RelayCommand]
    private async Task SaveAsAsync(SimpleFileInfo fileOrDir)
    {
        switch (fileOrDir)
        {
            case BackupFile file:
                await SaveFile(file);
                break;
            case TreeDirInfo dir:
                await SaveFolder(dir);
                break;
        }
    }

    private async Task SaveFile(BackupFile file)
    {
        if (file.Entity.BackupFileName == null)
        {
            await DialogService.ShowErrorDialogAsync("备份文件不存在", "该文件不存在实际备份文件，可能是由虚拟快照生成");
            return;
        }

        var extension = Path.GetExtension(file.Name).TrimStart('.');
        var saveFile = await this.SendMessage(new GetStorageProviderMessage()).StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions()
            {
                DefaultExtension = extension,
                SuggestedFileName = file.Name,
                FileTypeChoices =
                [
                    new FilePickerFileType($"{extension}文件")
                        { Patterns = [$"*.{(extension.Length == 0 ? "*" : extension)}"] }
                ]
            });
        var path = saveFile?.TryGetLocalPath();
        if (path != null)
        {
            var dialog = new FileProgressDialog();
            this.SendMessage(new DialogHostMessage(dialog));
            string backupFile = Path.Combine(SelectedTask.BackupDir, file.Entity.BackupFileName);
            if (!File.Exists(backupFile))
            {
                await DialogService.ShowErrorDialogAsync("备份文件不存在", "该文件不存在实际备份文件，可能是文件丢失");
                return;
            }

            await dialog.CopyFileAsync(backupFile, path, file.Time);
        }
    }

    private async Task SaveFolder(TreeDirInfo dir)
    {
        var folders = await this.SendMessage(new GetStorageProviderMessage()).StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions());
        if (folders is { Count: 1 })
        {
            var rootDir = folders[0].TryGetLocalPath();
            var dialog = new FileProgressDialog();
            this.SendMessage(new DialogHostMessage(dialog));
            var files = dir.Flatten();
            List<string> sourcePaths = new List<string>();
            List<string> destinationPaths = new List<string>();
            List<DateTime> times = new List<DateTime>();
            List<string> notExistedFiles = new List<string>();
            foreach (var file in files.Cast<BackupFile>())
            {
                if (file.Entity.BackupFileName == null)
                {
                    notExistedFiles.Add(file.Entity.RawFileRelativePath);
                    continue;
                }

                string backupFile = Path.Combine(SelectedTask.BackupDir, file.Entity.BackupFileName);
                if (!File.Exists(backupFile))
                {
                    notExistedFiles.Add(file.Entity.RawFileRelativePath);
                    continue;
                }

                string fileRelativePath = dir.RelativePath == null
                    ? file.RelativePath
                    : Path.GetRelativePath(dir.RelativePath, file.RelativePath);
                string destinationPath = Path.Combine(rootDir, fileRelativePath);
                sourcePaths.Add(backupFile);
                destinationPaths.Add(destinationPath);
                times.Add(file.Time);
            }

            bool copy = true;
            if (notExistedFiles.Count > 0)
            {
                copy = (bool)await this.SendMessage(new CommonDialogMessage()
                {
                    Title = "部分文件不存在",
                    Message = "部分文件不存在实际备份文件，可能是虚拟备份或文件丢失。是否另存为其余文件？",
                    Detail = string.Join(Environment.NewLine, notExistedFiles),
                    Type = CommonDialogMessage.CommonDialogType.YesNo
                }).Task;
            }

            if (copy)
            {
                await dialog.CopyFilesAsync(sourcePaths, destinationPaths, times);
            }
            else
            {
                dialog.Close();
            }
        }
    }

    [RelayCommand]
    private async Task OrganizeFilesAsync()
    {
        (IList<FileInfo> RedundantFiles, IList<BackupFileEntity> LostFiles) issuedFiles = ([], []);
        await TryDoAsync("整理文件", async () =>
        {
            await using var db = new DbService(SelectedTask);
            issuedFiles = await db.CheckFilesAsync(default);
        });
        if (issuedFiles.RedundantFiles.Count + issuedFiles.LostFiles.Count == 0)
        {
            await this.SendMessage(new CommonDialogMessage()
            {
                Type = CommonDialogMessage.CommonDialogType.Ok,
                Message = "不存在多余或丢失的备份文件",
                Title = "检查文件"
            }).Task;
            return;
        }

        if (true.Equals(await this.SendMessage(new CommonDialogMessage()
            {
                Type = CommonDialogMessage.CommonDialogType.YesNo,
                Message =
                    $"存在{issuedFiles.RedundantFiles.Count}个多余文件；{Environment.NewLine}存在{issuedFiles.LostFiles.Count}个丢失的备份文件{Environment.NewLine}是否删除多余文件？",
                Detail = $"多余文件（在备份文件夹中存在但数据库中不存在的文件）：{Environment.NewLine}"
                         + string.Join(Environment.NewLine, issuedFiles.RedundantFiles.Select(p => p.Name))
                         + $"{Environment.NewLine}{Environment.NewLine}丢失文件（被数据库记录但无物理文件，会导致恢复失败）：{Environment.NewLine}"
                         + string.Join(Environment.NewLine, issuedFiles.LostFiles.Select(p => p.RawFileRelativePath)),
                Title = "检查文件"
            }).Task))
        {
            await TryDoAsync("删除多余文件", () =>
            {
                return Task.Run(() =>
                {
                    foreach (var file in issuedFiles.RedundantFiles)
                    {
                        FileDeleteHelper.DeleteByConfig(file.FullName);
                    }
                });
            });
        }
    }

    [RelayCommand]
    private async Task CopyAsync(object obj)
    {
        await this.SendMessage(new GetClipboardMessage()).Clipboard
            .SetTextAsync(obj is string str ? str : obj?.ToString() ?? "");
    }
}