using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Dialogs;
using ArchiveMaster.Messages;
using ArchiveMaster.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveMaster.Configs;
using System.Reflection;
using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Interactivity;
using ArchiveMaster.Platforms;
using FzLib;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Avalonia.Threading;
using Serilog;

namespace ArchiveMaster.Views;

public partial class MainView : UserControl
{
    private readonly AppConfig appConfig;
    private readonly IDialogService dialogService;
    private readonly IPermissionService permissionService;
    private CancellationTokenSource loadingToken = null;

    public MainView(MainViewModel viewModel,
        AppConfig appConfig,
        IDialogService dialogService,
        IViewPadding viewPadding = null,
        IPermissionService permissionService = null)
    {
        this.appConfig = appConfig;
        this.dialogService = dialogService;
        this.permissionService = permissionService;
        DataContext = viewModel;

        InitializeComponent();
        if (viewPadding != null)
        {
            Padding = new Thickness(0, viewPadding.GetTop(), 0, viewPadding.GetBottom());
        }
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        permissionService?.CheckPermissions();
        if (appConfig.LoadError != null)
        {
            await dialogService.ShowErrorDialogAsync("加载配置失败", appConfig.LoadError);
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (Bounds.Width <= 420)
        {
            Resources["BoxWidth"] = 160d;
            Resources["BoxHeight"] = 200d;
            Resources["ShowDescription"] = false;
        }
        else
        {
            Resources["BoxWidth"] = 200d;
            Resources["BoxHeight"] = 280d;
            Resources["ShowDescription"] = true;
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        WeakReferenceMessenger.Default.UnregisterAll(this);
        WeakReferenceMessenger.Default.Cleanup();
    }

    private void ToolItem_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            TopLevel.GetTopLevel(this).FocusManager.ClearFocus();
            (DataContext as MainViewModel).EnterToolCommand.Execute((sender as ToolItemBox).DataContext);
        }
    }
}