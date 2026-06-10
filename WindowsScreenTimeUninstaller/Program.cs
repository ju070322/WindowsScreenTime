using System.Diagnostics;
using Microsoft.Win32;

namespace WindowsScreenTimeUninstaller;

static class Program
{
    private const string AppName = "Windows Screen Time";
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WindowsScreenTime";

    [STAThread]
    static int Main()
    {
        ApplicationConfiguration.Initialize();

        var confirm = MessageBox.Show(
            "确定要卸载 Windows Screen Time 吗？\r\n\r\n程序文件和快捷方式会被删除，统计数据默认保留在本地用户数据目录。",
            AppName,
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.OK)
        {
            return 0;
        }

        try
        {
            var installDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            StopRunningApp();
            DeleteShortcuts();
            DeleteRegistryEntries();
            ScheduleDirectoryRemoval(installDir);
            MessageBox.Show("卸载完成。统计数据已保留。", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"卸载失败：{ex.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    private static void StopRunningApp()
    {
        foreach (var process in Process.GetProcessesByName("WindowsScreenTime"))
        {
            try
            {
                process.CloseMainWindow();
                if (!process.WaitForExit(3000))
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(3000);
                }
            }
            catch
            {
                // Continue cleanup; locked files will be removed after reboot or manual retry.
            }
        }
    }

    private static void DeleteShortcuts()
    {
        File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Windows Screen Time.lnk"));
        File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Windows Screen Time.lnk"));
    }

    private static void DeleteRegistryEntries()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        runKey?.DeleteValue("WindowsScreenTime", false);

        using var uninstallRoot = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true);
        uninstallRoot?.DeleteSubKeyTree("WindowsScreenTime", false);
    }

    private static void ScheduleDirectoryRemoval(string installDir)
    {
        var script = Path.Combine(Path.GetTempPath(), $"WindowsScreenTime-uninstall-{Guid.NewGuid():N}.cmd");
        File.WriteAllText(script, $"""
@echo off
timeout /t 2 /nobreak >nul
rmdir /s /q "{installDir}" >nul 2>nul
del "%~f0" >nul 2>nul
""");
        Process.Start(new ProcessStartInfo
        {
            FileName = script,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            UseShellExecute = true
        });
    }
}
