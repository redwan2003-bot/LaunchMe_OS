using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using LaunchMe.Contracts.Workflows;
using LaunchMe.UI.ViewModels;

namespace LaunchMe.UI.Views;

public sealed partial class LauncherWindow : Window
{
    private readonly AppWindow _appWindow;
    private readonly OverlappedPresenter _presenter;
    private readonly nint _hwnd;

    public LauncherViewModel ViewModel { get; }
    public bool IsLauncherVisible { get; private set; }

    public LauncherWindow(LauncherViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();

        try
        {
            SystemBackdrop = new DesktopAcrylicBackdrop();
        }
        catch
        {
            // DesktopAcrylicBackdrop may be unsupported on downlevel Windows builds
        }

        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        _presenter = OverlappedPresenter.Create();
        _presenter.IsResizable = false;
        _presenter.IsMaximizable = false;
        _presenter.IsMinimizable = false;
        _presenter.IsAlwaysOnTop = true;
        _presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
        _appWindow.SetPresenter(_presenter);

        ApplyToolWindowStyle();
        PositionOnCursorMonitor();

        ViewModel.DismissRequested += (_, _) => Dismiss();
        Closed += (_, _) =>
        {
            LiquidGlassBackdrop.Detach();
            WallpaperSurface.Dispose();
        };
    }

    private void ApplyToolWindowStyle()
    {
        const int GWL_EXSTYLE = -20;
        const nint WS_EX_TOOLWINDOW = 0x00000080;
        const nint WS_EX_APPWINDOW = 0x00040000;

        nint exStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE);
        exStyle = (exStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW;
        SetWindowLongPtr(_hwnd, GWL_EXSTYLE, exStyle);
    }

    public void ShowLauncher()
    {
        PositionOnCursorMonitor();
        _appWindow.Show();
        IsLauncherVisible = true;
        LiquidGlassBackdrop.Attach();
        ViewModel.OnShown();
        SearchBox.Focus(FocusState.Programmatic);
    }

    public void Dismiss()
    {
        // Enforce Pause-on-Hide GPU Law: Immediately stop timers, render loops, compositor effects, and video decoding
        IsLauncherVisible = false;
        ViewModel.OnHidden();
        LiquidGlassBackdrop.Detach();
        _appWindow.Hide();
    }

    private void PositionOnCursorMonitor()
    {
        if (!GetCursorPos(out POINT cursorPt)) return;

        nint hMonitor = MonitorFromPoint(cursorPt, MONITOR_DEFAULTTONEAREST);
        if (hMonitor == IntPtr.Zero) return;

        MONITORINFO mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        if (GetMonitorInfo(hMonitor, ref mi))
        {
            int width = mi.rcMonitor.Right - mi.rcMonitor.Left;
            int height = mi.rcMonitor.Bottom - mi.rcMonitor.Top;
            _appWindow.MoveAndResize(new RectInt32(mi.rcMonitor.Left, mi.rcMonitor.Top, width, height));
        }
    }

    private void DismissBackdrop_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        Dismiss();
    }

    private void FolderScrim_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.CloseFolder();
        e.Handled = true;
    }

    private void SettingsScrim_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.IsSettingsOpen = false;
        e.Handled = true;
    }

    private void ModalCard_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = true;
    }

    private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            if (ViewModel.IsFolderOpen)
            {
                ViewModel.CloseFolder();
            }
            else if (ViewModel.IsSettingsOpen)
            {
                ViewModel.IsSettingsOpen = false;
            }
            else
            {
                Dismiss();
            }

            e.Handled = true;
        }
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Tab)
        {
            ViewModel.ToggleEmojiMode();
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Down)
        {
            ViewModel.NavigateSearchSelection(1);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Up)
        {
            ViewModel.NavigateSearchSelection(-1);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Enter)
        {
            ViewModel.ExecuteSelectedSearchResult();
            e.Handled = true;
        }
    }

    private void SearchResultsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is UnifiedSearchResultViewModel result)
        {
            ViewModel.ExecuteSearchResult(result);
        }
    }

    private void LauncherGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is LauncherItemViewModel item)
        {
            ViewModel.ExecuteItem(item);
        }
    }

    private void FolderGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is LauncherItemViewModel item)
        {
            ViewModel.ExecuteItem(item);
        }
    }

    private void QuickTintButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string tintHex)
        {
            ViewModel.ApplyQuickThemeTint(tintHex);
        }
    }

    private Microsoft.UI.Xaml.Controls.ItemsWrapGrid? _folderWrapGrid;

    private void FolderWrapGrid_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.Controls.ItemsWrapGrid grid)
        {
            _folderWrapGrid = grid;
            grid.MaximumRowsOrColumns = ViewModel.ActiveFolderColumns;
        }
    }

    private void FolderDensityButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string preset)
        {
            ViewModel.SetActiveFolderGridDensity(preset);
            if (_folderWrapGrid != null)
            {
                _folderWrapGrid.MaximumRowsOrColumns = ViewModel.ActiveFolderColumns;
            }
        }
    }

    private void CategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string category)
        {
            ViewModel.SelectedCategory = category;
        }
    }

    private void SpaceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string spaceId)
        {
            _ = ViewModel.SwitchSpaceAsync(spaceId);
        }
    }

    private void WorkflowButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is WorkflowDefinition workflow)
        {
            _ = ViewModel.ExecuteWorkflowAsync(workflow);
        }
    }

    private void PageDot_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is PageIndicatorViewModel dot)
        {
            ViewModel.CurrentPageIndex = dot.PageIndex;
        }
    }

    private void Item_DragStarting(UIElement sender, DragStartingEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is LauncherItemViewModel item)
        {
            e.Data.SetText(item.Id);
            ViewModel.StartDrag(item);
        }
    }

    private void Item_Drop(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is LauncherItemViewModel target)
        {
            ViewModel.HandleDropOnItem(target);
        }
    }

    #region Win32 P/Invoke
    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);
    #endregion
}
