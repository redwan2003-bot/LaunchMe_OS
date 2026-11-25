using System.Runtime.InteropServices;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.Contracts.Voice;
using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Voice;

/// <summary>
/// Injects transcribed speech text directly into the currently focused window using SendInput.
/// </summary>
public class Win32KeystrokeInjector : IKeystrokeInjector
{
    private readonly IClipboardWriter? _clipboardWriter;

    public Win32KeystrokeInjector(IClipboardWriter? clipboardWriter = null)
    {
        _clipboardWriter = clipboardWriter;
    }

    public void InjectText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (text.Length > 10 && _clipboardWriter != null)
        {
            // Clipboard paste pathway (Ctrl+V) for long phrases to ensure instant, atomic typing
            _clipboardWriter.SetText(text);

            var inputs = new Win32Interop.INPUT[4];
            int inputSize = Marshal.SizeOf<Win32Interop.INPUT>();

            // Ctrl down
            inputs[0].type = Win32Interop.INPUT_KEYBOARD;
            inputs[0].ki.wVk = Win32Interop.VK_CONTROL;

            // V down
            inputs[1].type = Win32Interop.INPUT_KEYBOARD;
            inputs[1].ki.wVk = Win32Interop.VK_V;

            // V up
            inputs[2].type = Win32Interop.INPUT_KEYBOARD;
            inputs[2].ki.wVk = Win32Interop.VK_V;
            inputs[2].ki.dwFlags = Win32Interop.KEYEVENTF_KEYUP;

            // Ctrl up
            inputs[3].type = Win32Interop.INPUT_KEYBOARD;
            inputs[3].ki.wVk = Win32Interop.VK_CONTROL;
            inputs[3].ki.dwFlags = Win32Interop.KEYEVENTF_KEYUP;

            Win32Interop.SendInput((uint)inputs.Length, inputs, inputSize);
        }
        else
        {
            // Direct Unicode keystroke injection pathway
            var inputs = new Win32Interop.INPUT[text.Length * 2];
            int inputSize = Marshal.SizeOf<Win32Interop.INPUT>();

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                int idx = i * 2;

                // Key down
                inputs[idx].type = Win32Interop.INPUT_KEYBOARD;
                inputs[idx].ki.wScan = c;
                inputs[idx].ki.dwFlags = Win32Interop.KEYEVENTF_UNICODE;

                // Key up
                inputs[idx + 1].type = Win32Interop.INPUT_KEYBOARD;
                inputs[idx + 1].ki.wScan = c;
                inputs[idx + 1].ki.dwFlags = Win32Interop.KEYEVENTF_UNICODE | Win32Interop.KEYEVENTF_KEYUP;
            }

            Win32Interop.SendInput((uint)inputs.Length, inputs, inputSize);
        }
    }
}
