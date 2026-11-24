namespace LaunchMe.Contracts.Search;

/// <summary>
/// Privacy-first file search restricted strictly to user-granted folder scopes.
/// </summary>
public interface IFileSearchService
{
    IReadOnlyList<string> ApprovedFolderScopes { get; }
    void GrantFolderScope(string folderPath);
    void RevokeFolderScope(string folderPath);
    Task<IReadOnlyList<FileSearchResult>> SearchFilesAsync(string query, CancellationToken ct = default);
    Task CopyFileToClipboardAsync(string path);
    void RevealInExplorer(string path);
    void OpenFile(string path);
    Task<bool> DeleteFileAsync(string path, bool sendToRecycleBin);
}
