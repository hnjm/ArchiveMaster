using ArchiveMaster.Configs;
using ArchiveMaster.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using FzLib.Avalonia.Dialogs;
using FzLib.Avalonia.Services;

namespace ArchiveMaster.ViewModels
{
    public partial class BackupManageCenterViewModel : ViewModelBase
    {
        public IClipboardService Clipboard { get; }
        public IStorageProviderService Storage { get; }
        private readonly BackupService backupService;

        private AppConfig appConfig;

        [ObservableProperty]
        private int selectedTabIndex;

        public BackupManageCenterViewModel(AppConfig appConfig, IDialogService dialogService,
            IClipboardService clipboard, IStorageProviderService storage, BackupService backupService)
            : base(dialogService)
        {
            Clipboard = clipboard;
            Storage = storage;
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
    }
}