using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Storage;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.UI.ViewModels;

namespace LaunchMe.UI.Views;

public sealed partial class NotchWindow : Window
{
    private readonly AppWindow _appWindow;
    private readonly OverlappedPresenter _presenter;
    private readonly nint _hwnd;
    private readonly SnootSpringController _springController = new();
    private IReadOnlyList<string> _pendingShareFiles = [];

    public NotchViewModel ViewModel { get; }
    public SnootSpringController SpringController => _springController;

    public NotchWindow(NotchViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();

        _springController.Attach(NotchContainer);
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.NearbyShareRequested += OnNearbyShareRequested;

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

        Closed += (_, _) =>
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            ViewModel.NearbyShareRequested -= OnNearbyShareRequested;
            _springController.Detach();
            ViewModel.Dispose();
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NotchViewModel.IsExpanded))
        {
            if (ViewModel.IsExpanded)
            {
                _springController.AnimateExpand();
            }
            else
            {
                _springController.AnimateCollapse();
            }

            PositionTopCenter();
        }
        else if (e.PropertyName is nameof(NotchViewModel.WidthPx) or nameof(NotchViewModel.HeightPx))
        {
            PositionTopCenter();
        }
        else if (e.PropertyName == nameof(NotchViewModel.IsHiddenByFullscreenGame))
        {
            if (ViewModel.IsHiddenByFullscreenGame)
            {
                _springController.StopAnimation();
                _appWindow.Hide();
            }
            else
            {
                _appWindow.Show();
                PositionTopCenter();
            }
        }
    }

    private void NotchContainer_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            ViewModel.BeginFileDragOver();
        }
    }

    private void NotchContainer_DragLeave(object sender, DragEventArgs e)
    {
        ViewModel.EndFileDragOver();
    }

    private async void NotchContainer_Drop(object sender, DragEventArgs e)
    {
        var paths = await ExtractDroppedFilePathsAsync(e);
        if (paths.Count > 0)
        {
            ViewModel.DropFilesToPocket(paths);
        }
        else
        {
            ViewModel.EndFileDragOver();
        }
    }

    private async void PocketDropZone_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        var paths = await ExtractDroppedFilePathsAsync(e);
        ViewModel.DropFilesToPocket(paths);
    }

    private async void NearbyShareDropZone_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        var paths = await ExtractDroppedFilePathsAsync(e);
        ViewModel.DropFilesToNearbyShare(paths);
    }

    private static async Task<IReadOnlyList<string>> ExtractDroppedFilePathsAsync(DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return [];
        }

        var items = await e.DataView.GetStorageItemsAsync();
        return items.OfType<StorageFile>().Select(f => f.Path).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
    }

    private void NearbyShareButton_Click(object sender, RoutedEventArgs e)
    {
        var files = ViewModel.PocketItems.Select(p => p.FilePath).ToList();
        ViewModel.DropFilesToNearbyShare(files);
    }

    private void OnNearbyShareRequested(object? sender, IReadOnlyList<string> filePaths)
    {
        _pendingShareFiles = filePaths;
        try
        {
            var dtm = DataTransferManagerInterop.GetForWindow(_hwnd);
            dtm.DataRequested -= OnShareDataRequested;
            dtm.DataRequested += OnShareDataRequested;
            DataTransferManagerInterop.ShowShareUIForWindow(_hwnd);
        }
        catch
        {
            // Headless or unsupported environments ignore native Share UI invocation
        }
    }

    private async void OnShareDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
    {
        var deferral = args.Request.GetDeferral();
        try
        {
            args.Request.Data.Properties.Title = "LaunchMe Nearby Share";
            var storageItems = new List<IStorageItem>();
            foreach (var path in _pendingShareFiles)
            {
                if (File.Exists(path))
                {
                    storageItems.Add(await StorageFile.GetFileFromPathAsync(path));
                }
            }

            if (storageItems.Count > 0)
            {
                args.Request.Data.SetStorageItems(storageItems);
            }
            else
            {
                args.Request.Data.SetText("Shared via LaunchMe Snoot HUD");
            }
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void ClipboardCategoryChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && Enum.TryParse<ClipboardCategory>(tag, out var cat))
        {
            ViewModel.SelectClipboardCategory(cat == ClipboardCategory.Recent ? null : cat);
        }
    }

    private void ClipboardFavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id)
        {
            ViewModel.ToggleFavoriteClipboardEntry(id);
        }
    }

    private void ClipboardCopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ClipboardEntry entry)
        {
            ViewModel.CopyClipboardEntry(entry);
        }
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
