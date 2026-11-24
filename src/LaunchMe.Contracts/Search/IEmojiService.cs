namespace LaunchMe.Contracts.Search;

/// <summary>
/// Emoji and Unicode symbol lookup provider triggered via TAB in Unified Search.
/// </summary>
public interface IEmojiService
{
    IReadOnlyList<EmojiItem> SearchEmojis(string query);
    IReadOnlyList<EmojiItem> GetAllEmojis();
}
