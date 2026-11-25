using FluentAssertions;
using LaunchMe.Platform.Win32.Gestures;
using LaunchMe.Platform.Win32.Native;
using Xunit;

namespace LaunchMe.Platform.Tests;

public class HotkeyTests
{
    [Theory]
    [InlineData("Alt+Space", Win32Interop.MOD_ALT, 0x20u)]
    [InlineData("Win+4", Win32Interop.MOD_WIN, (uint)'4')]
    [InlineData("Ctrl+Shift+D", Win32Interop.MOD_CONTROL | Win32Interop.MOD_SHIFT, (uint)'D')]
    [InlineData("Ctrl+Alt+1", Win32Interop.MOD_CONTROL | Win32Interop.MOD_ALT, (uint)'1')]
    [InlineData("F11", 0u, 0x7Au)]
    public void TryParse_ValidHotkeyStrings_ParsesCorrectly(string hotkey, uint expectedMod, uint expectedVk)
    {
        bool success = Win32HotkeyParser.TryParse(hotkey, out uint mod, out uint vk);
        success.Should().BeTrue();
        mod.Should().Be(expectedMod);
        vk.Should().Be(expectedVk);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("InvalidModifier+Space")]
    public void TryParse_InvalidStrings_ReturnsFalse(string invalid)
    {
        bool success = Win32HotkeyParser.TryParse(invalid, out _, out _);
        success.Should().BeFalse();
    }

    [Fact]
    public void HotkeyService_UnregisterUnregisteredKey_ReturnsFalseIdempotently()
    {
        using var service = new Win32HotkeyService();
        bool unregistered = service.Unregister(Win32Interop.MOD_ALT, 0x20);
        unregistered.Should().BeFalse();
    }

    [Fact]
    public void HotkeyService_Dispose_TerminatesMessagePumpThreadDeterministicallyAndIsIdempotent()
    {
        var service = new Win32HotkeyService();
        service.IsPumpAlive.Should().BeTrue();

        service.Dispose();

        service.IsPumpAlive.Should().BeFalse();
        service.Register(Win32Interop.MOD_ALT, 0x20).Should().BeFalse();
        service.Unregister(Win32Interop.MOD_ALT, 0x20).Should().BeFalse();

        Action secondDispose = () => service.Dispose();
        secondDispose.Should().NotThrow();
    }
}
