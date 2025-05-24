using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Messages;
using ArchiveMaster.Configs;
using ArchiveMaster.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ArchiveMaster.Enums;

namespace ArchiveMaster.ViewModels;

public partial class RenameViewModel(AppConfig appConfig)
    : TwoStepViewModelBase<RenameService, RenameConfig>(appConfig)
{
    [ObservableProperty]
    private ObservableCollection<FileSystem.RenameFileInfo> files;

    [ObservableProperty]
    private bool showMatchedOnly = true;

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private int matchedCount;

    private bool isWithdraw = false;

    protected override Task OnInitializingAsync()
    {
        isWithdraw = false;
        return base.OnInitializingAsync();
    }

    protected override Task OnInitializedAsync()
    {
        var matched = Service.Files.Where(p => p.IsMatched);
        Files = new ObservableCollection<FileSystem.RenameFileInfo>(ShowMatchedOnly ? matched : Service.Files);
        TotalCount = Service.Files.Count;
        MatchedCount = matched.Count();
        return base.OnInitializedAsync();
    }

    protected override async Task OnExecutedAsync(CancellationToken token)
    {
        if (isWithdraw)
        {
            return;
        }

        if (true.Equals(await this.SendMessage(new CommonDialogMessage
            {
                Type = CommonDialogMessage.CommonDialogType.YesNo,
                Title = "重命名完成，请检查结果",
                Message =
                    $"共重命名{Files.Count(p => p.IsCompleted)}项，失败{Files.Count(p => p.Status == ProcessStatus.Error)}项。{Environment.NewLine}" +
                    $"请检查结果，若不符合预期，可以进行撤销。是否撤销？"
            }).Task))
        {
            Config.Manual = true;
            Config.ManualMaps = string.Join(Environment.NewLine, Files
                .Where(p => p.IsCompleted)
                .Select(p => $"{p.GetNewPath()}\t{p.Name}"));
            await InitializeCommand.ExecuteAsync(null);
            isWithdraw = true;
        }
    }

    partial void OnShowMatchedOnlyChanged(bool value)
    {
        if (Service?.Files == null)
        {
            return;
        }

        Files = new ObservableCollection<FileSystem.RenameFileInfo>(value
            ? Service.Files.Where(p => p.IsMatched)
            : Service.Files);
    }

    protected override void OnReset()
    {
        Files = null;
        TotalCount = 0;
        MatchedCount = 0;
    }
}