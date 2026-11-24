using FluentAssertions;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.Core.Clipboard;
using Xunit;

namespace LaunchMe.Core.Tests.Clipboard;

public sealed class ClipboardClassifierTests
{
    [Theory]
    [InlineData("#1E40AF", ClipboardCategory.Colors)]
    [InlineData("#FFF", ClipboardCategory.Colors)]
    [InlineData("rgba(255, 0, 0, 0.5)", ClipboardCategory.Colors)]
    public void Classify_ColorStrings_ReturnsColors(string text, ClipboardCategory expected)
    {
        var result = ClipboardClassifier.Classify(text, null, false);
        result.Should().Be(expected);
    }

    [Fact]
    public void Classify_FilePaths_ReturnsFiles()
    {
        var result = ClipboardClassifier.Classify(null, ["C:\\file.txt"], false);
        result.Should().Be(ClipboardCategory.Files);
    }

    [Fact]
    public void Classify_Bitmap_ReturnsImages()
    {
        var result = ClipboardClassifier.Classify(null, null, true);
        result.Should().Be(ClipboardCategory.Images);
    }

    [Fact]
    public void Classify_Screenshot_ReturnsScreenshots()
    {
        var result = ClipboardClassifier.Classify(null, null, true, isScreenshot: true);
        result.Should().Be(ClipboardCategory.Screenshots);
    }

    [Fact]
    public void Classify_CodeSnippet_ReturnsCode()
    {
        var snippet = "public class Foo { public void Bar() { return; } }";
        var result = ClipboardClassifier.Classify(snippet, null, false);
        result.Should().Be(ClipboardCategory.Code);
    }

    [Fact]
    public void Classify_PlainText_ReturnsText()
    {
        var result = ClipboardClassifier.Classify("Meeting at 3pm tomorrow.", null, false);
        result.Should().Be(ClipboardCategory.Text);
    }
}
