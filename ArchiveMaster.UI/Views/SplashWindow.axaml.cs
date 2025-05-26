using System;
using System.Threading;
using ArchiveMaster.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Controls;

namespace ArchiveMaster.Views;

public partial class SplashWindow : Window
{
    private static SplashWindow splashWindow;

    private SplashWindow()
    {
        InitializeComponent();
    }

    public static void CreateAndShow()
    {
        if (splashWindow != null)
        {
            throw new Exception("Splash Window不可重复创建");
        }

        splashWindow = new SplashWindow();
        splashWindow.Show();
    }

    public static void CloseCurrent()
    {
        if (splashWindow == null)
        {
            return;
            //throw new Exception("还未创建Splash Window");
        }

        try
        {
            splashWindow.Close();
        }
        catch
        {
        }

        splashWindow = null;
    }
}