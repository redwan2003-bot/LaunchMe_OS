using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Gestures;

public static class Win32HotkeyParser
{
    public static bool TryParse(string hotkeyString, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;

        if (string.IsNullOrWhiteSpace(hotkeyString))
        {
            return false;
        }

        var parts = hotkeyString.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < parts.Length - 1; i++)
        {
            var mod = parts[i].ToLowerInvariant();
            switch (mod)
            {
                case "ctrl":
                case "control":
                    modifiers |= Win32Interop.MOD_CONTROL;
                    break;
                case "alt":
                    modifiers |= Win32Interop.MOD_ALT;
                    break;
                case "shift":
                    modifiers |= Win32Interop.MOD_SHIFT;
                    break;
                case "win":
                case "windows":
                    modifiers |= Win32Interop.MOD_WIN;
                    break;
                default:
                    return false;
            }
        }

        var keyPart = parts[^1].ToUpperInvariant();
        if (keyPart.Length == 1 && keyPart[0] >= 'A' && keyPart[0] <= 'Z')
        {
            virtualKey = (uint)keyPart[0];
            return true;
        }

        if (keyPart.Length == 1 && keyPart[0] >= '0' && keyPart[0] <= '9')
        {
            virtualKey = (uint)keyPart[0];
            return true;
        }

        virtualKey = keyPart switch
        {
            "SPACE" => 0x20,
            "ENTER" or "RETURN" => 0x0D,
            "ESCAPE" or "ESC" => 0x1B,
            "TAB" => 0x09,
            "BACKSPACE" => 0x08,
            "F1" => 0x70,
            "F2" => 0x71,
            "F3" => 0x72,
            "F4" => 0x73,
            "F5" => 0x74,
            "F6" => 0x75,
            "F7" => 0x76,
            "F8" => 0x77,
            "F9" => 0x78,
            "F10" => 0x79,
            "F11" => 0x7A,
            "F12" => 0x7B,
            _ => 0
        };

        return virtualKey != 0;
    }
}
