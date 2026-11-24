namespace LaunchMe.Core.Widgets.Affirmations;

public interface IAffirmationsProvider
{
    AffirmationItem GetDailyAffirmation(DateOnly? date = null);
    AffirmationItem GetRandomAffirmation(AffirmationCategory? category = null);
    IReadOnlyList<AffirmationItem> GetByCategory(AffirmationCategory category);
    IReadOnlyList<AffirmationItem> GetAll();
    AffirmationItem AddCustomAffirmation(string quote, string author = "Unknown", AffirmationCategory category = AffirmationCategory.Focus);
    bool RemoveCustomAffirmation(string id);
}

public sealed class AffirmationsProvider : IAffirmationsProvider
{
    private static readonly List<AffirmationItem> BuiltInAffirmations =
    [
        new() { Id = "focus-01", Quote = "Concentrate all your thoughts upon the work at hand. The sun's rays do not burn until brought to a focus.", Author = "Alexander Graham Bell", Category = AffirmationCategory.Focus },
        new() { Id = "focus-02", Quote = "Simplicity boils down to two steps: Identify the essential. Eliminate the rest.", Author = "Leo Babauta", Category = AffirmationCategory.Focus },
        new() { Id = "focus-03", Quote = "Starve your distractions, feed your focus.", Author = "Anonymous", Category = AffirmationCategory.Focus },
        new() { Id = "focus-04", Quote = "Where focus goes, energy flows.", Author = "Tony Robbins", Category = AffirmationCategory.Focus },

        new() { Id = "prod-01", Quote = "Action is the foundational key to all success.", Author = "Pablo Picasso", Category = AffirmationCategory.Productivity },
        new() { Id = "prod-02", Quote = "Amateurs sit and wait for inspiration, the rest of us just get up and go to work.", Author = "Stephen King", Category = AffirmationCategory.Productivity },
        new() { Id = "prod-03", Quote = "Done is better than perfect.", Author = "Sheryl Sandberg", Category = AffirmationCategory.Productivity },
        new() { Id = "prod-04", Quote = "Small deeds done are better than great deeds planned.", Author = "Peter Marshall", Category = AffirmationCategory.Productivity },

        new() { Id = "mind-01", Quote = "Peace comes from within. Do not seek it without.", Author = "Buddha", Category = AffirmationCategory.Mindfulness },
        new() { Id = "mind-02", Quote = "Within you, there is a stillness and a sanctuary to which you can retreat at any time.", Author = "Hermann Hesse", Category = AffirmationCategory.Mindfulness },
        new() { Id = "mind-03", Quote = "Breathe in calm, breathe out noise.", Author = "Anonymous", Category = AffirmationCategory.Mindfulness },

        new() { Id = "conf-01", Quote = "Believe you can and you're halfway there.", Author = "Theodore Roosevelt", Category = AffirmationCategory.Confidence },
        new() { Id = "conf-02", Quote = "You are capable of more than you know.", Author = "E.O. Wilson", Category = AffirmationCategory.Confidence },
        new() { Id = "conf-03", Quote = "Courage is resistance to fear, mastery of fear, not absence of fear.", Author = "Mark Twain", Category = AffirmationCategory.Confidence },

        new() { Id = "creat-01", Quote = "Creativity is intelligence having fun.", Author = "Albert Einstein", Category = AffirmationCategory.Creativity },
        new() { Id = "creat-02", Quote = "You can't use up creativity. The more you use, the more you have.", Author = "Maya Angelou", Category = AffirmationCategory.Creativity },

        new() { Id = "wis-01", Quote = "We are what we repeatedly do. Excellence, then, is not an act, but a habit.", Author = "Aristotle", Category = AffirmationCategory.Wisdom },
        new() { Id = "wis-02", Quote = "The only true wisdom is in knowing you know nothing.", Author = "Socrates", Category = AffirmationCategory.Wisdom }
    ];

    private readonly List<AffirmationItem> _customAffirmations = [];
    private readonly Lock _lock = new();

    public AffirmationItem GetDailyAffirmation(DateOnly? date = null)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var all = GetAll();
        if (all.Count == 0)
        {
            return BuiltInAffirmations[0];
        }

        var hash = Math.Abs((targetDate.Year * 367 + targetDate.DayOfYear * 31 + targetDate.Day) % all.Count);
        return all[hash];
    }

    public AffirmationItem GetRandomAffirmation(AffirmationCategory? category = null)
    {
        var pool = category.HasValue ? GetByCategory(category.Value) : GetAll();
        if (pool.Count == 0)
        {
            return BuiltInAffirmations[0];
        }

        var randIndex = Random.Shared.Next(pool.Count);
        return pool[randIndex];
    }

    public IReadOnlyList<AffirmationItem> GetByCategory(AffirmationCategory category)
    {
        lock (_lock)
        {
            return BuiltInAffirmations
                .Concat(_customAffirmations)
                .Where(a => a.Category == category)
                .ToList();
        }
    }

    public IReadOnlyList<AffirmationItem> GetAll()
    {
        lock (_lock)
        {
            return BuiltInAffirmations
                .Concat(_customAffirmations)
                .ToList();
        }
    }

    public AffirmationItem AddCustomAffirmation(
        string quote,
        string author = "Unknown",
        AffirmationCategory category = AffirmationCategory.Focus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(quote);

        var item = new AffirmationItem
        {
            Id = $"custom-{Guid.NewGuid():N}",
            Quote = quote.Trim(),
            Author = string.IsNullOrWhiteSpace(author) ? "Unknown" : author.Trim(),
            Category = category,
            IsCustom = true
        };

        lock (_lock)
        {
            _customAffirmations.Add(item);
        }

        return item;
    }

    public bool RemoveCustomAffirmation(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;

        lock (_lock)
        {
            return _customAffirmations.RemoveAll(a => a.Id == id) > 0;
        }
    }
}
