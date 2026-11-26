using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace LaunchMe.UI.Services;

/// <summary>
/// Watches for newly connected Bluetooth devices via WinRT DeviceWatcher and raises
/// <see cref="DeviceConnected"/> so Snoot HUD can display quick-connect toast banners.
/// Degrades gracefully in headless or VM environments without a Bluetooth radio.
/// </summary>
public sealed class BluetoothWatcherService : IDisposable
{
    private DeviceWatcher? _watcher;
    private bool _disposed;

    public event EventHandler<string>? DeviceConnected;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        if (_disposed || IsRunning) return;
        try
        {
            string selector = BluetoothDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected);
            _watcher = DeviceInformation.CreateWatcher(selector);
            _watcher.Added += OnDeviceAdded;
            _watcher.Start();
            IsRunning = true;
        }
        catch
        {
            IsRunning = false;
        }
    }

    private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation args)
    {
        if (!string.IsNullOrWhiteSpace(args.Name))
        {
            DeviceConnected?.Invoke(this, args.Name);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        IsRunning = false;
        if (_watcher != null)
        {
            _watcher.Added -= OnDeviceAdded;
            try
            {
                if (_watcher.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted)
                {
                    _watcher.Stop();
                }
            }
            catch
            {
            }
            _watcher = null;
        }
    }
}
