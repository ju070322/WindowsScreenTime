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
    private readonly Label _addressLabel = new();
    private readonly BarChartPanel _chart = new();
    private readonly ListView _usageList = new();
    private readonly Label _emptyState = new();
    private readonly Button _todayNav;
    private readonly Button _weekNav;
    private bool _reallyExit;

    public MainForm()
    {
        Text = $"Windows Screen Time {AppInfo.Version}";
        MinimumSize = new Size(1100, 760);
        Size = new Size(1260, 840);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Window;
        Font = new Font("Microsoft YaHei UI", 9F);
        _appIcon = LoadAppIcon();
        Icon = _appIcon;

        _settings = AppSettings.Load();
        _tracker = new ActivityTracker(_settings);
        _tracker.Updated += (_, _) => RefreshData();
        _notifyIcon = CreateNotifyIcon();

        _todayNav = MakeNavButton("今日使用时间", (_, _) => SetPeriod(UsagePeriod.Day));
        _weekNav = MakeNavButton("本周使用时间", (_, _) => SetPeriod(UsagePeriod.Week));

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
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Window
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 238));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(shell);

        shell.Controls.Add(BuildSidebar(), 0, 0);
        shell.Controls.Add(BuildContent(), 1, 0);
    }

    private Control BuildSidebar()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Sidebar,
            Padding = new Padding(14, 18, 14, 14)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Sidebar
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 184));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        sidebar.Controls.Add(layout);

        var brand = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Windows\r\nScreen Time",
            Font = new Font(Font.FontFamily, 15F, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(brand, 0, 0);

        var nav = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Theme.Sidebar
        };
        nav.Controls.Add(_todayNav);
        nav.Controls.Add(_weekNav);
        nav.Controls.Add(MakeNavButton("导出数据", (_, _) => ExportData()));
        nav.Controls.Add(MakeNavButton("导入数据", (_, _) => ImportData()));
        nav.Controls.Add(MakeNavButton("设置", (_, _) => OpenSettings()));
        layout.Controls.Add(nav, 0, 1);

        var footer = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"版本 {AppInfo.Version}\r\n作者：{AppInfo.Author}\r\n{AppInfo.ProjectUrl}",
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.BottomLeft
        };
        layout.Controls.Add(footer, 0, 3);
        return sidebar;
    }

    private Control BuildContent()
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(18),
            BackColor = Theme.Window
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 44));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 56));

        content.Controls.Add(BuildCommandBar(), 0, 0);
        content.Controls.Add(BuildMetrics(), 0, 1);
        content.Controls.Add(BuildChartPanel(), 0, 2);
        content.Controls.Add(BuildUsagePanel(), 0, 3);
        return content;
    }

    private Control BuildCommandBar()
    {
        var bar = MakePanel();
        bar.Padding = new Padding(10, 8, 10, 8);
        bar.Margin = new Padding(0, 0, 0, 14);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.White
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 152));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        bar.Controls.Add(layout);

        var arrows = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, WrapContents = false };
        arrows.Controls.Add(MakeIconButton("<", (_, _) => SetPeriod(UsagePeriod.Day)));
        arrows.Controls.Add(MakeIconButton(">", (_, _) => SetPeriod(UsagePeriod.Week)));
        layout.Controls.Add(arrows, 0, 0);

        _addressLabel.Dock = DockStyle.Fill;
        _addressLabel.BackColor = Theme.Address;
        _addressLabel.ForeColor = Theme.Text;
        _addressLabel.TextAlign = ContentAlignment.MiddleLeft;
        _addressLabel.Padding = new Padding(12, 0, 0, 0);
        layout.Controls.Add(_addressLabel, 1, 0);

        layout.Controls.Add(MakeButton("打开项目", (_, _) => OpenProjectUrl(), false), 2, 0);
        layout.Controls.Add(MakeButton("重置", (_, _) => ResetData(), false), 3, 0);
        return bar;
    }

    private Control BuildMetrics()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Theme.Window
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
        panel.Margin = new Padding(0, 0, 0, 16);
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
        _usageList.Columns.Add("最近窗口标题", 520);

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

    private Control MakeMetric(string labelText, Label valueLabel)
    {
        var panel = MakePanel();
        panel.Margin = new Padding(0, 0, 12, 16);
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

    private static Button MakeIconButton(string text, EventHandler onClick)
    {
        var button = MakeButton(text, onClick, false);
        button.Width = 34;
        button.Margin = new Padding(0, 0, 8, 0);
        return button;
    }

    private static Button MakeNavButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            Width = 202,
            Height = 34,
            Margin = new Padding(0, 0, 0, 6),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Sidebar,
            ForeColor = Theme.Text
        };
        button.FlatAppearance.BorderSize = 0;
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
        _addressLabel.Text = _tracker.Period == UsagePeriod.Day
            ? "Windows Screen Time  >  今日使用时间"
            : "Windows Screen Time  >  本周使用时间";
        _notifyIcon.Text = ClipNotifyText($"Windows Screen Time\n当前：{_tracker.Current.AppName}\n总计：{UiFormat.Duration(_tracker.ActiveTotal)}");
        RefreshNavState();

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

    private void RefreshNavState()
    {
        StyleNav(_todayNav, _tracker.Period == UsagePeriod.Day);
        StyleNav(_weekNav, _tracker.Period == UsagePeriod.Week);
    }

    private static void StyleNav(Button button, bool active)
    {
        button.BackColor = active ? Theme.NavActive : Theme.Sidebar;
        button.ForeColor = Theme.Text;
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
        public static readonly Color Window = Color.FromArgb(243, 244, 248);
        public static readonly Color Sidebar = Color.FromArgb(248, 249, 252);
        public static readonly Color NavActive = Color.FromArgb(230, 238, 255);
        public static readonly Color Address = Color.FromArgb(247, 248, 251);
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
