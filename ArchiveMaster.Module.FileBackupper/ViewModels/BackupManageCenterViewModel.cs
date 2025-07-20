using ArchiveMaster.Configs;
using ArchiveMaster.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using ArchiveMaster.Basic;
using ArchiveMaster.Converters;
using ArchiveMaster.Enums;
using ArchiveMaster.Messages;
using ArchiveMaster.Models;
using ArchiveMaster.ViewModels.FileSystem;
using ArchiveMaster.Views;
using Avalonia.Platform.Storage;
using FzLib.Avalonia.Messages;
using Microsoft.Extensions.Logging;

namespace ArchiveMaster.ViewModels
{
    public partial class BackupManageCenterViewModel : ViewModelBase
    {
        private readonly BackupService backupService;

        private AppConfig appConfig;

        [ObservableProperty]
        private int selectedTabIndex;

        public BackupManageCenterViewModel(AppConfig appConfig, BackupService backupService)
        {
            Config = appConfig.GetOrCreateConfigWithDefaultKey<FileBackupperConfig>();
            this.appConfig = appConfig;
            this.backupService = backupService;
            BackupService.NewLog += (s, e) =>
            {
                if (e.Task == SelectedTask)
                {
                    LastLog = e.Log;
                }
            };
        }

        public FileBackupperConfig Config { get; }

        public override async void OnEnter()
        {
            base.OnEnter();
            await LoadTasksAsync();
        }

        private void ThrowIfIsBackingUp()
        {
            if (backupService.IsBackingUp)
            {
                throw new InvalidOperationException("有任务正在备份，无法进行操作");
            }
        }

        private async Task<bool> TryDoAsync(string workName, Func<Task> task)
        {
            this.SendMessage(new LoadingMessage(true));
            await Task.Delay(100);
            try
            {
                await task();
                this.SendMessage(new LoadingMessage(false));
                return true;
            }
            catch (Exception ex)
            {
                this.SendMessage(new LoadingMessage(false));
                await DialogService.ShowErrorDialogAsync($"{workName}失败", ex);
                return false;
            }
        }
    }
}