using System.Runtime.InteropServices;

namespace Vigil.Native;

public static class Win32
{
    [Flags]
    public enum ExecutionState : uint
    {
        EsSystemRequired = 0x00000001,
        EsDisplayRequired = 0x00000002,
        EsUserPresent = 0x00000004,
        EsAwayModeRequired = 0x00000040,
        EsContinuous = 0x80000000
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

    public const int WmSysCommand = 0x0112;
    public const int ScMonitorPower = 0xF170;
    public const int MonitorOff = 2;
    public const int MonitorOn = -1;
    public static readonly IntPtr HwndBroadcast = new(0xffff);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

    public const uint MouseEventfMove = 0x0001;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtSuspendProcess(IntPtr processHandle);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtResumeProcess(IntPtr processHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AttachConsole(int dwProcessId);

    public const int AttachParentProcess = -1;

    public const uint ProcessSuspendResume = 0x0800;
    public const uint ProcessQueryLimitedInformation = 0x1000;
}
