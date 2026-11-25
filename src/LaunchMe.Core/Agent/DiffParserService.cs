using LaunchMe.Contracts.Agent;

namespace LaunchMe.Core.Agent;

/// <summary>
/// Parser for unified diff strings produced by git and AI coding agents.
/// </summary>
public class DiffParserService : IDiffParserService
{
    public DiffDocument Parse(string unifiedDiff)
    {
        if (string.IsNullOrWhiteSpace(unifiedDiff))
        {
            return new DiffDocument(Array.Empty<DiffFile>());
        }

        var files = new List<DiffFile>();
        var lines = unifiedDiff.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        string currentOldPath = string.Empty;
        string currentNewPath = string.Empty;
        var currentLines = new List<DiffLine>();

        int oldLineNo = 0;
        int newLineNo = 0;

        foreach (var rawLine in lines)
        {
            if (rawLine.StartsWith("diff --git") || rawLine.StartsWith("--- "))
            {
                if (currentLines.Count > 0 && !string.IsNullOrEmpty(currentNewPath))
                {
                    files.Add(new DiffFile(currentOldPath, currentNewPath, currentLines.ToList()));
                    currentLines.Clear();
                }

                if (rawLine.StartsWith("--- "))
                {
                    currentOldPath = rawLine[4..].Trim();
                }
                continue;
            }

            if (rawLine.StartsWith("+++ "))
            {
                currentNewPath = rawLine[4..].Trim();
                continue;
            }

            if (rawLine.StartsWith("@@"))
            {
                // Chunk header: @@ -1,5 +1,6 @@
                var parts = rawLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    var oldPart = parts[1].TrimStart('-');
                    var newPart = parts[2].TrimStart('+');

                    int.TryParse(oldPart.Split(',')[0], out oldLineNo);
                    int.TryParse(newPart.Split(',')[0], out newLineNo);
                }

                currentLines.Add(new DiffLine(null, null, DiffLineType.Header, rawLine));
                continue;
            }

            if (rawLine.StartsWith("+"))
            {
                currentLines.Add(new DiffLine(null, newLineNo++, DiffLineType.Added, rawLine[1..]));
            }
            else if (rawLine.StartsWith("-"))
            {
                currentLines.Add(new DiffLine(oldLineNo++, null, DiffLineType.Deleted, rawLine[1..]));
            }
            else
            {
                string text = rawLine.StartsWith(" ") ? rawLine[1..] : rawLine;
                currentLines.Add(new DiffLine(oldLineNo++, newLineNo++, DiffLineType.Context, text));
            }
        }

        if (currentLines.Count > 0 && !string.IsNullOrEmpty(currentNewPath))
        {
            files.Add(new DiffFile(currentOldPath, currentNewPath, currentLines));
        }
        else if (currentLines.Count > 0 && files.Count == 0)
        {
            // Generic single-file diff without explicit headers
            files.Add(new DiffFile("original", "modified", currentLines));
        }

        return new DiffDocument(files);
    }
}
