using System.Numerics;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using LaunchMe.Platform.Win32.Shell;
using LaunchMe.UI.ViewModels;

namespace LaunchMe.UI.Views;

public sealed partial class DockWindow : Window
{
    private readonly AppWindow _appWindow;
    private readonly OverlappedPresenter _presenter;
    private readonly nint _hwnd;

    public DockViewModel ViewModel { get; }

    public DockWindow(DockViewModel viewModel)
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
        Win32WindowEdgeHelper.ApplyBorderlessPillDwmChrome(_hwnd);
        PositionBottomCenter();

        Closed += (_, _) => ViewModel.Dispose();
    }

    /// <summary>
    /// Applies a magnetic spring hover scale animation to a dock item visual.
    /// Safely no-ops in headless unit test environments without a WinUI Compositor.
    /// </summary>
    public static void ApplyDockItemHoverSpring(UIElement? element, bool isHovered)
    {
        if (element == null)
        {
            return;
        }

        Vector3 targetScale = isHovered ? new Vector3(1.18f, 1.18f, 1.0f) : Vector3.One;
        Vector3 initialScale = isHovered ? Vector3.One : new Vector3(1.18f, 1.18f, 1.0f);
        float dampingRatio = isHovered ? 0.72f : 0.85f;
        int periodMs = isHovered ? 160 : 180;

        SnootSpringController.StartScaleSpring(element, initialScale, targetScale, dampingRatio, periodMs);
    }

    private void DockItem_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is UIElement element)
        {
            ApplyDockItemHoverSpring(element, isHovered: true);
        }
    }

    private void DockItem_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is UIElement element)
        {
            ApplyDockItemHoverSpring(element, isHovered: false);
        }
    }

    private void DockItems_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Microsoft.UI.Xaml.Controls.ItemsControl itemsControl) return;
        int count = itemsControl.Items.Count;
        if (count == 0) return;

        double pointerX = e.GetCurrentPoint(itemsControl).Position.X;
        double spacing = 44.0;
        double[] scales = DockViewModel.ComputeGaussianWaveScales(count, pointerX, spacing);

        for (int i = 0; i < count; i++)
        {
            if (itemsControl.ContainerFromIndex(i) is UIElement container)
            {
                float s = (float)scales[i];
                SnootSpringController.StartScaleSpring(container, new Vector3(1f), new Vector3(s, s, 1f), 0.72f, 140);
            }
        }
    }

    private void DockItems_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Microsoft.UI.Xaml.Controls.ItemsControl itemsControl) return;
        int count = itemsControl.Items.Count;
        for (int i = 0; i < count; i++)
        {
            if (itemsControl.ContainerFromIndex(i) is UIElement container)
            {
                SnootSpringController.StartScaleSpring(container, new Vector3(1.18f, 1.18f, 1f), Vector3.One, 0.85f, 180);
            }
        }
    }

    private void ToolBarButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.Controls.Button btn && btn.Tag is DockToolBarAction action)
        {
            ViewModel.ExecuteToolBarAction(action);
        }
    }

    private void ApplyToolWindowStyle()
    {
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        int exStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
        SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
    }

    private void PositionBottomCenter()
    {
        var mon = Win32WindowEdgeHelper.GetActiveMonitorBounds();
        int width = Math.Min(1280, mon.Width - 40);
        int height = ViewModel.DockHeight + 16;
        int x = mon.Left + ((mon.Width - width) / 2);
        int y = mon.Bottom - height;

        _appWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);
}
