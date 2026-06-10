using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace WindowsScreenTimeApp;

public sealed class MainForm : Form
{
    private enum ChartMode
    {
        Bar,
        Ring
    }

    private readonly AppSettings _settings;
    private readonly ActivityTracker _tracker;
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _appIcon;
    private readonly Label _bootValue = new();
    private readonly Label _foregroundValue = new();
    private readonly Label _backgroundValue = new();
    private readonly Label _currentValue = new();
    private readonly BarChartPanel _chart = new();
    private readonly RingChartPanel _ringChart = new();
    private readonly ListView _usageList = new();
    private readonly Label _emptyState = new();
    private readonly Button _todayNav;
    private readonly Button _weekNav;
    private readonly Button _barTab;
    private readonly Button _ringTab;
    private ChartMode _chartMode = ChartMode.Bar;
    private bool _reallyExit;

    public MainForm()
    {
        Text = $"Windows Screen Time {AppInfo.Version}";
        MinimumSize = new Size(1120, 760);
        Size = new Size(1280, 840);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Window;
        Font = new Font("Microsoft YaHei UI", 9F);
        _appIcon = LoadAppIcon();
        Icon = _appIcon;

        _settings = AppSettings.Load();
        _tracker = new ActivityTracker(_settings);
        _tracker.Updated += (_, _) => RefreshData();
        _notifyIcon = CreateNotifyIcon();

        _todayNav = MakeNavButton("\u4eca\u65e5\u4f7f\u7528\u65f6\u95f4", (_, _) => SetPeriod(UsagePeriod.Day));
        _weekNav = MakeNavButton("\u672c\u5468\u4f7f\u7528\u65f6\u95f4", (_, _) => SetPeriod(UsagePeriod.Week));
        _barTab = MakeTabButton("\u67f1\u72b6\u56fe", (_, _) => SetChartMode(ChartMode.Bar), true);
        _ringTab = MakeTabButton("\u5706\u73af\u56fe", (_, _) => SetChartMode(ChartMode.Ring), false);

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
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 248));
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
            Padding = new Padding(16, 18, 16, 14)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Theme.Sidebar
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 142));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 178));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        sidebar.Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Windows\r\nScreen Time",
            Font = new Font(Font.FontFamily, 15F, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var viewSection = MakeSidebarSection("\u89c6\u56fe");
        viewSection.Controls.Add(_weekNav);
        viewSection.Controls.Add(_todayNav);
        layout.Controls.Add(viewSection, 0, 1);

        var actionSection = MakeSidebarSection("\u64cd\u4f5c");
        actionSection.Controls.Add(MakeNavButton("\u8bbe\u7f6e", (_, _) => OpenSettings()));
        actionSection.Controls.Add(MakeNavButton("\u5bfc\u5165\u6570\u636e", (_, _) => ImportData()));
        actionSection.Controls.Add(MakeNavButton("\u5bfc\u51fa\u6570\u636e", (_, _) => ExportData()));
        actionSection.Controls.Add(MakeNavButton("\u91cd\u7f6e\u7edf\u8ba1", (_, _) => ResetData()));
        layout.Controls.Add(actionSection, 0, 2);

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = $"\u7248\u672c {AppInfo.Version}\r\n\u4f5c\u8005\uff1a{AppInfo.Author}\r\n{AppInfo.ProjectUrl}",
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 4);
        return sidebar;
    }

    private static FlowLayoutPanel MakeSidebarSection(string title)
    {
        var section = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.BottomUp,
            WrapContents = false,
            BackColor = Theme.Sidebar,
            Padding = new Padding(0, 24, 0, 0)
        };
        section.Controls.Add(new Label
        {
            Width = 206,
            Height = 24,
            Text = title,
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.MiddleLeft
        });
        return section;
    }

    private Control BuildContent()
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = Theme.Window
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 52));

        content.Controls.Add(BuildMetrics(), 0, 0);
        content.Controls.Add(BuildChartPanel(), 0, 1);
        content.Controls.Add(BuildUsagePanel(), 0, 2);
        return content;
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

        grid.Controls.Add(MakeMetric("\u5f00\u673a\u65f6\u95f4", _bootValue), 0, 0);
        grid.Controls.Add(MakeMetric("\u524d\u53f0\u65f6\u95f4", _foregroundValue), 1, 0);
        grid.Controls.Add(MakeMetric("\u540e\u53f0\u65f6\u95f4", _backgroundValue), 2, 0);
        grid.Controls.Add(MakeMetric("\u5f53\u524d\u5e94\u7528", _currentValue), 3, 0);
        return grid;
    }

    private Control BuildChartPanel()
    {
        var panel = MakePanel();
        panel.Margin = new Padding(0, 0, 0, 16);
        panel.Padding = new Padding(18, 14, 18, 18);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var tabs = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.White
        };
        tabs.Controls.Add(_barTab);
        tabs.Controls.Add(_ringTab);
        layout.Controls.Add(tabs, 0, 0);

        var chartHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        _chart.Dock = DockStyle.Fill;
        _ringChart.Dock = DockStyle.Fill;
        _ringChart.Visible = false;
        chartHost.Controls.Add(_ringChart);
        chartHost.Controls.Add(_chart);
        layout.Controls.Add(chartHost, 0, 1);

        panel.Controls.Add(layout);
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
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "\u5e94\u7528\u660e\u7ec6",
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
        _usageList.Columns.Add("\u5e94\u7528", 210);
        _usageList.Columns.Add("\u524d\u53f0", 110);
        _usageList.Columns.Add("\u540e\u53f0", 110);
        _usageList.Columns.Add("\u603b\u65f6\u95f4", 110);
        _usageList.Columns.Add("\u5360\u6bd4", 80);
        _usageList.Columns.Add("\u6700\u8fd1\u7a97\u53e3\u6807\u9898", 520);

        _emptyState.Dock = DockStyle.Fill;
        _emptyState.Text = "\u7edf\u8ba1\u51e0\u79d2\u540e\u4f1a\u663e\u793a\u5e94\u7528\u660e\u7ec6\u3002";
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
            Dock = DockStyle.Fill,
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

    private static Button MakeNavButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            Width = 206,
            Height = 36,
            Margin = new Padding(0, 0, 0, 8),
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

    private static Button MakeTabButton(string text, EventHandler onClick, bool active)
    {
        var button = new Button
        {
            Text = text,
            Width = 92,
            Height = 30,
            Margin = new Padding(0, 0, 8, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = active ? Theme.NavActive : Color.White,
            ForeColor = Theme.Text
        };
        button.FlatAppearance.BorderColor = active ? Theme.Accent : Theme.Line;
        button.FlatAppearance.BorderSize = 1;
        button.Click += onClick;
        return button;
    }

    private NotifyIcon CreateNotifyIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("\u6253\u5f00", null, (_, _) => ShowFromTray());
        menu.Items.Add("\u5bfc\u51fa\u6570\u636e", null, (_, _) => ExportData());
        menu.Items.Add("\u5bfc\u5165\u6570\u636e", null, (_, _) => ImportData());
        menu.Items.Add("\u8bbe\u7f6e", null, (_, _) => OpenSettings());
        menu.Items.Add("\u91cd\u7f6e\u7edf\u8ba1", null, (_, _) => ResetData());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("\u9000\u51fa", null, (_, _) => ExitApplication());

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
        _bootValue.Text = UiFormat.Duration(GetSystemUptime());
        _foregroundValue.Text = UiFormat.Duration(_tracker.ForegroundTotal);
        _backgroundValue.Text = UiFormat.Duration(_tracker.BackgroundTotal);
        _currentValue.Text = _tracker.Current.AppName;
        _notifyIcon.Text = ClipNotifyText($"Windows Screen Time\n\u5f53\u524d\uff1a{_tracker.Current.AppName}\n\u603b\u8ba1\uff1a{UiFormat.Duration(_tracker.ActiveTotal)}");
        RefreshNavState();

        var items = _tracker.Items
            .Where(item => _settings.IncludeIdleInList || !item.IsIdle)
            .ToList();

        var activeItems = items.Where(item => !item.IsIdle).ToList();
        _chart.SetItems(activeItems);
        _ringChart.SetDurations(_tracker.ForegroundTotal, _tracker.BackgroundTotal);

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

    private void SetChartMode(ChartMode mode)
    {
        _chartMode = mode;
        RefreshChartTabs();
    }

    private void RefreshChartTabs()
    {
        var showBar = _chartMode == ChartMode.Bar;
        _chart.Visible = showBar;
        _ringChart.Visible = !showBar;
        StyleTab(_barTab, showBar);
        StyleTab(_ringTab, !showBar);
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

    private static void StyleTab(Button button, bool active)
    {
        button.BackColor = active ? Theme.NavActive : Color.White;
        button.FlatAppearance.BorderColor = active ? Theme.Accent : Theme.Line;
    }

    private static TimeSpan GetSystemUptime() =>
        TimeSpan.FromMilliseconds(Environment.TickCount64);

    private void ExportData()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "\u5bfc\u51fa\u4f7f\u7528\u6570\u636e",
            Filter = "Windows Screen Time \u6570\u636e (*.wstdata)|*.wstdata|JSON \u6587\u4ef6 (*.json)|*.json",
            FileName = $"WindowsScreenTime-{DateTime.Now:yyyyMMdd-HHmm}.wstdata"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _tracker.ExportTo(dialog.FileName, _settings);
            MessageBox.Show("\u6570\u636e\u5df2\u5bfc\u51fa\u3002", "Windows Screen Time", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"\u5bfc\u51fa\u5931\u8d25\uff1a{ex.Message}", "Windows Screen Time", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportData()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "\u5bfc\u5165\u4f7f\u7528\u6570\u636e",
            Filter = "Windows Screen Time \u6570\u636e (*.wstdata;*.json)|*.wstdata;*.json|\u6240\u6709\u6587\u4ef6 (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var confirm = MessageBox.Show(
            "\u5bfc\u5165\u4f1a\u628a\u6587\u4ef6\u4e2d\u7684\u4f7f\u7528\u65f6\u95f4\u5408\u5e76\u5230\u5f53\u524d\u7edf\u8ba1\uff0c\u5e76\u7ee7\u627f\u5bfc\u5165\u6587\u4ef6\u91cc\u7684\u8bbe\u7f6e\u3002\u662f\u5426\u7ee7\u7eed\uff1f",
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
            MessageBox.Show("\u6570\u636e\u5df2\u5bfc\u5165\u5e76\u5408\u5e76\u3002", "Windows Screen Time", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"\u5bfc\u5165\u5931\u8d25\uff1a{ex.Message}", "Windows Screen Time", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetData()
    {
        var confirm = MessageBox.Show(
            "\u786e\u5b9a\u8981\u6e05\u7a7a\u5f53\u524d\u7d2f\u8ba1\u7edf\u8ba1\u5417\uff1f\u8fd9\u4e0d\u4f1a\u5220\u9664\u5bfc\u51fa\u7684\u5907\u4efd\u6587\u4ef6\u3002",
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
            MessageBox.Show($"\u4fdd\u5b58\u8bbe\u7f6e\u5931\u8d25\uff1a{ex.Message}", "Windows Screen Time",
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
