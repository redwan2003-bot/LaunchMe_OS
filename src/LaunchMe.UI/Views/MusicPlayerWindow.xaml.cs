using Windows.Graphics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using LaunchMe.UI.ViewModels;

namespace LaunchMe.UI.Views;

public sealed partial class MusicPlayerWindow : Window
{
    private readonly AppWindow _appWindow;
    private readonly OverlappedPresenter _presenter;
    private readonly nint _hwnd;

    public MusicPlayerViewModel ViewModel { get; }

    public MusicPlayerWindow(MusicPlayerViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();

        try
        {
            SystemBackdrop = new DesktopAcrylicBackdrop();
        }
        catch
        {
        }

        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        _presenter = OverlappedPresenter.Create();
        _presenter.IsResizable = false;
        _presenter.IsMaximizable = false;
        _presenter.IsMinimizable = false;
        _presenter.IsAlwaysOnTop = true;
        _presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
        _appWindow.SetPresenter(_presenter);

        MaximizeToFullscreen();

        Activated += (_, args) =>
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                ViewModel.OnHidden();
            }
            else
            {
                ViewModel.OnShown();
            }
        };
        ViewModel.DismissRequested += (_, _) => Close();
        Closed += (_, _) => ViewModel.Dispose();
    }

    private void MaximizeToFullscreen()
    {
        int screenWidth = GetSystemMetrics(0);
        int screenHeight = GetSystemMetrics(1);
        _appWindow.MoveAndResize(new RectInt32(0, 0, screenWidth, screenHeight));
    }

    private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}
