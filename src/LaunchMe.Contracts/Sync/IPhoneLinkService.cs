using LaunchMe.Contracts.Agent;

namespace LaunchMe.Contracts.Sync;

public interface IPhoneLinkService
{
    event EventHandler<DevicePeer> PeerDiscovered;
    event EventHandler<DevicePeer> PeerConnected;
    event EventHandler<DevicePeer> PeerDisconnected;

    Task<string> StartPairingSessionAsync(string deviceName);
    Task<bool> CompletePairingAsync(string pin, string deviceId, string deviceName, string platform);
    Task BroadcastNotificationAsync(string title, string body);
    Task BroadcastApprovalRequestAsync(AgentApprovalRequest request);
    IReadOnlyList<DevicePeer> GetPairedDevices();
    void UnpairDevice(string deviceId);
}
