using FluentAssertions;
using LaunchMe.Contracts.Sync;
using LaunchMe.Core.Sync;
using Xunit;

namespace LaunchMe.Core.Tests.Sync;

public class PhoneLinkServiceTests
{
    [Fact]
    public async Task StartPairingSession_Generates6DigitNumericPin()
    {
        using var service = new PhoneLinkService();

        string pin = await service.StartPairingSessionAsync("Pixel 9 Pro");

        pin.Should().HaveLength(6);
        int.TryParse(pin, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CompletePairing_WithMatchingPin_RegistersPairedDevice()
    {
        using var service = new PhoneLinkService();
        DevicePeer? connectedPeer = null;
        service.PeerConnected += (_, p) => connectedPeer = p;

        string pin = await service.StartPairingSessionAsync("iPhone 16");
        bool success = await service.CompletePairingAsync(pin, "device-uuid-1", "iPhone 16", "iOS");

        success.Should().BeTrue();
        connectedPeer.Should().NotBeNull();
        connectedPeer!.DeviceId.Should().Be("device-uuid-1");
        service.GetPairedDevices().Should().ContainSingle(p => p.DeviceId == "device-uuid-1");
    }

    [Fact]
    public async Task CompletePairing_WithWrongPin_Fails()
    {
        using var service = new PhoneLinkService();

        _ = await service.StartPairingSessionAsync("Pixel 9");
        bool success = await service.CompletePairingAsync("000000", "device-uuid-2", "Pixel 9", "Android");

        success.Should().BeFalse();
        service.GetPairedDevices().Should().BeEmpty();
    }

    [Fact]
    public async Task UnpairDevice_RemovesDeviceAndFiresEvent()
    {
        using var service = new PhoneLinkService();
        DevicePeer? disconnectedPeer = null;
        service.PeerDisconnected += (_, p) => disconnectedPeer = p;

        string pin = await service.StartPairingSessionAsync("Galaxy S24");
        await service.CompletePairingAsync(pin, "device-uuid-3", "Galaxy S24", "Android");

        service.UnpairDevice("device-uuid-3");

        service.GetPairedDevices().Should().BeEmpty();
        disconnectedPeer.Should().NotBeNull();
        disconnectedPeer!.DeviceId.Should().Be("device-uuid-3");
    }
}
