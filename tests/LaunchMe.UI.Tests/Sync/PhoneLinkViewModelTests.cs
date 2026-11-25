using FluentAssertions;
using LaunchMe.Contracts.Sync;
using LaunchMe.Core.Sync;
using LaunchMe.UI.Sync;
using Xunit;

namespace LaunchMe.UI.Tests.Sync;

public class PhoneLinkViewModelTests
{
    [Fact]
    public void StartPairingCommand_GeneratesPinAndSetsActive()
    {
        using var service = new PhoneLinkService();
        var vm = new PhoneLinkViewModel(service);

        vm.StartPairingCommand.Execute(null);

        vm.IsPairingActive.Should().BeTrue();
        vm.CurrentPairingPin.Should().HaveLength(6);
    }

    [Fact]
    public async Task PairingCompletion_AddsDeviceToCollection()
    {
        using var service = new PhoneLinkService();
        var vm = new PhoneLinkViewModel(service);

        string pin = await service.StartPairingSessionAsync("MacBook");
        await service.CompletePairingAsync(pin, "dev-mac", "MacBook Air", "macOS");

        vm.PairedDevices.Should().ContainSingle(p => p.DeviceId == "dev-mac");
        vm.IsPairingActive.Should().BeFalse();
    }

    [Fact]
    public async Task UnpairCommand_RemovesDeviceFromCollection()
    {
        using var service = new PhoneLinkService();
        var vm = new PhoneLinkViewModel(service);

        string pin = await service.StartPairingSessionAsync("iPad");
        await service.CompletePairingAsync(pin, "dev-ipad", "iPad Pro", "iOS");

        vm.UnpairCommand.Execute("dev-ipad");

        vm.PairedDevices.Should().BeEmpty();
    }
}
