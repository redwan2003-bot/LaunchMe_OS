namespace LaunchMe.Contracts.Agent;

public enum DiffLineType
{
    Header,
    Context,
    Added,
    Deleted
}

public record DiffLine(int? OldLineNumber, int? NewLineNumber, DiffLineType Type, string Text);

public record DiffFile(string OldPath, string NewPath, IReadOnlyList<DiffLine> Lines);

public record DiffDocument(IReadOnlyList<DiffFile> Files);

public interface IDiffParserService
{
    DiffDocument Parse(string unifiedDiff);
}
