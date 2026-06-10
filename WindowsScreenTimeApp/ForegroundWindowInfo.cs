using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowsScreenTimeApp;

public sealed record ForegroundWindowInfo(string AppName, string ExePath, string WindowTitle, int ProcessId, bool IsIdle)
{
    public string AppKey => IsIdle ? "Idle" : $"{AppName}|{ExePath}";

    public static ForegroundWindowInfo Idle() =>
        new("Idle", "", "无键盘或鼠标活动", 0, true);
}

public static class ForegroundWindowReader
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LastInputInfo plii);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("kernel32.dll")]
    private static extern uint GetTickCount();

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    public static ForegroundWindowInfo Read(TimeSpan idleLimit)
    {
        if (GetIdleTime() >= idleLimit)
        {
            return ForegroundWindowInfo.Idle();
        }

        var handle = GetForegroundWindow();
        if (handle == IntPtr.Zero)
        {
            return new ForegroundWindowInfo("Unknown", "", "", 0, false);
        }

        return ReadWindow(handle, false);
    }

    public static IReadOnlyList<ForegroundWindowInfo> ReadVisibleApps()
    {
        var apps = new Dictionary<string, ForegroundWindowInfo>();
        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle) || GetWindowTextLength(handle) <= 0)
            {
                return true;
            }

            var info = ReadWindow(handle, false);
            if (info.ProcessId > 0 && !string.IsNullOrWhiteSpace(info.AppName) && !apps.ContainsKey(info.AppKey))
            {
                apps[info.AppKey] = info;
            }

            return true;
        }, IntPtr.Zero);

        return apps.Values.ToList();
    }

    private static ForegroundWindowInfo ReadWindow(IntPtr handle, bool isIdle)
    {
        GetWindowThreadProcessId(handle, out var pid);
        var title = ReadWindowTitle(handle);
        try
        {
            using var process = Process.GetProcessById((int)pid);
            var exePath = "";
            try
            {
                exePath = process.MainModule?.FileName ?? "";
            }
            catch
            {
                exePath = "";
            }

            var appName = !string.IsNullOrWhiteSpace(process.ProcessName)
                ? process.ProcessName
                : Path.GetFileNameWithoutExtension(exePath);

            return new ForegroundWindowInfo(appName, exePath, title, (int)pid, isIdle);
        }
        catch
        {
            return new ForegroundWindowInfo("Unknown", "", title, (int)pid, isIdle);
        }
    }

    private static string ReadWindowTitle(IntPtr handle)
    {
        var length = GetWindowTextLength(handle);
        if (length <= 0)
        {
            return "";
        }

        var builder = new StringBuilder(length + 1);
        GetWindowText(handle, builder, builder.Capacity);
        return builder.ToString();
    }

    private static TimeSpan GetIdleTime()
    {
        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref info))
        {
            return TimeSpan.Zero;
        }

        var milliseconds = GetTickCount() - info.Time;
        return TimeSpan.FromMilliseconds(milliseconds);
    }
}
