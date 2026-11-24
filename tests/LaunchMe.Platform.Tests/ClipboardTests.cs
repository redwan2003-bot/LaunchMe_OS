using FluentAssertions;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.Platform.Win32.Clipboard;
using Xunit;

namespace LaunchMe.Platform.Tests;

public class ClipboardTests
{
    [Theory]
    [InlineData("#FFF", ClipboardCategory.Colors)]
    [InlineData("#1E40AF", ClipboardCategory.Colors)]
    [InlineData("rgb(255, 0, 0)", ClipboardCategory.Colors)]
    [InlineData("rgba(30, 64, 175, 0.65)", ClipboardCategory.Colors)]
    public void Classify_ColorCodes_ReturnsColors(string text, ClipboardCategory expected)
    {
        var category = ClipboardClassifier.Classify(text, null, false);
        category.Should().Be(expected);
    }

    [Theory]
    [InlineData("public class Program { }", ClipboardCategory.Code)]
    [InlineData("const x = () => { return 42; };", ClipboardCategory.Code)]
    [InlineData("{\"key\": \"value\"}", ClipboardCategory.Code)]
    public void Classify_CodeSnippets_ReturnsCode(string text, ClipboardCategory expected)
    {
        var category = ClipboardClassifier.Classify(text, null, false);
        category.Should().Be(expected);
    }

    [Fact]
    public void Classify_BitmapPresent_ReturnsImages()
    {
        var category = ClipboardClassifier.Classify("any text", null, true);
        category.Should().Be(ClipboardCategory.Images);
    }

    [Fact]
    public void Classify_FilePathsPresent_ReturnsFiles()
    {
        var category = ClipboardClassifier.Classify(null, ["C:\\file.txt"], false);
        category.Should().Be(ClipboardCategory.Files);
    }

    [Fact]
    public void Store_CapacityLimit_EvictsUnpinnedFirst()
    {
        var store = new ClipboardStore(2);
        var item1 = new ClipboardEntry { Id = "1", TextContent = "Item 1", IsPinned = true };
        var item2 = new ClipboardEntry { Id = "2", TextContent = "Item 2", IsPinned = false };
        var item3 = new ClipboardEntry { Id = "3", TextContent = "Item 3", IsPinned = false };

        store.AddEntry(item1);
        store.AddEntry(item2);
        store.AddEntry(item3);

        var recent = store.GetItems(ClipboardCategory.Recent);
        recent.Should().Contain(x => x.Id == "1"); // Pinned item retained
        recent.Should().Contain(x => x.Id == "3"); // Newest item present
    }
}
