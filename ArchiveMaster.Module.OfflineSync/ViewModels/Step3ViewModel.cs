using CommunityToolkit.Mvvm.ComponentModel;
using FzLib;
using ArchiveMaster.Views;
using System.Collections;
using System.Collections.ObjectModel;
using ArchiveMaster.Configs;
using ArchiveMaster.Enums;
using ArchiveMaster.Services;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using FzLib.Avalonia.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiveMaster.ViewModels
{
    public partial class Step3ViewModel(AppConfig appConfig)
        : OfflineSyncViewModelBase<Step3Service, OfflineSyncStep3Config, FileSystem.SyncFileInfo>(appConfig)
    {
        public IEnumerable DeleteModes => Enum.GetValues<DeleteMode>();

        protected override Task OnInitializedAsync()
        {
            Files = new ObservableCollection<FileSystem.SyncFileInfo>(Service.UpdateFiles);
            return base.OnInitializedAsync();
        }

        protected override async Task OnExecutedAsync(CancellationToken token)
        {
            if (Service.DeletingDirectories.Count != 0)
            {
                var result = await this.SendMessage(new CommonDialogMessage()
                {
                    Title = "删除空目录",
                    Message = $"有{Service.DeletingDirectories.Count}个已不存在于本地的空目录，是否删除？",
                    Detail = string.Join(Environment.NewLine,
                        Service.DeletingDirectories.Select(p => p.Path)),
                    Type = CommonDialogMessage.CommonDialogType.YesNo
                }).Task;
                if (result.Equals(true))
                {
                    await Service.DeleteEmptyDirectoriesAsync();
                }
            }
        }
    }
}