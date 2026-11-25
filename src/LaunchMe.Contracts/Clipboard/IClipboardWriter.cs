namespace LaunchMe.Contracts.Clipboard;

/// <summary>
/// Writes text or file paths cleanly to the Windows system clipboard.
/// </summary>
public interface IClipboardWriter
{
    bool SetText(string text);
    bool SetFiles(IEnumerable<string> filePaths);
}
