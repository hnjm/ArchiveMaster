using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ArchiveMaster.Configs;
using ArchiveMaster.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArchiveMaster.ViewModels.FileSystem;
using FzLib.Avalonia.Dialogs;

namespace ArchiveMaster.ViewModels;

public partial class TwinFileCleanerViewModel(AppConfig appConfig, IDialogService dialogService)
    : TwoStepViewModelBase<TwinFileCleanerService, TwinFileCleanerConfig>(appConfig, dialogService)
{
    [ObservableProperty]
    private List<TwinFileInfo> deletingFiles;

    protected override Task OnInitializedAsync()
    {
        DeletingFiles = Service.DeletingFiles;
        return base.OnInitializedAsync();
    }

    protected override void OnReset()
    {
        DeletingFiles = null;
    }
}