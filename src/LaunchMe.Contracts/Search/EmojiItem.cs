namespace LaunchMe.Contracts.Search;

public sealed class EmojiItem
{
    public string Character { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = [];
}
