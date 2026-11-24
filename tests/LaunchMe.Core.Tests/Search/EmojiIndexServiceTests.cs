using FluentAssertions;
using LaunchMe.Core.Search;
using Xunit;

namespace LaunchMe.Core.Tests.Search;

public sealed class EmojiIndexServiceTests
{
    private readonly EmojiIndexService _service = new();

    [Fact]
    public void GetAll_ReturnsComprehensiveCatalog()
    {
        var emojis = _service.GetAllEmojis();
        emojis.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("rocket", "🚀")]
    [InlineData("happy", "😀")]
    [InlineData("laptop", "💻")]
    public void Search_MatchesKeywordsOrNames(string query, string expectedChar)
    {
        var results = _service.SearchEmojis(query);
        results.Should().Contain(e => e.Character == expectedChar);
    }
}
