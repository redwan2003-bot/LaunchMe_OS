using FluentAssertions;
using LaunchMe.Core.Widgets.Affirmations;
using Xunit;

namespace LaunchMe.Core.Tests.Widgets;

public sealed class AffirmationsProviderTests
{
    private readonly IAffirmationsProvider _provider = new AffirmationsProvider();

    [Fact]
    public void GetDailyAffirmation_IsDeterministicForSameDate()
    {
        var date = new DateOnly(2026, 11, 24);
        var first = _provider.GetDailyAffirmation(date);
        var second = _provider.GetDailyAffirmation(date);

        first.Should().NotBeNull();
        first.Id.Should().Be(second.Id);
        first.Quote.Should().Be(second.Quote);
    }

    [Fact]
    public void GetRandomAffirmation_ReturnsValidItem()
    {
        var item = _provider.GetRandomAffirmation(AffirmationCategory.Focus);

        item.Should().NotBeNull();
        item.Category.Should().Be(AffirmationCategory.Focus);
        item.Quote.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetByCategory_ReturnsMatchingItems()
    {
        var items = _provider.GetByCategory(AffirmationCategory.Productivity);

        items.Should().NotBeEmpty();
        items.Should().OnlyContain(i => i.Category == AffirmationCategory.Productivity);
    }

    [Fact]
    public void AddCustomAffirmation_And_RemoveCustomAffirmation_WorksCorrectly()
    {
        var added = _provider.AddCustomAffirmation("Small steps lead to massive destinations.", "Mentor", AffirmationCategory.Mindfulness);

        added.Should().NotBeNull();
        added.IsCustom.Should().BeTrue();
        added.Quote.Should().Be("Small steps lead to massive destinations.");

        var all = _provider.GetAll();
        all.Should().Contain(i => i.Id == added.Id);

        var removed = _provider.RemoveCustomAffirmation(added.Id);
        removed.Should().BeTrue();

        var allAfterRemove = _provider.GetAll();
        allAfterRemove.Should().NotContain(i => i.Id == added.Id);
    }
}
