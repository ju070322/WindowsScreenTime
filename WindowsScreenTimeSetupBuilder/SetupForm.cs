using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WindowsScreenTimeSetupBuilder;

public sealed class SetupForm : Form
{
    private const string AppName = "Windows Screen Time";
    private const string FolderName = "WindowsScreenTime";

    private readonly TextBox _installPath = new();
    private readonly ProgressBar _progress = new();
    private readonly Label _status = new();
    private readonly Button _installButton = new();
    private readonly Button _browseButton = new();
    private readonly Button _cancelButton = new();

    public SetupForm()
    {
        Text = "Windows Screen Time Setup";
        ClientSize = new Size(720, 430);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(246, 248, 252);
        Font = new Font("Microsoft YaHei UI", 9F);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;

        BuildLayout();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = BackColor
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 236));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildHero(), 0, 0);
        root.Controls.Add(BuildInstallerPanel(), 1, 0);
    }

    private Control BuildHero()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(17, 24, 39),
            Padding = new Padding(24)
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 96,
            Text = "Windows\r\nScreen Time",
            Font = new Font(Font.FontFamily, 20F, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var desc = new Label
        {
            Dock = DockStyle.Top,
            Height = 120,
            Text = "安装或更新应用。\r\n旧版本数据会保留在本地用户数据目录。",
            ForeColor = Color.FromArgb(203, 213, 225),
            TextAlign = ContentAlignment.TopLeft
        };
        var version = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            Text = "v1.3.0\r\n作者：哈呼呼吗",
            ForeColor = Color.FromArgb(148, 163, 184),
            TextAlign = ContentAlignment.BottomLeft
        };
        panel.Controls.Add(version);
        panel.Controls.Add(desc);
        panel.Controls.Add(title);
        return panel;
    }

    private Control BuildInstallerPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(32),
            BackColor = Color.FromArgb(246, 248, 252)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = panel.BackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        panel.Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "准备安装",
            Font = new Font(Font.FontFamily, 22F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "安装位置",
            ForeColor = Color.FromArgb(71, 85, 105),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);

        var pathRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = panel.BackColor
        };
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));

        _installPath.Dock = DockStyle.Fill;
        _installPath.Text = DefaultInstallDir();
        _installPath.BorderStyle = BorderStyle.FixedSingle;
        _browseButton.Text = "浏览";
        _browseButton.Dock = DockStyle.Fill;
        _browseButton.FlatStyle = FlatStyle.System;
        _browseButton.Click += (_, _) => BrowseInstallPath();
        pathRow.Controls.Add(_installPath, 0, 0);
        pathRow.Controls.Add(_browseButton, 1, 0);
        layout.Controls.Add(pathRow, 0, 2);

        _progress.Dock = DockStyle.Bottom;
        _progress.Height = 18;
        _progress.Style = ProgressBarStyle.Continuous;
        _status.Dock = DockStyle.Top;
        _status.Height = 30;
        _status.Text = "点击安装即可开始。";
        _status.ForeColor = Color.FromArgb(71, 85, 105);
        var progressHost = new Panel { Dock = DockStyle.Fill, BackColor = panel.BackColor };
        progressHost.Controls.Add(_progress);
        progressHost.Controls.Add(_status);
        layout.Controls.Add(progressHost, 0, 3);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = panel.BackColor
        };
        _installButton.Text = "安装 / 更新";
        _installButton.Width = 120;
        _installButton.Height = 34;
        _installButton.BackColor = Color.FromArgb(37, 99, 235);
        _installButton.ForeColor = Color.White;
        _installButton.FlatStyle = FlatStyle.Flat;
        _installButton.FlatAppearance.BorderColor = Color.FromArgb(37, 99, 235);
        _installButton.Click += async (_, _) => await InstallAsync();

        _cancelButton.Text = "取消";
        _cancelButton.Width = 90;
        _cancelButton.Height = 34;
        _cancelButton.FlatStyle = FlatStyle.System;
        _cancelButton.Click += (_, _) => Close();

        buttons.Controls.Add(_installButton);
        buttons.Controls.Add(_cancelButton);
        layout.Controls.Add(buttons, 0, 5);
        return panel;
    }

    private async Task InstallAsync()
    {
        SetBusy(true);
        try
        {
            await Task.Run(() =>
            {
                UpdateProgress(8, "正在关闭旧版本...");
                StopRunningApp();

                var installDir = _installPath.Text.Trim();
                Directory.CreateDirectory(installDir);

                UpdateProgress(28, "正在复制程序文件...");
                Extract("payload.WindowsScreenTime.exe", Path.Combine(installDir, "WindowsScreenTime.exe"));
                Extract("payload.app.ico", Path.Combine(installDir, "app.ico"));
                Extract("payload.app-icon.png", Path.Combine(installDir, "app-icon.png"));
                Extract("payload.README.md", Path.Combine(installDir, "README.md"));
                Extract("payload.uninstall.cmd", Path.Combine(installDir, "uninstall.cmd"));

                UpdateProgress(70, "正在创建快捷方式...");
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

                UpdateProgress(92, "正在启动应用...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = installDir,
                    UseShellExecute = true
                });
            });

            UpdateProgress(100, "安装/更新完成。旧版本数据已保留。");
            MessageBox.Show("安装/更新完成。", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"安装失败：{ex.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateProgress(0, "安装失败。");
            SetBusy(false);
        }
    }

    private void BrowseInstallPath()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择安装目录",
            SelectedPath = _installPath.Text
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _installPath.Text = dialog.SelectedPath;
        }
    }

    private void SetBusy(bool busy)
    {
        _installButton.Enabled = !busy;
        _browseButton.Enabled = !busy;
        _cancelButton.Enabled = !busy;
        _installPath.Enabled = !busy;
    }

    private void UpdateProgress(int value, string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateProgress(value, text));
            return;
        }

        _progress.Value = Math.Clamp(value, 0, 100);
        _status.Text = text;
    }

    private static string DefaultInstallDir() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", FolderName);

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
                // File replacement will surface a clear error if the old app is still locked.
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
