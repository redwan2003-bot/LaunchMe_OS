namespace LaunchMe.Core.Apps;

public static class ShellLinkHelper
{
    public static string ResolveShortcutTarget(string shortcutPath)
    {
        if (!File.Exists(shortcutPath)) return string.Empty;

        try
        {
            // Binary parser for MS-SHLLINK (.lnk) structure
            var bytes = File.ReadAllBytes(shortcutPath);
            if (bytes.Length < 0x4C) return string.Empty;

            var flags = BitConverter.ToInt32(bytes, 0x14);
            var hasLinkInfo = (flags & 0x02) != 0;
            if (!hasLinkInfo) return string.Empty;

            var idListSize = 0;
            var offset = 0x4C;
            var hasLinkTargetIdList = (flags & 0x01) != 0;
            if (hasLinkTargetIdList && bytes.Length >= 0x4E)
            {
                idListSize = BitConverter.ToUInt16(bytes, 0x4C);
                offset = 0x4E + idListSize;
            }

            if (bytes.Length < offset + 0x1C) return string.Empty;

            var localBasePathOffset = BitConverter.ToInt32(bytes, offset + 0x10);
            var targetOffset = offset + localBasePathOffset;
            if (targetOffset >= bytes.Length) return string.Empty;

            var end = targetOffset;
            while (end < bytes.Length && bytes[end] != 0)
            {
                end++;
            }

            return System.Text.Encoding.Default.GetString(bytes, targetOffset, end - targetOffset);
        }
        catch
        {
            return string.Empty;
        }
    }
}
