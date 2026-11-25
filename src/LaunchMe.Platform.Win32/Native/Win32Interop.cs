using System.Runtime.InteropServices;

namespace LaunchMe.Platform.Win32.Native;

public static class Win32Interop
{
    // Window Affinity
    public const uint WDA_NONE = 0x00000000;
    public const uint WDA_MONITOR = 0x00000001;
    public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    // Window Messages & Hooks
    public const int WH_MOUSE_LL = 14;
    public const int WM_MOUSEMOVE = 0x0200;
    public const int WM_HOTKEY = 0x0312;
    public const int WM_DESTROY = 0x0002;
    public const int WM_CLOSE = 0x0010;
    public const uint WM_APP = 0x8000;
    public const uint WM_LBUTTONUP = 0x0202;
    public const uint WM_LBUTTONDBLCLK = 0x0203;
    public const uint WM_RBUTTONUP = 0x0205;
    public const uint WS_POPUP = 0x80000000;

    // Clipboard & Global Memory
    public const uint CF_UNICODETEXT = 13;
    public const uint CF_HDROP = 15;
    public const uint GMEM_MOVEABLE = 0x0002;

    // System Tray & Menus
    public const uint NIM_ADD = 0x00000000;
    public const uint NIM_MODIFY = 0x00000001;
    public const uint NIM_DELETE = 0x00000002;
    public const uint NIF_MESSAGE = 0x00000001;
    public const uint NIF_ICON = 0x00000002;
    public const uint NIF_TIP = 0x00000004;
    public const int IDI_APPLICATION = 32512;
    public const uint MF_STRING = 0x00000000;
    public const uint MF_SEPARATOR = 0x00000800;
    public const uint TPM_RIGHTBUTTON = 0x0002;
    public const uint TPM_RETURNCMD = 0x0100;

    // Hotkey Modifiers
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    // AppBar Messages
    public const uint ABM_NEW = 0x00000000;
    public const uint ABM_REMOVE = 0x00000001;
    public const uint ABM_QUERYPOS = 0x00000002;
    public const uint ABM_SETPOS = 0x00000003;
    public const uint ABM_GETSTATE = 0x00000004;
    public const uint ABM_GETTASKBARPOS = 0x00000005;
    public const uint ABM_ACTIVATE = 0x00000006;
    public const uint ABM_GETAUTOHIDEBAR = 0x00000007;
    public const uint ABM_SETAUTOHIDEBAR = 0x00000008;
    public const uint ABM_WINDOWPOSCHANGED = 0x00000009;
    public const uint ABM_SETSTATE = 0x0000000A;

    public const uint ABE_LEFT = 0;
    public const uint ABE_TOP = 1;
    public const uint ABE_RIGHT = 2;
    public const uint ABE_BOTTOM = 3;

    public const uint ABS_AUTOHIDE = 0x0000001;
    public const uint ABS_ALWAYSONTOP = 0x0000002;

    // DWM
    public const uint DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    // Monitor flags
    public const uint MONITOR_DEFAULTTONULL = 0;
    public const uint MONITOR_DEFAULTTOPRIMARY = 1;
    public const uint MONITOR_DEFAULTTONEAREST = 2;

    // Virtual Screen Metrics
    public const int SM_XVIRTUALSCREEN = 76;
    public const int SM_YVIRTUALSCREEN = 77;
    public const int SM_CXVIRTUALSCREEN = 78;
    public const int SM_CYVIRTUALSCREEN = 79;
    public const int SM_CMONITORS = 80;

    // Shell Change Notifications
    public const int SHCNE_ASSOCCHANGED = 0x08000000;
    public const uint SHCNF_IDLIST = 0x0000;

    public static readonly nint HWND_MESSAGE = new(-3);

    public delegate nint HookProc(int nCode, nint wParam, nint lParam);
    public delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);
    public delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, ref RECT lprcMonitor, nint dwData);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct APPBARDATA
    {
        public uint cbSize;
        public nint hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RECT rc;
        public nint lParam;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public WndProc lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;

        public readonly ulong ToUInt64() => ((ulong)dwHighDateTime << 32) | dwLowDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MIB_IF_ROW2
    {
        public ulong InterfaceLuid;
        public uint InterfaceIndex;
        public Guid InterfaceGuid;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
        public string Alias;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
        public string Description;

        public uint PhysicalAddressLength;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] PhysicalAddress;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] PermanentPhysicalAddress;

        public uint Mtu;
        public uint Type;
        public int TunnelType;
        public int MediaType;
        public int PhysicalMediumType;
        public int AccessType;
        public int DirectionType;
        public byte InterfaceAndOperStatusFlags;
        public byte Padding1;
        public byte Padding2;
        public byte Padding3;
        public int OperStatus;
        public int AdminStatus;
        public int MediaConnectState;
        public Guid NetworkGuid;
        public int ConnectionType;
        public int Padding4;

        public ulong TransmitLinkSpeed;
        public ulong ReceiveLinkSpeed;
        public ulong InOctets;
        public ulong InUcastPkts;
        public ulong InNUcastPkts;
        public ulong InDiscards;
        public ulong InErrors;
        public ulong InUnknownProtos;
        public ulong InUcastOctets;
        public ulong InMulticastOctets;
        public ulong InBroadcastOctets;
        public ulong OutOctets;
        public ulong OutUcastPkts;
        public ulong OutNUcastPkts;
        public ulong OutDiscards;
        public ulong OutErrors;
        public ulong OutUcastOctets;
        public ulong OutMulticastOctets;
        public ulong OutBroadcastOctets;
        public ulong OutQLen;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DROPFILES
    {
        public uint pFiles;
        public POINT pt;
        public int fNC;
        public int fWide;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NOTIFYICONDATAW
    {
        public uint cbSize;
        public nint hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public nint hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string? szTip;

        public uint dwState;
        public uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string? szInfo;

        public uint uTimeoutOrVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string? szInfoTitle;

        public uint dwInfoFlags;
        public Guid guidItem;
        public nint hBalloonIcon;
    }

    public enum QUERY_USER_NOTIFICATION_STATE
    {
        QUNS_NOT_PRESENT = 1,
        QUNS_BUSY = 2,
        QUNS_RUNNING_D_3D_FULL_SCREEN = 3,
        QUNS_PRESENTATION_MODE = 4,
        QUNS_ACCEPTS_NOTIFICATIONS = 5,
        QUNS_QUIET_TIME = 6,
        QUNS_APP = 7
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SetWindowsHookExW(int idHook, HookProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    public static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint GetModuleHandleW(string? lpModuleName);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(nint hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowDisplayAffinity(nint hWnd, uint dwAffinity);

    [DllImport("shell32.dll", SetLastError = true)]
    public static extern int SHQueryUserNotificationState(out QUERY_USER_NOTIFICATION_STATE pquns);

    [DllImport("shell32.dll", SetLastError = true)]
    public static extern nint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern nint FindWindowW(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern ushort RegisterClassExW(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterClassW(string lpClassName, nint hInstance);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern nint CreateWindowExW(
        uint dwExStyle,
        string lpClassName,
        string lpWindowName,
        uint dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        nint hWndParent,
        nint hMenu,
        nint hInstance,
        nint lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyWindow(nint hWnd);

    [DllImport("user32.dll")]
    public static extern nint DefWindowProcW(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    public static extern int GetMessageW(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    public static extern nint DispatchMessageW(ref MSG lpMsg);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessageW(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetSystemTimes(
        out FILETIME lpIdleTime,
        out FILETIME lpKernelTime,
        out FILETIME lpUserTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern int GetIfTable2(out nint ppIfTable);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern void FreeMibTable(nint pIfTable);

    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(nint hwnd, uint dwAttribute, out RECT pvAttribute, uint cbAttribute);

    [DllImport("user32.dll")]
    public static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int GetClassNameW(nint hWnd, [Out] System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(nint hWnd);

    [DllImport("user32.dll")]
    public static extern nint GetShellWindow();

    [DllImport("user32.dll")]
    public static extern nint GetDesktopWindow();

    [DllImport("user32.dll")]
    public static extern void PostQuitMessage(int nExitCode);

    // Clipboard & Global Memory
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool OpenClipboard(nint hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SetClipboardData(uint uFormat, nint hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint GlobalAlloc(uint uFlags, nuint dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint GlobalLock(nint hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GlobalUnlock(nint hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint GlobalFree(nint hMem);

    // System Tray & Menus
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Shell_NotifyIconW(uint dwMessage, ref NOTIFYICONDATAW lpData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint LoadIconW(nint hInstance, nint lpIconName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AppendMenuW(nint hMenu, uint uFlags, nuint uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int TrackPopupMenu(nint hMenu, uint uFlags, int x, int y, int nReserved, nint hWnd, nint prcRect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyMenu(nint hMenu);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    public static extern uint GetDpiForSystem();

    [DllImport("shell32.dll")]
    public static extern void SHChangeNotify(int wEventId, uint uFlags, nint dwItem1, nint dwItem2);

    // Raw Input
    public const uint WM_INPUT = 0x00FF;
    public const uint RID_INPUT = 0x10000003;
    public const uint RIDEV_INPUTSINK = 0x00000100;
    public const uint RIDEV_REMOVE = 0x00000001;
    public const ushort HID_USAGE_PAGE_DIGITIZER = 0x0D;
    public const ushort HID_USAGE_DIGITIZER_TOUCH_PAD = 0x05;
    public const ushort HID_USAGE_DIGITIZER_TOUCH_SCREEN = 0x04;

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public nint hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public nint hDevice;
        public nint wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTDEVICELIST
    {
        public nint hDevice;
        public uint dwType;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterRawInputDevices(
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] RAWINPUTDEVICE[] pRawInputDevices,
        uint uiNumDevices,
        uint cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetRawInputData(
        nint hRawInput,
        uint uiCommand,
        nint pData,
        ref uint pcbSize,
        uint cbSizeHeader);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetRawInputDeviceList(
        [Out] RAWINPUTDEVICELIST[]? pRawInputDeviceList,
        ref uint puiNumDevices,
        uint cbSize);
}
