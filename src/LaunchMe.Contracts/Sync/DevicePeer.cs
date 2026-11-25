namespace LaunchMe.Contracts.Sync;

public record DevicePeer(
    string DeviceId,
    string DeviceName,
    string Platform,
    bool IsPaired,
    DateTime LastSeen
);
