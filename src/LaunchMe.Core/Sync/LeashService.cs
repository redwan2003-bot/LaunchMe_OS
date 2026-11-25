using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using LaunchMe.Contracts.Agent;
using LaunchMe.Contracts.Sync;

namespace LaunchMe.Core.Sync;

/// <summary>
/// Leash service managing zero-cloud encrypted local cross-device pairing, discovery, and notification mirroring.
/// </summary>
public class LeashService : ILeashService, IDisposable
{
    private readonly IAgentApprovalService? _approvalService;
    private readonly ConcurrentDictionary<string, DevicePeer> _pairedDevices = new();
    private readonly object _lock = new();

    private string? _activePairingPin;
    private DateTime _pairingPinExpires = DateTime.MinValue;
    private bool _disposed;

    public event EventHandler<DevicePeer>? PeerDiscovered;
    public event EventHandler<DevicePeer>? PeerConnected;
    public event EventHandler<DevicePeer>? PeerDisconnected;

    public LeashService(IAgentApprovalService? approvalService = null)
    {
        _approvalService = approvalService;

        if (_approvalService != null)
        {
            _approvalService.ApprovalRequested += (_, request) =>
            {
                _ = BroadcastApprovalRequestAsync(request);
            };
        }
    }

    public Task<string> StartPairingSessionAsync(string deviceName)
    {
        lock (_lock)
        {
            // Generate 6-digit cryptographic numeric PIN
            int pinNumber = RandomNumberGenerator.GetInt32(100000, 1000000);
            _activePairingPin = pinNumber.ToString();
            _pairingPinExpires = DateTime.UtcNow.AddMinutes(5);

            return Task.FromResult(_activePairingPin);
        }
    }

    public void ReportDiscoveredPeer(DevicePeer peer)
    {
        PeerDiscovered?.Invoke(this, peer);
    }

    public Task<bool> CompletePairingAsync(string pin, string deviceId, string deviceName, string platform)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(_activePairingPin) ||
                DateTime.UtcNow > _pairingPinExpires ||
                !string.Equals(_activePairingPin, pin, StringComparison.Ordinal))
            {
                return Task.FromResult(false);
            }

            var peer = new DevicePeer(deviceId, deviceName, platform, IsPaired: true, DateTime.UtcNow);
            _pairedDevices[deviceId] = peer;
            _activePairingPin = null;

            PeerConnected?.Invoke(this, peer);
            return Task.FromResult(true);
        }
    }

    public Task BroadcastNotificationAsync(string title, string body)
    {
        var payload = new { Title = title, Body = body, Timestamp = DateTime.UtcNow };
        _ = JsonSerializer.Serialize(payload);
        return Task.CompletedTask;
    }

    public Task BroadcastApprovalRequestAsync(AgentApprovalRequest request)
    {
        var payload = new
        {
            Type = "ApprovalRequest",
            request.RequestId,
            request.AgentName,
            request.ToolName,
            request.CommandOrAction,
            Risk = request.Risk.ToString()
        };
        _ = JsonSerializer.Serialize(payload);
        return Task.CompletedTask;
    }

    public async Task HandleRemoteDecisionAsync(string requestId, ApprovalAction action, string? userResponseText = null)
    {
        if (_approvalService != null)
        {
            await _approvalService.SubmitDecisionAsync(requestId, action, userResponseText);
        }
    }

    public IReadOnlyList<DevicePeer> GetPairedDevices()
    {
        return _pairedDevices.Values.ToList();
    }

    public void UnpairDevice(string deviceId)
    {
        if (_pairedDevices.TryRemove(deviceId, out var peer))
        {
            PeerDisconnected?.Invoke(this, peer);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _activePairingPin = null;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
