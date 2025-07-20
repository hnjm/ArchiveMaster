using ArchiveMaster.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using FzLib;
using ArchiveMaster.Views;
using System.Collections;
using System.Collections.ObjectModel;
using ArchiveMaster.Enums;
using ArchiveMaster.Configs;
using ArchiveMaster.Services;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using FzLib.Avalonia.Messages;
using Microsoft.Extensions.DependencyInjection;
using LocalAndOffsiteDir = ArchiveMaster.ViewModels.FileSystem.LocalAndOffsiteDir;

namespace ArchiveMaster.ViewModels
{
    public partial class Step2ViewModel(AppConfig appConfig)
        : OfflineSyncViewModelBase<Step2Service, OfflineSyncStep2Config, FileSystem.SyncFileInfo>(appConfig)
    {
        [RelayCommand]
        private async Task BrowseLocalDirAsync()
        {
            var provider = this.SendMessage(new GetStorageProviderMessage()).StorageProvider;
            var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                AllowMultiple = true,
            });

            if (folders.Count > 0)
            {
                string path = string.Join(Environment.NewLine, folders.Select((p => p.TryGetLocalPath())));
                if (string.IsNullOrEmpty(Config.LocalDir))
                {
                    Config.LocalDir = path;
                }
                else
                {
                    Config.LocalDir += Environment.NewLine + path;
                }
            }
        }

        [RelayCommand]
        private async Task BrowseOffsiteSnapshotAsync()
        {
            var provider = this.SendMessage(new GetStorageProviderMessage()).StorageProvider;
            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                FileTypeFilter =
                [
                    new FilePickerFileType("异地备份快照") { Patterns = ["*.os1"] }
                ]
            });

            if (files.Count > 0)
            {
                Config.OffsiteSnapshot = files[0].TryGetLocalPath();
            }
        }

        [RelayCommand]
        private async Task BrowsePatchDirAsync()
        {
            var provider = this.SendMessage(new GetStorageProviderMessage()).StorageProvider;
            var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                AllowMultiple = false,
            });

            if (folders.Count > 0)
            {
                Config.PatchDir = folders[0].TryGetLocalPath();
            }
        }

        protected override Task OnExecutingAsync(CancellationToken token)
        {
            if (Files.Count == 0)
            {
                throw new Exception("本地和异地没有差异");
            }

            return base.OnExecutingAsync(token);
        }

        protected override async Task OnExecutedAsync(CancellationToken token)
        {
            if (Files.Any(p => p.Status == ProcessStatus.Error))
            {
                await DialogService.ShowErrorDialogAsync("导出失败", "导出完成，但部分文件出现错误");
            }
        }

        private static readonly char[] LocalDirSplitter = ['|', '\r', '\n'];

        [RelayCommand]
        private async Task MatchDirsAsync()
        {
            try
            {
                await MatchDirectoriesAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorDialogAsync("匹配失败", ex);
            }
        }

        private async Task MatchDirectoriesAsync()
        {
            Config.Check();
            string[] localSearchingDirs =
                Config.LocalDir.Split(LocalDirSplitter, StringSplitOptions.RemoveEmptyEntries);
            Config.MatchingDirs =
                new ObservableCollection<LocalAndOffsiteDir>(await Step2Service
                    .MatchLocalAndOffsiteDirsAsync(Config.OffsiteSnapshot, localSearchingDirs));
        }

        protected override async Task OnInitializingAsync()
        {
            if (Config.MatchingDirs is null or { Count: 0 })
            {
                await MatchDirectoriesAsync();
            }
        }

        protected override Task OnInitializedAsync()
        {
            Files = new ObservableCollection<FileSystem.SyncFileInfo>(Service.UpdateFiles);
            return base.OnInitializedAsync();
        }
    }
}