using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.Contracts.Sync;
using LaunchMe.UI.ViewModels;

namespace LaunchMe.UI.Sync;

/// <summary>
/// ViewModel coordinating cross-device pairing and device management.
/// </summary>
public class PhoneLinkViewModel : INotifyPropertyChanged
{
    private readonly IPhoneLinkService _phoneLink;
    private string _currentPairingPin = string.Empty;
    private bool _isPairingActive;

    public ObservableCollection<DevicePeer> PairedDevices { get; } = [];

    public PhoneLinkViewModel(IPhoneLinkService phoneLink)
    {
        _phoneLink = phoneLink;
        _phoneLink.PeerConnected += (_, peer) =>
        {
            PairedDevices.Add(peer);
            IsPairingActive = false;
        };

        _phoneLink.PeerDisconnected += (_, peer) =>
        {
            var existing = PairedDevices.FirstOrDefault(p => p.DeviceId == peer.DeviceId);
            if (existing != null)
            {
                PairedDevices.Remove(existing);
            }
        };

        RefreshPairedDevices();

        StartPairingCommand = new RelayCommand(async () =>
        {
            CurrentPairingPin = await _phoneLink.StartPairingSessionAsync(Environment.MachineName);
            IsPairingActive = true;
        });

        UnpairCommand = new RelayCommand<string>(deviceId =>
        {
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                _phoneLink.UnpairDevice(deviceId);
            }
        });
    }

    public string CurrentPairingPin
    {
        get => _currentPairingPin;
        private set
        {
            if (_currentPairingPin != value)
            {
                _currentPairingPin = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsPairingActive
    {
        get => _isPairingActive;
        private set
        {
            if (_isPairingActive != value)
            {
                _isPairingActive = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand StartPairingCommand { get; }
    public ICommand UnpairCommand { get; }

    private void RefreshPairedDevices()
    {
        PairedDevices.Clear();
        foreach (var device in _phoneLink.GetPairedDevices())
        {
            PairedDevices.Add(device);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
