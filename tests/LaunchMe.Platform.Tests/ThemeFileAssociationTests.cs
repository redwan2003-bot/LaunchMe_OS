using FluentAssertions;
using LaunchMe.Platform.Win32.Shell;
using Microsoft.Win32;
using Xunit;

namespace LaunchMe.Platform.Tests;

public sealed class ThemeFileAssociationTests : IDisposable
{
    private readonly string _testRootSubKey = $@"Software\LaunchMeWinTests\{Guid.NewGuid():N}";
    private readonly ThemeFileAssociationService _service;

    public ThemeFileAssociationTests()
    {
        _service = new ThemeFileAssociationService(_testRootSubKey);
    }

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(_testRootSubKey, throwOnMissingSubKey: false);
    }

    [Fact]
    public void IsRegistered_BeforeRegistration_ReturnsFalse()
    {
        _service.IsRegistered.Should().BeFalse();
    }

    [Fact]
    public void RegisterFileAssociation_CreatesExpectedKeysAndIsRegistered()
    {
        const string exePath = @"C:\Program Files\LaunchMe\LaunchMe.exe";

        _service.RegisterFileAssociation(exePath);

        _service.IsRegistered.Should().BeTrue();

        using var extKey = Registry.CurrentUser.OpenSubKey($@"{_testRootSubKey}\.launchmetheme");
        extKey.Should().NotBeNull();
        extKey!.GetValue(null).Should().Be("LaunchMeWin.Theme");
        extKey.GetValue("Content Type").Should().Be("application/x-launchme-theme");

        using var progIdKey = Registry.CurrentUser.OpenSubKey($@"{_testRootSubKey}\LaunchMeWin.Theme");
        progIdKey.Should().NotBeNull();
        progIdKey!.GetValue(null).Should().Be("LaunchMe Theme Package");

        using var iconKey = Registry.CurrentUser.OpenSubKey($@"{_testRootSubKey}\LaunchMeWin.Theme\DefaultIcon");
        iconKey.Should().NotBeNull();
        iconKey!.GetValue(null).Should().Be($"{exePath},0");

        using var commandKey = Registry.CurrentUser.OpenSubKey($@"{_testRootSubKey}\LaunchMeWin.Theme\shell\open\command");
        commandKey.Should().NotBeNull();
        commandKey!.GetValue(null).Should().Be($"\"{exePath}\" --import-theme \"%1\"");
    }

    [Fact]
    public void UnregisterFileAssociation_RemovesKeysAndMarksUnregistered()
    {
        const string exePath = @"C:\LaunchMe\LaunchMe.exe";
        _service.RegisterFileAssociation(exePath);
        _service.IsRegistered.Should().BeTrue();

        _service.UnregisterFileAssociation();

        _service.IsRegistered.Should().BeFalse();
        Registry.CurrentUser.OpenSubKey($@"{_testRootSubKey}\.launchmetheme").Should().BeNull();
        Registry.CurrentUser.OpenSubKey($@"{_testRootSubKey}\LaunchMeWin.Theme").Should().BeNull();
    }

    [Fact]
    public void RegisterAndUnregister_AreIdempotent()
    {
        const string exePath = @"C:\LaunchMe\LaunchMe.exe";

        _service.RegisterFileAssociation(exePath);
        _service.RegisterFileAssociation(exePath);
        _service.IsRegistered.Should().BeTrue();

        _service.UnregisterFileAssociation();
        _service.UnregisterFileAssociation();
        _service.IsRegistered.Should().BeFalse();
    }

    [Fact]
    public void IsRegistered_WhenCommandMissingOrEmpty_ReturnsFalse()
    {
        using (var extKey = Registry.CurrentUser.CreateSubKey($@"{_testRootSubKey}\.launchmetheme", writable: true))
        {
            extKey.SetValue(null, "LaunchMeWin.Theme");
        }

        _service.IsRegistered.Should().BeFalse();

        using (var cmdKey = Registry.CurrentUser.CreateSubKey($@"{_testRootSubKey}\LaunchMeWin.Theme\shell\open\command", writable: true))
        {
            cmdKey.SetValue(null, "   ");
        }

        _service.IsRegistered.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("LaunchMe.exe")]
    [InlineData(@"relative\LaunchMe.exe")]
    [InlineData(@"C:\LaunchMe""\LaunchMe.exe")]
    [InlineData("C:\\LaunchMe\nLaunchMe.exe")]
    [InlineData("C:\\LaunchMe\rLaunchMe.exe")]
    public void RegisterFileAssociation_WithInvalidExecutablePath_ThrowsArgumentException(string invalidPath)
    {
        Action act = () => _service.RegisterFileAssociation(invalidPath);

        act.Should().Throw<ArgumentException>();
        _service.IsRegistered.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithWhitespaceSubKey_ThrowsArgumentException()
    {
        Action act = () => _ = new ThemeFileAssociationService("   ");

        act.Should().Throw<ArgumentException>();
    }
}
