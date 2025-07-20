using System.ComponentModel;
using ArchiveMaster.Configs;
using ArchiveMaster.Services;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FzLib.Avalonia.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiveMaster.ViewModels;

public abstract partial class ViewModelBase(IDialogService dialogService) : ObservableObject
{
    public IDialogService DialogService { get; } = dialogService;
    
    public static ViewModelBase Current { get; private set; }
    
    [ObservableProperty]
    private bool isWorking = false;
    
    public event EventHandler RequestClosing;

    public void Exit()
    {
        RequestClosing?.Invoke(this, EventArgs.Empty);
    }

    public virtual void OnEnter()
    {
        Current = this;
    }

    public virtual Task OnExitAsync(CancelEventArgs args)
    {
        Current = null;
        return Task.CompletedTask;
    }
}