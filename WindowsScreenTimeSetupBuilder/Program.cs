using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WindowsScreenTimeSetupBuilder;

static class Program
{
    private const string AppName = "Windows Screen Time";
    private const string FolderName = "WindowsScreenTime";

    [STAThread]
    static int Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var installDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                FolderName);
            Directory.CreateDirectory(installDir);

            StopRunningApp();

            Extract("payload.WindowsScreenTime.exe", Path.Combine(installDir, "WindowsScreenTime.exe"));
            Extract("payload.app.ico", Path.Combine(installDir, "app.ico"));
            Extract("payload.app-icon.png", Path.Combine(installDir, "app-icon.png"));
            Extract("payload.README.md", Path.Combine(installDir, "README.md"));
            Extract("payload.uninstall.cmd", Path.Combine(installDir, "uninstall.cmd"));

            var exePath = Path.Combine(installDir, "WindowsScreenTime.exe");
            var iconPath = Path.Combine(installDir, "app.ico");
            CreateShortcut(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Windows Screen Time.lnk"),
                exePath,
                installDir,
                iconPath);
            CreateShortcut(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Windows Screen Time.lnk"),
                exePath,
                installDir,
                iconPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = installDir,
                UseShellExecute = true
            });

            MessageBox.Show($"{AppName} 安装/更新完成。旧版本数据已保留。", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"安装失败：{ex.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                // Continue installation; file replacement will report a clear error if the old app is still locked.
            }
        }
    }

    private static void Extract(string resourceName, string outputPath)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing resource: {resourceName}");
        using var file = File.Create(outputPath);
        stream.CopyTo(file);
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string iconPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell is unavailable.");
        var shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Could not create WScript.Shell.");

        try
        {
            dynamic shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                null,
                shell,
                [shortcutPath])!;
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = workingDirectory;
            shortcut.IconLocation = iconPath;
            shortcut.Save();
            Marshal.FinalReleaseComObject(shortcut);
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell);
        }
    }
}
