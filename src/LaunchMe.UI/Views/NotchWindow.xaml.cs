using System.Runtime.InteropServices;
using Windows.Graphics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using LaunchMe.UI.ViewModels;

namespace LaunchMe.UI.Views;

public sealed partial class NotchWindow : Window
{
    private readonly AppWindow _appWindow;
    private readonly OverlappedPresenter _presenter;
    private readonly nint _hwnd;

    public NotchViewModel ViewModel { get; }

    public NotchWindow(NotchViewModel viewModel)
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

        ApplyToolWindowStyle();
        PositionTopCenter();

        // Screen Capture Exclusion
        ViewModel.ApplyScreenCaptureExclusion(_hwnd);

        Closed += (_, _) => ViewModel.Dispose();
    }

    private void ApplyToolWindowStyle()
    {
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        int exStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
        SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
    }

    private void PositionTopCenter()
    {
        int screenWidth = GetSystemMetrics(0);
        int width = (int)ViewModel.WidthPx;
        int height = (int)ViewModel.HeightPx;
        int x = (screenWidth - width) / 2;
        int y = 0;

        _appWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}
