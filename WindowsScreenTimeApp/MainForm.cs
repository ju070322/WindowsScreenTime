using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace WindowsScreenTimeApp;

public sealed class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly ActivityTracker _tracker;
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _appIcon;
    private readonly Label _totalValue = new();
    private readonly Label _foregroundValue = new();
    private readonly Label _backgroundValue = new();
    private readonly Label _currentValue = new();
    private readonly Label _periodLabel = new();
    private readonly BarChartPanel _chart = new();
    private readonly ListView _usageList = new();
    private readonly Label _emptyState = new();
    private readonly Button _dayButton;
    private readonly Button _weekButton;
    private bool _reallyExit;

    public MainForm()
    {
        Text = $"Windows Screen Time {AppInfo.Version}";
        MinimumSize = new Size(1040, 760);
        Size = new Size(1220, 820);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Page;
        Font = new Font("Microsoft YaHei UI", 9F);
        _appIcon = LoadAppIcon();
        Icon = _appIcon;

        _settings = AppSettings.Load();
        _tracker = new ActivityTracker(_settings);
        _tracker.Updated += (_, _) => RefreshData();
        _notifyIcon = CreateNotifyIcon();

        _dayButton = MakeButton("今日", (_, _) => SetPeriod(UsagePeriod.Day), true);
        _weekButton = MakeButton("本周", (_, _) => SetPeriod(UsagePeriod.Week), false);

        BuildLayout();
        _tracker.Start();

        if (_settings.StartMinimized || Environment.GetCommandLineArgs().Contains("--minimized"))
        {
            WindowState = FormWindowState.Minimized;
            Shown += (_, _) => HideToTray();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_reallyExit && _settings.MinimizeToTrayOnClose)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        _notifyIcon.Visible = false;
        _tracker.Dispose();
        _appIcon.Dispose();
        base.OnFormClosing(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
        {
            HideToTray();
        }
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(24),
            BackColor = Theme.Page
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 46));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildMetrics(), 0, 1);
        root.Controls.Add(BuildChartPanel(), 0, 2);
        root.Controls.Add(BuildUsagePanel(), 0, 3);
        root.Controls.Add(BuildFooter(), 0, 4);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Page
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 604));

        var titleStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Theme.Page
        };
        titleStack.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Windows Screen Time",
            Font = new Font(Font.FontFamily, 22F, FontStyle.Bold),
            ForeColor = Theme.Text,
            Margin = new Padding(0, 0, 0, 4)
        });
        _periodLabel.AutoSize = true;
        _periodLabel.ForeColor = Theme.Muted;
        titleStack.Controls.Add(_periodLabel);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Theme.Page,
            Padding = new Padding(0, 12, 0, 0)
        };
        actions.Controls.Add(MakeButton("设置", (_, _) => OpenSettings(), true));
        actions.Controls.Add(MakeButton("导入", (_, _) => ImportData(), false));
        actions.Controls.Add(MakeButton("导出", (_, _) => ExportData(), false));
        actions.Controls.Add(MakeButton("重置", (_, _) => ResetData(), false));
        actions.Controls.Add(_weekButton);
        actions.Controls.Add(_dayButton);

        header.Controls.Add(titleStack, 0, 0);
        header.Controls.Add(actions, 1, 0);
        return header;
    }

    private Control BuildMetrics()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Theme.Page
        };
        for (var i = 0; i < 4; i++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        grid.Controls.Add(MakeMetric("总时间", _totalValue), 0, 0);
        grid.Controls.Add(MakeMetric("前台时间", _foregroundValue), 1, 0);
        grid.Controls.Add(MakeMetric("后台时间", _backgroundValue), 2, 0);
        grid.Controls.Add(MakeMetric("当前应用", _currentValue), 3, 0);
        return grid;
    }

    private Control BuildChartPanel()
    {
        var panel = MakePanel();
        panel.Margin = new Padding(0, 0, 0, 18);
        panel.Padding = new Padding(18);
        _chart.Dock = DockStyle.Fill;
        panel.Controls.Add(_chart);
        return panel;
    }

    private Control BuildUsagePanel()
    {
        var panel = MakePanel();
        panel.Padding = new Padding(18);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "应用明细",
            Font = new Font(Font.FontFamily, 13F, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        _usageList.Dock = DockStyle.Fill;
        _usageList.View = View.Details;
        _usageList.FullRowSelect = true;
        _usageList.GridLines = false;
        _usageList.BorderStyle = BorderStyle.None;
        _usageList.BackColor = Color.White;
        _usageList.ForeColor = Theme.Text;
        _usageList.Columns.Add("应用", 210);
        _usageList.Columns.Add("前台", 110);
        _usageList.Columns.Add("后台", 110);
        _usageList.Columns.Add("总时间", 110);
        _usageList.Columns.Add("占比", 80);
        _usageList.Columns.Add("最近窗口标题", 510);

        _emptyState.Dock = DockStyle.Fill;
        _emptyState.Text = "统计几秒后会显示应用明细。";
        _emptyState.ForeColor = Theme.Muted;
        _emptyState.TextAlign = ContentAlignment.MiddleCenter;

        var listHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        listHost.Controls.Add(_usageList);
        listHost.Controls.Add(_emptyState);

        layout.Controls.Add(listHost, 0, 1);
        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Page
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));

        var author = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"版本 {AppInfo.Version}  ·  作者：{AppInfo.Author}\r\n项目地址：{AppInfo.ProjectUrl}",
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var openProject = MakeButton("打开项目", (_, _) => OpenProjectUrl(), false);
        openProject.Dock = DockStyle.Right;
        footer.Controls.Add(author, 0, 0);
        footer.Controls.Add(openProject, 1, 0);
        return footer;
    }

    private Control MakeMetric(string labelText, Label valueLabel)
    {
        var panel = MakePanel();
        panel.Margin = new Padding(0, 0, 12, 18);
        panel.Padding = new Padding(16, 12, 16, 12);

        var label = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = labelText,
            ForeColor = Theme.Muted
        };
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.Font = new Font(Font.FontFamily, 17F, FontStyle.Bold);
        valueLabel.ForeColor = Theme.Text;
        valueLabel.AutoEllipsis = true;
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;

        panel.Controls.Add(valueLabel);
        panel.Controls.Add(label);
        return panel;
    }

    private static Panel MakePanel() => new RoundedPanel
    {
        Dock = DockStyle.Fill,
        BackColor = Color.White,
        BorderColor = Theme.Line,
        Radius = 8
    };

    private static Button MakeButton(string text, EventHandler onClick, bool primary)
    {
        var button = new Button
        {
            Text = text,
            Width = 96,
            Height = 34,
            Margin = new Padding(8, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Theme.Accent : Color.White,
            ForeColor = primary ? Color.White : Theme.Text
        };
        button.FlatAppearance.BorderColor = primary ? Theme.Accent : Theme.Line;
        button.FlatAppearance.BorderSize = 1;
        button.Click += onClick;
        return button;
    }

    private NotifyIcon CreateNotifyIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("打开", null, (_, _) => ShowFromTray());
        menu.Items.Add("导出数据", null, (_, _) => ExportData());
        menu.Items.Add("导入数据", null, (_, _) => ImportData());
        menu.Items.Add("设置", null, (_, _) => OpenSettings());
        menu.Items.Add("重置统计", null, (_, _) => ResetData());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitApplication());

        var icon = new NotifyIcon
        {
            Text = "Windows Screen Time",
            Icon = _appIcon,
            Visible = true,
            ContextMenuStrip = menu
        };
        icon.DoubleClick += (_, _) => ShowFromTray();
        return icon;
    }

    private void RefreshData()
    {
        _totalValue.Text = UiFormat.Duration(_tracker.ActiveTotal);
        _foregroundValue.Text = UiFormat.Duration(_tracker.ForegroundTotal);
        _backgroundValue.Text = UiFormat.Duration(_tracker.BackgroundTotal);
        _currentValue.Text = _tracker.Current.AppName;
        _periodLabel.Text = _tracker.Period == UsagePeriod.Day
            ? "查看范围：今日使用时间"
            : "查看范围：本周使用时间";
        _notifyIcon.Text = ClipNotifyText($"Windows Screen Time\n当前：{_tracker.Current.AppName}\n总计：{UiFormat.Duration(_tracker.ActiveTotal)}");
        RefreshPeriodButtons();

        var items = _tracker.Items
            .Where(item => _settings.IncludeIdleInList || !item.IsIdle)
            .ToList();

        var activeItems = items.Where(item => !item.IsIdle).ToList();
        _chart.SetItems(activeItems);

        _usageList.BeginUpdate();
        _usageList.Items.Clear();
        foreach (var item in items)
        {
            var percent = _tracker.ActiveTotal.TotalSeconds <= 0 || item.IsIdle
                ? "0%"
                : $"{item.Duration.TotalSeconds / _tracker.ActiveTotal.TotalSeconds:P1}";
            var row = new ListViewItem(item.AppName);
            row.SubItems.Add(UiFormat.Duration(item.ForegroundDuration));
            row.SubItems.Add(UiFormat.Duration(item.BackgroundDuration));
            row.SubItems.Add(UiFormat.Duration(item.Duration));
            row.SubItems.Add(percent);
            row.SubItems.Add(item.WindowTitle);
            row.ForeColor = item.IsIdle ? Theme.Muted : Theme.Text;
            _usageList.Items.Add(row);
        }
        _usageList.EndUpdate();
        _emptyState.Visible = items.Count == 0;
        _usageList.Visible = items.Count > 0;
    }

    private void SetPeriod(UsagePeriod period)
    {
        _tracker.SetPeriod(period);
    }

    private void RefreshPeriodButtons()
    {
        StyleToggle(_dayButton, _tracker.Period == UsagePeriod.Day);
        StyleToggle(_weekButton, _tracker.Period == UsagePeriod.Week);
    }

    private static void StyleToggle(Button button, bool active)
    {
        button.BackColor = active ? Theme.Accent : Color.White;
        button.ForeColor = active ? Color.White : Theme.Text;
        button.FlatAppearance.BorderColor = active ? Theme.Accent : Theme.Line;
    }

    private void ExportData()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "导出使用数据",
            Filter = "Windows Screen Time 数据 (*.wstdata)|*.wstdata|JSON 文件 (*.json)|*.json",
            FileName = $"WindowsScreenTime-{DateTime.Now:yyyyMMdd-HHmm}.wstdata"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _tracker.ExportTo(dialog.FileName, _settings);
            MessageBox.Show("数据已导出。", "Windows Screen Time", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "Windows Screen Time", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportData()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "导入使用数据",
            Filter = "Windows Screen Time 数据 (*.wstdata;*.json)|*.wstdata;*.json|所有文件 (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var confirm = MessageBox.Show(
            "导入会把文件中的使用时间合并到当前统计，并继承导入文件里的设置。是否继续？",
            "Windows Screen Time",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.OK)
        {
            return;
        }

        try
        {
            var result = _tracker.ImportFrom(dialog.FileName);
            if (result.Settings is not null)
            {
                _settings.CopyFrom(result.Settings);
                StartupManager.SetEnabled(_settings.StartWithWindows);
                _settings.Save();
                _tracker.ApplySettings(_settings);
            }
            RefreshData();
            MessageBox.Show("数据已导入并合并。", "Windows Screen Time", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导入失败：{ex.Message}", "Windows Screen Time", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetData()
    {
        var confirm = MessageBox.Show(
            "确定要清空当前累计统计吗？这不会删除导出的备份文件。",
            "Windows Screen Time",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning);
        if (confirm == DialogResult.OK)
        {
            _tracker.Reset();
        }
    }

    private void OpenSettings()
    {
        using var dialog = new SettingsForm(_settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            StartupManager.SetEnabled(_settings.StartWithWindows);
            _settings.Save();
            _tracker.ApplySettings(_settings);
            RefreshData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存设置失败：{ex.Message}", "Windows Screen Time",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void OpenProjectUrl()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = AppInfo.ProjectUrl,
            UseShellExecute = true
        });
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
        _notifyIcon.Visible = true;
    }

    private void ShowFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _reallyExit = true;
        Close();
    }

    private static string ClipNotifyText(string text) =>
        text.Length <= 63 ? text : text[..60] + "...";

    private static Icon LoadAppIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(path))
        {
            return new Icon(path);
        }

        return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
    }

    private static class Theme
    {
        public static readonly Color Page = Color.FromArgb(243, 244, 248);
        public static readonly Color Text = Color.FromArgb(29, 36, 48);
        public static readonly Color Muted = Color.FromArgb(102, 112, 133);
        public static readonly Color Line = Color.FromArgb(217, 222, 231);
        public static readonly Color Accent = Color.FromArgb(47, 111, 237);
    }

    private sealed class RoundedPanel : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Radius { get; init; } = 8;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor { get; init; } = Color.LightGray;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRectangle(ClientRectangle, Radius);
            using var pen = new Pen(BorderColor);
            e.Graphics.DrawPath(pen, path);
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter - 1;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter - 1;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
