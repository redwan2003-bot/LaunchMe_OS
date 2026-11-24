namespace LaunchMe.Contracts.Search;

public sealed class FileSearchResult
{
    public string Path { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public double MatchScore { get; set; }
}
