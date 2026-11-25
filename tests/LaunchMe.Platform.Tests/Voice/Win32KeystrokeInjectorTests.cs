using FluentAssertions;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.Platform.Win32.Voice;
using Moq;
using Xunit;

namespace LaunchMe.Platform.Tests.Voice;

public class Win32KeystrokeInjectorTests
{
    [Fact]
    public void InjectText_EmptyOrNull_DoesNotThrow()
    {
        var injector = new Win32KeystrokeInjector();

        var actNull = () => injector.InjectText(null!);
        var actEmpty = () => injector.InjectText(string.Empty);

        actNull.Should().NotThrow();
        actEmpty.Should().NotThrow();
    }

    [Fact]
    public void InjectText_ShortText_ExecutesWithoutError()
    {
        var injector = new Win32KeystrokeInjector();

        var act = () => injector.InjectText("hello");

        act.Should().NotThrow();
    }

    [Fact]
    public void InjectText_LongText_InvokesClipboardWriter()
    {
        var clipboardMock = new Mock<IClipboardWriter>();
        var injector = new Win32KeystrokeInjector(clipboardMock.Object);

        injector.InjectText("This is a long sentence meant to trigger clipboard paste.");

        clipboardMock.Verify(c => c.SetText(It.IsAny<string>()), Times.Once);
    }
}
