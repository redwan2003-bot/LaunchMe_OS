using FluentAssertions;
using LaunchMe.Core.Sync;
using LaunchMe.UI.Sync;
using Xunit;

namespace LaunchMe.UI.Tests.Sync;

public class LeashViewModelTests
{
    [Fact]
    public void StartPairingCommand_GeneratesPinAndSetsActive()
    {
        using var service = new LeashService();
        var vm = new LeashViewModel(service);

        vm.StartPairingCommand.Execute(null);

        vm.IsPairingActive.Should().BeTrue();
        vm.CurrentPairingPin.Should().HaveLength(6);
    }

    [Fact]
    public async Task PairingCompletion_AddsDeviceToCollection()
    {
        using var service = new LeashService();
        var vm = new LeashViewModel(service);

        string pin = await service.StartPairingSessionAsync("Android");
        await service.CompletePairingAsync(pin, "dev-android", "Pixel 9", "Android");

        vm.PairedDevices.Should().ContainSingle(p => p.DeviceId == "dev-android");
        vm.IsPairingActive.Should().BeFalse();
    }

    [Fact]
    public async Task UnpairCommand_RemovesDeviceFromCollection()
    {
        using var service = new LeashService();
        var vm = new LeashViewModel(service);

        string pin = await service.StartPairingSessionAsync("Tablet");
        await service.CompletePairingAsync(pin, "dev-tab", "Galaxy Tab", "Android");

        vm.UnpairCommand.Execute("dev-tab");

        vm.PairedDevices.Should().BeEmpty();
    }
}
