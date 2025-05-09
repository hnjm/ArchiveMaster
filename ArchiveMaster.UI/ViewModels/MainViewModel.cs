using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ArchiveMaster.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static ArchiveMaster.ViewModels.MainViewModel;
using ArchiveMaster.Configs;
using ArchiveMaster.Models;
using ArchiveMaster.Platforms;
using ArchiveMaster.Services;
using Avalonia;
using Avalonia.Input;
using FzLib.Program.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiveMaster.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IStartupManager startupManager;

    [ObservableProperty]
    private bool isAutoStart;

    [ObservableProperty]
    private bool isToolOpened;

    [ObservableProperty]
    private object mainContent;
    
    [ObservableProperty]
    private bool scrollViewBringIntoViewOnFocusChange;

    [ObservableProperty]
    private ObservableCollection<ToolPanelGroupInfo> panelGroups = new ObservableCollection<ToolPanelGroupInfo>();

    public MainViewModel(AppConfig appConfig, IStartupManager startupManager = null,
        IBackCommandService backCommandService = null)
    {
        this.startupManager = startupManager;
        foreach (var view in Initializer.Views)
        {
            PanelGroups.Add(view);
        }

        backCommandService?.RegisterBackCommand(() =>
        {
            if (mainContent is PanelBase && IsToolOpened)
            {
                IsToolOpened = false;
                return true;
            }

            return false;
        });
        BackCommandService = backCommandService;

        IsAutoStart = startupManager?.IsStartupEnabled() ?? false;
    }

    public IBackCommandService BackCommandService { get; }

    [RelayCommand]
    private void ScrollViewKeyDown()
    {
        //按Tab时，需要按钮自动进入视野；平常的话，会导致鼠标多点一下
        ScrollViewBringIntoViewOnFocusChange = true;
    }

    [RelayCommand]
    private void EnterTool(ToolPanelInfo panelInfo)
    {
        if (panelInfo.PanelInstance == null)
        {
            panelInfo.PanelInstance = HostServices.GetService(panelInfo.ViewType) as PanelBase ??
                                      throw new Exception($"无法找到{panelInfo.ViewType}服务");
            if (panelInfo.PanelInstance.DataContext is ViewModelBase vm)
            {
                vm.RequestClosing += async (s, e) =>
                {
                    CancelEventArgs args = new CancelEventArgs();
                    if ((s as StyledElement)?.DataContext is ViewModelBase vm)
                    {
                        await vm.OnExitAsync(args);
                    }

                    if (!args.Cancel)
                    {
                        IsToolOpened = false;
                    }
                };
            }

            panelInfo.PanelInstance.Title = panelInfo.Title;
            panelInfo.PanelInstance.Description = panelInfo.Description;
        }

        (panelInfo.PanelInstance.DataContext as ViewModelBase)?.OnEnter();
        MainContent = panelInfo.PanelInstance;
        IsToolOpened = true;
    }

    [RelayCommand]
    private void SetAutoStart(bool autoStart)
    {
        if (startupManager == null)
        {
            return;
        }
        if (autoStart)
        {
            startupManager.EnableStartup("s");
        }
        else
        {
            startupManager.DisableStartup();
        }
    }
}