using System.Diagnostics;
using LaunchMe.Contracts.Search;
using Microsoft.VisualBasic.FileIO;

namespace LaunchMe.Core.Files;

public sealed class FileSearchService : IFileSearchService
{
    private readonly HashSet<string> _approvedScopes = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public IReadOnlyList<string> ApprovedFolderScopes
    {
        get
        {
            lock (_lock)
            {
                return _approvedScopes.ToList();
            }
        }
    }

    public void GrantFolderScope(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        var normalized = Path.GetFullPath(folderPath);
        if (!Directory.Exists(normalized))
        {
            throw new DirectoryNotFoundException($"Folder does not exist: {normalized}");
        }

        lock (_lock)
        {
            _approvedScopes.Add(normalized);
        }
    }

    public void RevokeFolderScope(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        var normalized = Path.GetFullPath(folderPath);
        lock (_lock)
        {
            _approvedScopes.Remove(normalized);
        }
    }

    public Task<IReadOnlyList<FileSearchResult>> SearchFilesAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<IReadOnlyList<FileSearchResult>>([]);
        }

        List<string> scopes;
        lock (_lock)
        {
            scopes = _approvedScopes.ToList();
        }

        if (scopes.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<FileSearchResult>>([]);
        }

        var results = new List<FileSearchResult>();
        var cleanQuery = query.Trim();

        foreach (var root in scopes)
        {
            ct.ThrowIfCancellationRequested();
            if (!Directory.Exists(root)) continue;

            try
            {
                var options = new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true,
                    MaxRecursionDepth = 8
                };

                foreach (var file in Directory.EnumerateFiles(root, "*", options))
                {
                    ct.ThrowIfCancellationRequested();
                    var fileName = Path.GetFileName(file);
                    var score = CalculateMatchScore(fileName, cleanQuery);

                    if (score > 0)
                    {
                        var info = new FileInfo(file);
                        results.Add(new FileSearchResult
                        {
                            Path = file,
                            FileName = fileName,
                            Extension = info.Extension,
                            SizeBytes = info.Length,
                            LastModifiedUtc = info.LastWriteTimeUtc,
                            MatchScore = score
                        });
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { /* Skip inaccessible paths */ }
        }

        var sorted = results
            .OrderByDescending(r => r.MatchScore)
            .ThenByDescending(r => r.LastModifiedUtc)
            .Take(100)
            .ToList();

        return Task.FromResult<IReadOnlyList<FileSearchResult>>(sorted);
    }

    private static double CalculateMatchScore(string fileName, string query)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        if (nameWithoutExt.Equals(query, StringComparison.OrdinalIgnoreCase)) return 1.0;
        if (nameWithoutExt.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 0.85;
        if (fileName.Contains(query, StringComparison.OrdinalIgnoreCase)) return 0.65;
        return 0.0;
    }

    public void RevealInExplorer(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path) && !Directory.Exists(path)) return;

        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"")
        {
            UseShellExecute = true
        });
    }

    public void OpenFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path)) return;

        Process.Start(new ProcessStartInfo(path)
        {
            UseShellExecute = true
        });
    }

    public Task CopyFileToClipboardAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteFileAsync(string path, bool sendToRecycleBin = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path)) return Task.FromResult(false);

        try
        {
            if (sendToRecycleBin)
            {
                FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
            else
            {
                File.Delete(path);
            }
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
