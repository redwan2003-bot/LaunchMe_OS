using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Clipboard;

/// <summary>
/// Writes UTF-16 text and CF_HDROP file lists to the Windows system clipboard.
/// </summary>
public sealed class Win32ClipboardWriter : IClipboardWriter
{
    private const int OpenClipboardRetries = 5;
    private const int OpenClipboardRetryDelayMs = 5;

    public bool SetText(string text)
    {
        if (text is null)
        {
            return false;
        }

        int charByteCount = Encoding.Unicode.GetByteCount(text);
        byte[] payload = new byte[charByteCount + 2];
        Encoding.Unicode.GetBytes(text, 0, text.Length, payload, 0);

        return WriteGlobalBytes(Win32Interop.CF_UNICODETEXT, payload);
    }

    public bool SetFiles(IEnumerable<string> filePaths)
    {
        if (filePaths is null)
        {
            return false;
        }

        var validPaths = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string? rawPath in filePaths)
        {
            if (TryGetExistingFullPath(rawPath, out string fullPath) && seen.Add(fullPath))
            {
                validPaths.Add(fullPath);
            }
        }

        if (validPaths.Count == 0)
        {
            return false;
        }

        byte[] payload = BuildDropFilesPayload(validPaths);
        return WriteGlobalBytes(Win32Interop.CF_HDROP, payload);
    }

    /// <summary>
    /// Serializes a list of full file system paths into a Win32 DROPFILES (CF_HDROP) Unicode payload.
    /// </summary>
    public static byte[] BuildDropFilesPayload(IReadOnlyList<string> fullPaths)
    {
        ArgumentNullException.ThrowIfNull(fullPaths);
        if (fullPaths.Count == 0)
        {
            throw new ArgumentException("At least one path is required to build a DROPFILES payload.", nameof(fullPaths));
        }

        int headerSize = Marshal.SizeOf<Win32Interop.DROPFILES>();
        int pathsByteCount = 0;
        for (int i = 0; i < fullPaths.Count; i++)
        {
            pathsByteCount += Encoding.Unicode.GetByteCount(fullPaths[i]) + 2;
        }

        // Trailing extra UTF-16 null terminator for double-null termination.
        byte[] payload = new byte[headerSize + pathsByteCount + 2];

        var dropFiles = new Win32Interop.DROPFILES
        {
            pFiles = (uint)headerSize,
            pt = default,
            fNC = 0,
            fWide = 1
        };

        MemoryMarshal.Write(payload.AsSpan(0, headerSize), in dropFiles);

        int offset = headerSize;
        for (int i = 0; i < fullPaths.Count; i++)
        {
            string path = fullPaths[i];
            int written = Encoding.Unicode.GetBytes(path, 0, path.Length, payload, offset);
            offset += written + 2; // Leave 2 zero bytes as per-string null terminator.
        }

        return payload;
    }

    private static bool TryGetExistingFullPath(string? rawPath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return false;
        }

        try
        {
            string candidate = Path.GetFullPath(rawPath.Trim());
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                fullPath = candidate;
                return true;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or SecurityException)
        {
            return false;
        }

        return false;
    }

    private static bool WriteGlobalBytes(uint format, byte[] payload)
    {
        nint hGlobal = Win32Interop.GlobalAlloc(Win32Interop.GMEM_MOVEABLE, (nuint)payload.Length);
        if (hGlobal == nint.Zero)
        {
            return false;
        }

        bool ownershipTransferred = false;
        try
        {
            nint lockedPtr = Win32Interop.GlobalLock(hGlobal);
            if (lockedPtr == nint.Zero)
            {
                return false;
            }

            try
            {
                Marshal.Copy(payload, 0, lockedPtr, payload.Length);
            }
            finally
            {
                Win32Interop.GlobalUnlock(hGlobal);
            }

            if (!TryOpenClipboard())
            {
                return false;
            }

            try
            {
                if (!Win32Interop.EmptyClipboard())
                {
                    return false;
                }

                nint dataHandle = Win32Interop.SetClipboardData(format, hGlobal);
                if (dataHandle == nint.Zero)
                {
                    return false;
                }

                ownershipTransferred = true;
                return true;
            }
            finally
            {
                Win32Interop.CloseClipboard();
            }
        }
        finally
        {
            if (!ownershipTransferred)
            {
                Win32Interop.GlobalFree(hGlobal);
            }
        }
    }

    private static bool TryOpenClipboard()
    {
        for (int attempt = 0; attempt < OpenClipboardRetries; attempt++)
        {
            if (Win32Interop.OpenClipboard(nint.Zero))
            {
                return true;
            }

            if (attempt < OpenClipboardRetries - 1)
            {
                Thread.Sleep(OpenClipboardRetryDelayMs);
            }
        }

        return false;
    }
}
