using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WindowsScreenTimeSetupBuilder;

public sealed class SetupForm : Form
{
    private const string AppName = "Windows Screen Time";
    private const string FolderName = "WindowsScreenTime";
    private const string AppVersion = "1.4.2";
    private const string Publisher = "\u54c8\u547c\u547c\u5417";
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WindowsScreenTime";

    private readonly TextBox _installPath = new();
    private readonly ProgressBar _progress = new();
    private readonly Label _status = new();
    private readonly Button _installButton = new();
    private readonly Button _browseButton = new();
    private readonly Button _cancelButton = new();
    private readonly CheckBox _launchAfterInstall = new();
    private readonly Panel _finishOptions = new();

    private string? _installedExePath;
    private string? _installedDir;
    private bool _installCompleted;
    private bool _finishCloseHandled;

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

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_installCompleted && !_finishCloseHandled)
        {
            _finishCloseHandled = true;
            if (_launchAfterInstall.Checked)
            {
                LaunchInstalledApp();
            }
        }

        base.OnFormClosing(e);
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

        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            Text = $"v{AppVersion}\r\n\u4f5c\u8005\uff1a{Publisher}",
            ForeColor = Color.FromArgb(148, 163, 184),
            TextAlign = ContentAlignment.BottomLeft
        });
        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            Height = 120,
            Text = "\u5b89\u88c5\u5230 C \u76d8 Program Files\u3002\r\n\u4f1a\u663e\u793a\u5728 Windows \u7a0b\u5e8f\u548c\u529f\u80fd\u4e2d\uff0c\u53ef\u4ece\u7cfb\u7edf\u8bbe\u7f6e\u5378\u8f7d\u3002",
            ForeColor = Color.FromArgb(203, 213, 225),
            TextAlign = ContentAlignment.TopLeft
        });
        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            Height = 96,
            Text = "Windows\r\nScreen Time",
            Font = new Font(Font.FontFamily, 20F, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        });
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
            Text = "\u51c6\u5907\u5b89\u88c5",
            Font = new Font(Font.FontFamily, 22F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "\u5b89\u88c5\u4f4d\u7f6e",
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
        _browseButton.Text = "\u6d4f\u89c8";
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
        _status.Text = "\u70b9\u51fb\u5b89\u88c5\u5373\u53ef\u5f00\u59cb\u3002";
        _status.ForeColor = Color.FromArgb(71, 85, 105);
        var progressHost = new Panel { Dock = DockStyle.Fill, BackColor = panel.BackColor };
        progressHost.Controls.Add(_progress);
        progressHost.Controls.Add(_status);
        layout.Controls.Add(progressHost, 0, 3);

        _finishOptions.Dock = DockStyle.Fill;
        _finishOptions.BackColor = panel.BackColor;
        layout.Controls.Add(_finishOptions, 0, 4);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = panel.BackColor
        };
        _installButton.Text = "\u5b89\u88c5 / \u66f4\u65b0";
        _installButton.Width = 120;
        _installButton.Height = 34;
        _installButton.BackColor = Color.FromArgb(37, 99, 235);
        _installButton.ForeColor = Color.White;
        _installButton.FlatStyle = FlatStyle.Flat;
        _installButton.FlatAppearance.BorderColor = Color.FromArgb(37, 99, 235);
        _installButton.Click += async (_, _) => await InstallOrFinishAsync();

        _cancelButton.Text = "\u53d6\u6d88";
        _cancelButton.Width = 90;
        _cancelButton.Height = 34;
        _cancelButton.FlatStyle = FlatStyle.System;
        _cancelButton.Click += (_, _) => Close();

        buttons.Controls.Add(_installButton);
        buttons.Controls.Add(_cancelButton);
        layout.Controls.Add(buttons, 0, 5);
        return panel;
    }

    private async Task InstallOrFinishAsync()
    {
        if (_installCompleted)
        {
            Close();
            return;
        }

        await InstallAsync();
    }

    private async Task InstallAsync()
    {
        SetBusy(true);
        try
        {
            string exePath = string.Empty;
            string installDir = string.Empty;

            await Task.Run(() =>
            {
                UpdateProgress(8, "\u6b63\u5728\u5173\u95ed\u65e7\u7248\u672c...");
                StopRunningApp();

                installDir = _installPath.Text.Trim();
                Directory.CreateDirectory(installDir);

                UpdateProgress(28, "\u6b63\u5728\u590d\u5236\u7a0b\u5e8f\u6587\u4ef6...");
                Extract("payload.WindowsScreenTime.exe", Path.Combine(installDir, "WindowsScreenTime.exe"));
                Extract("payload.WindowsScreenTimeUninstall.exe", Path.Combine(installDir, "WindowsScreenTimeUninstall.exe"));
                Extract("payload.app.ico", Path.Combine(installDir, "app.ico"));
                Extract("payload.app-icon.png", Path.Combine(installDir, "app-icon.png"));
                Extract("payload.README.md", Path.Combine(installDir, "README.md"));

                exePath = Path.Combine(installDir, "WindowsScreenTime.exe");
                var uninstallerPath = Path.Combine(installDir, "WindowsScreenTimeUninstall.exe");
                var iconPath = Path.Combine(installDir, "app.ico");

                UpdateProgress(58, "\u6b63\u5728\u521b\u5efa\u5feb\u6377\u65b9\u5f0f...");
                CreateShortcut(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), "Windows Screen Time.lnk"),
                    exePath,
                    installDir,
                    iconPath);
                CreateShortcut(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Windows Screen Time.lnk"),
                    exePath,
                    installDir,
                    iconPath);

                UpdateProgress(82, "\u6b63\u5728\u6ce8\u518c\u5378\u8f7d\u4fe1\u606f...");
                RegisterUninstallInfo(installDir, exePath, uninstallerPath, iconPath);
            });

            _installedExePath = exePath;
            _installedDir = installDir;
            _installCompleted = true;
            UpdateProgress(100, "\u5b89\u88c5/\u66f4\u65b0\u5b8c\u6210\u3002\u53ef\u9009\u62e9\u5173\u95ed\u540e\u662f\u5426\u542f\u52a8\u5e94\u7528\u3002");
            SetFinished();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"\u5b89\u88c5\u5931\u8d25\uff1a{ex.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateProgress(0, "\u5b89\u88c5\u5931\u8d25\u3002");
            SetBusy(false);
        }
    }

    private void LaunchInstalledApp()
    {
        if (string.IsNullOrWhiteSpace(_installedExePath) || string.IsNullOrWhiteSpace(_installedDir))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = _installedExePath,
            WorkingDirectory = _installedDir,
            UseShellExecute = true
        });
    }

    private void BrowseInstallPath()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "\u9009\u62e9\u5b89\u88c5\u76ee\u5f55",
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

    private void SetFinished()
    {
        _installButton.Text = "\u5b8c\u6210";
        _installButton.Enabled = true;
        _browseButton.Enabled = false;
        _cancelButton.Enabled = true;
        _cancelButton.Text = "\u5173\u95ed";
        _installPath.Enabled = false;
        ShowLaunchAfterInstallOption();
    }

    private void ShowLaunchAfterInstallOption()
    {
        if (_launchAfterInstall.Parent is not null)
        {
            return;
        }

        _launchAfterInstall.Text = "\u5173\u95ed\u540e\u542f\u52a8\u5e94\u7528";
        _launchAfterInstall.Checked = true;
        _launchAfterInstall.AutoSize = true;
        _launchAfterInstall.ForeColor = Color.FromArgb(51, 65, 85);
        _launchAfterInstall.Location = new Point(0, 10);
        _launchAfterInstall.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _finishOptions.Controls.Add(_launchAfterInstall);
        _launchAfterInstall.BringToFront();
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
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), FolderName);

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

    private static void RegisterUninstallInfo(string installDir, string exePath, string uninstallerPath, string iconPath)
    {
        using var key = Registry.LocalMachine.CreateSubKey(RegistryKeyPath, true)
            ?? throw new InvalidOperationException("\u65e0\u6cd5\u5199\u5165\u5378\u8f7d\u6ce8\u518c\u8868\u3002");
        key.SetValue("DisplayName", AppName);
        key.SetValue("DisplayVersion", AppVersion);
        key.SetValue("Publisher", Publisher);
        key.SetValue("InstallLocation", installDir);
        key.SetValue("DisplayIcon", iconPath);
        key.SetValue("UninstallString", $"\"{uninstallerPath}\"");
        key.SetValue("QuietUninstallString", $"\"{uninstallerPath}\" /quiet");
        key.SetValue("URLInfoAbout", "https://github.com/ju070322/WindowsScreenTime");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        key.SetValue("EstimatedSize", EstimateInstalledSizeKb(installDir), RegistryValueKind.DWord);
    }

    private static int EstimateInstalledSizeKb(string installDir)
    {
        try
        {
            return (int)(Directory.EnumerateFiles(installDir, "*", SearchOption.AllDirectories)
                .Sum(path => new FileInfo(path).Length) / 1024);
        }
        catch
        {
            return 0;
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
