using FluentAssertions;
using LaunchMe.UI.Widgets.Affirmations;
using Xunit;

namespace LaunchMe.UI.Tests.Widgets;

public sealed class AffirmationWidgetViewModelTests
{
    [Fact]
    public void InitialState_LoadsFirstQuote()
    {
        // Arrange & Act
        using var vm = new AffirmationWidgetViewModel(startActive: false);

        // Assert
        vm.CurrentText.Should().NotBeNullOrWhiteSpace();
        vm.Author.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void NextAndPreviousQuote_CyclesThroughQuotes()
    {
        // Arrange
        var custom = new List<AffirmationItem>
        {
            new("Quote 1", "Author 1"),
            new("Quote 2", "Author 2")
        };
        using var vm = new AffirmationWidgetViewModel(custom, startActive: false);

        // Act & Assert 1: First
        vm.CurrentText.Should().Be("“Quote 1”");

        // Act 2: Next
        vm.NextQuote();
        vm.CurrentText.Should().Be("“Quote 2”");

        // Act 3: Next wraps around
        vm.NextQuote();
        vm.CurrentText.Should().Be("“Quote 1”");

        // Act 4: Previous wraps to end
        vm.PreviousQuote();
        vm.CurrentText.Should().Be("“Quote 2”");
    }

    [Fact]
    public void AddQuote_AddsCustomItem()
    {
        // Arrange
        using var vm = new AffirmationWidgetViewModel(new[] { new AffirmationItem("First", "Author") }, startActive: false);

        // Act
        vm.AddQuote("Stay hungry, stay foolish.", "Steve Jobs");
        vm.NextQuote();

        // Assert
        vm.CurrentText.Should().Be("“Stay hungry, stay foolish.”");
        vm.Author.Should().Be("Steve Jobs");
    }
}
