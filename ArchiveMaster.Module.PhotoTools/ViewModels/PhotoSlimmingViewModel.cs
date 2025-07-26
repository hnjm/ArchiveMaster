﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using FzLib.Cryptography;
using Mapster;
using ArchiveMaster.Configs;
using ArchiveMaster.Services;
using ArchiveMaster.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ArchiveMaster.ViewModels.FileSystem;
using FzLib.Avalonia.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiveMaster.ViewModels;
public partial class PhotoSlimmingViewModel(AppConfig appConfig,IDialogService dialogService)
    : TwoStepViewModelBase<PhotoSlimmingService, PhotoSlimmingConfig>(appConfig,dialogService)
{
    [ObservableProperty]
    private bool canCancel;

    [ObservableProperty]
    private SlimmingFilesInfo compressFiles;

    [ObservableProperty]
    private SlimmingFilesInfo copyFiles;

    [ObservableProperty]
    private SlimmingFilesInfo deleteFiles;

    [ObservableProperty]
    private ObservableCollection<string> errorMessages;

    protected override Task OnExecutedAsync(CancellationToken token)
    {
        ErrorMessages = new ObservableCollection<string>(Service.ErrorMessages);
        return base.OnExecutedAsync(token);
    }

    protected override async Task OnInitializedAsync()
    {
        Progress = -1;
        Message = "正在生成统计信息";
        Progress = Double.NaN;
        await Service.CopyFiles.CreateRelativePathsAsync();
        await Service.CompressFiles.CreateRelativePathsAsync();
        await Service.DeleteFiles.CreateRelativePathsAsync();
        CopyFiles = Service.CopyFiles;
        CompressFiles = Service.CompressFiles;
        DeleteFiles = Service.DeleteFiles;
        ErrorMessages = new ObservableCollection<string>(Service.ErrorMessages);
    }

    protected override Task OnInitializingAsync()
    {
        if (Config == null)
        {
            throw new ArgumentException("请先选择配置");
        }

        return base.OnInitializingAsync();
    }
    protected override void OnReset()
    {
        CopyFiles = null;
        CompressFiles = null;
        DeleteFiles = null;
        ErrorMessages = null;
    }
}