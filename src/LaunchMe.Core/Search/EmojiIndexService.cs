using LaunchMe.Contracts.Search;

namespace LaunchMe.Core.Search;

public sealed class EmojiIndexService : IEmojiService
{
    private static readonly List<EmojiItem> EmojiCatalog =
    [
        new() { Character = "😀", Name = "Grinning Face", Category = "Smileys & Emotion", Keywords = ["smile", "happy", "joy", "grin"] },
        new() { Character = "😃", Name = "Grinning Face with Big Eyes", Category = "Smileys & Emotion", Keywords = ["smile", "happy", "joy", "haha"] },
        new() { Character = "😄", Name = "Grinning Face with Smiling Eyes", Category = "Smileys & Emotion", Keywords = ["happy", "joy", "laugh", "smile"] },
        new() { Character = "😁", Name = "Beaming Face with Smiling Eyes", Category = "Smileys & Emotion", Keywords = ["smile", "happy", "beam"] },
        new() { Character = "🚀", Name = "Rocket", Category = "Travel & Places", Keywords = ["rocket", "space", "launch", "fast", "fly"] },
        new() { Character = "🛸", Name = "Flying Saucer", Category = "Travel & Places", Keywords = ["ufo", "alien", "space"] },
        new() { Character = "💻", Name = "Laptop", Category = "Objects", Keywords = ["computer", "pc", "tech", "code", "dev"] },
        new() { Character = "📱", Name = "Mobile Phone", Category = "Objects", Keywords = ["phone", "cell", "mobile"] },
        new() { Character = "⚡", Name = "High Voltage", Category = "Symbols", Keywords = ["lightning", "thunder", "fast", "electric", "power"] },
        new() { Character = "🔥", Name = "Fire", Category = "Travel & Places", Keywords = ["flame", "hot", "lit", "burn"] },
        new() { Character = "✨", Name = "Sparkles", Category = "Activities", Keywords = ["magic", "shine", "stars", "sparkle", "clean"] },
        new() { Character = "🎉", Name = "Party Popper", Category = "Activities", Keywords = ["party", "celebration", "tada", "congrats"] },
        new() { Character = "❤️", Name = "Red Heart", Category = "Smileys & Emotion", Keywords = ["love", "heart", "like"] },
        new() { Character = "👍", Name = "Thumbs Up", Category = "People & Body", Keywords = ["yes", "ok", "good", "approve", "like"] },
        new() { Character = "👎", Name = "Thumbs Down", Category = "People & Body", Keywords = ["no", "bad", "dislike"] },
        new() { Character = "🎨", Name = "Artist Palette", Category = "Activities", Keywords = ["art", "design", "paint", "color"] },
        new() { Character = "🎵", Name = "Musical Note", Category = "Objects", Keywords = ["music", "song", "sound", "tune"] },
        new() { Character = "⚙️", Name = "Gear", Category = "Objects", Keywords = ["settings", "config", "tool", "preferences"] },
        new() { Character = "🔍", Name = "Magnifying Glass Left", Category = "Objects", Keywords = ["search", "find", "zoom", "query"] },
        new() { Character = "📁", Name = "File Folder", Category = "Objects", Keywords = ["folder", "directory", "file", "storage"] },
        new() { Character = "📅", Name = "Calendar", Category = "Objects", Keywords = ["date", "schedule", "time", "event"] },
        new() { Character = "🔒", Name = "Locked", Category = "Objects", Keywords = ["lock", "secure", "private", "security"] }
    ];

    public IReadOnlyList<EmojiItem> GetAllEmojis() => EmojiCatalog;

    public IReadOnlyList<EmojiItem> SearchEmojis(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return EmojiCatalog;
        }

        var q = query.Trim().ToLowerInvariant();
        return EmojiCatalog
            .Where(e => e.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        e.Keywords.Any(k => k.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        e.Category.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
