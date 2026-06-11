using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using Microsoft.Win32;

namespace WindowsScreenTimeApp;

public sealed class MainForm : Form
{
    private enum ChartMode
    {
        Bar,
        Ring
    }

    private enum UsageSortColumn
    {
        App,
        Foreground,
        Background,
        Total,
        Percent,
        Title
    }

    private readonly AppSettings _settings;
    private readonly ActivityTracker _tracker;
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _appIcon;
    private AppTheme _theme;
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
    private UsageSortColumn _sortColumn = UsageSortColumn.Total;
    private bool _sortDescending = true;
    private bool _reallyExit;

    public MainForm()
    {
        Text = $"Windows Screen Time {AppInfo.Version}";
        MinimumSize = new Size(1120, 760);
        Size = new Size(1280, 840);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);
        _appIcon = LoadAppIcon();
        Icon = _appIcon;

        _settings = AppSettings.Load();
        _theme = AppTheme.Resolve(_settings.ThemeMode);
        BackColor = _theme.Window;
        _tracker = new ActivityTracker(_settings);
        _tracker.Updated += (_, _) => RefreshData();
        _notifyIcon = CreateNotifyIcon();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

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
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
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
            BackColor = _theme.Window,
            Tag = "window"
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 264));
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
            BackColor = _theme.Sidebar,
            Padding = new Padding(16, 18, 16, 14),
            Tag = "sidebar"
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = _theme.Sidebar,
            Tag = "sidebar"
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 222));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        sidebar.Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Windows\r\nScreen Time",
            Font = new Font(Font.FontFamily, 15F, FontStyle.Bold),
            ForeColor = _theme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = "text"
        }, 0, 0);

        var viewSection = MakeSidebarSection("\u89c6\u56fe");
        viewSection.Controls.Add(_todayNav);
        viewSection.Controls.Add(_weekNav);
        layout.Controls.Add(viewSection, 0, 1);

        var actionSection = MakeSidebarSection("\u64cd\u4f5c");
        actionSection.Controls.Add(MakeNavButton("\u5bfc\u51fa\u6570\u636e", (_, _) => ExportData()));
        actionSection.Controls.Add(MakeNavButton("\u5bfc\u5165\u6570\u636e", (_, _) => ImportData()));
        actionSection.Controls.Add(MakeNavButton("\u8bbe\u7f6e", (_, _) => OpenSettings()));
        actionSection.Controls.Add(MakeNavButton("\u91cd\u7f6e\u7edf\u8ba1", (_, _) => ResetData()));
        layout.Controls.Add(actionSection, 0, 2);

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = $"\u7248\u672c {AppInfo.Version}\r\n\u4f5c\u8005\uff1a{AppInfo.Author}\r\n{AppInfo.ProjectUrl}",
            ForeColor = _theme.Muted,
            TextAlign = ContentAlignment.BottomLeft,
            Tag = "muted"
        }, 0, 4);
        return sidebar;
    }

    private FlowLayoutPanel MakeSidebarSection(string title)
    {
        var section = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = _theme.Sidebar,
            Padding = new Padding(0, 8, 0, 0),
            Tag = "sidebar"
        };
        section.Controls.Add(new Label
        {
            Width = 222,
            Height = 24,
            Text = title,
            ForeColor = _theme.Muted,
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = "muted"
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
            BackColor = _theme.Window,
            Tag = "window"
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
            BackColor = _theme.Window,
            Tag = "window"
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
            BackColor = _theme.Surface,
            Tag = "surface"
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var tabs = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = _theme.Surface,
            Tag = "surface"
        };
        tabs.Controls.Add(_barTab);
        tabs.Controls.Add(_ringTab);
        layout.Controls.Add(tabs, 0, 0);

        var chartHost = new Panel { Dock = DockStyle.Fill, BackColor = _theme.Surface, Tag = "surface" };
        _chart.Dock = DockStyle.Fill;
        _chart.ApplyTheme(_theme);
        _ringChart.Dock = DockStyle.Fill;
        _ringChart.ApplyTheme(_theme);
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
            BackColor = _theme.Surface,
            Tag = "surface"
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "\u5e94\u7528\u660e\u7ec6",
            Font = new Font(Font.FontFamily, 13F, FontStyle.Bold),
            ForeColor = _theme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = "text"
        }, 0, 0);

        _usageList.Dock = DockStyle.Fill;
        _usageList.View = View.Details;
        _usageList.FullRowSelect = true;
        _usageList.GridLines = false;
        _usageList.BorderStyle = BorderStyle.None;
        _usageList.BackColor = _theme.Surface;
        _usageList.ForeColor = _theme.Text;
        _usageList.Columns.Add("\u5e94\u7528", 210);
        _usageList.Columns.Add("\u524d\u53f0", 110);
        _usageList.Columns.Add("\u540e\u53f0", 110);
        _usageList.Columns.Add("\u603b\u65f6\u95f4", 110);
        _usageList.Columns.Add("\u5360\u6bd4", 80);
        _usageList.Columns.Add("\u6700\u8fd1\u7a97\u53e3\u6807\u9898", 520);
        _usageList.ColumnClick += OnUsageColumnClick;
        UpdateUsageColumnHeaders();

        _emptyState.Dock = DockStyle.Fill;
        _emptyState.Text = "\u7edf\u8ba1\u51e0\u79d2\u540e\u4f1a\u663e\u793a\u5e94\u7528\u660e\u7ec6\u3002";
        _emptyState.ForeColor = _theme.Muted;
        _emptyState.TextAlign = ContentAlignment.MiddleCenter;
        _emptyState.Tag = "muted";

        var listHost = new Panel { Dock = DockStyle.Fill, BackColor = _theme.Surface, Tag = "surface" };
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
            ForeColor = _theme.Muted,
            Tag = "muted"
        };
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.Font = new Font(Font.FontFamily, 17F, FontStyle.Bold);
        valueLabel.ForeColor = _theme.Text;
        valueLabel.AutoEllipsis = true;
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.Tag = "text";

        panel.Controls.Add(valueLabel);
        panel.Controls.Add(label);
        return panel;
    }

    private Panel MakePanel() => new RoundedPanel
    {
        Dock = DockStyle.Fill,
        BackColor = _theme.Surface,
        BorderColor = _theme.Line,
        Radius = 8,
        Tag = "surface"
    };

    private Button MakeButton(string text, EventHandler onClick, bool primary)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Height = 34,
            Margin = new Padding(8, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? _theme.Accent : _theme.Surface,
            ForeColor = primary ? Color.White : _theme.Text,
            Tag = primary ? "primaryButton" : "button"
        };
        button.FlatAppearance.BorderColor = primary ? _theme.Accent : _theme.Line;
        button.FlatAppearance.BorderSize = 1;
        button.Click += onClick;
        return button;
    }

    private Button MakeNavButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            Width = 222,
            Height = 36,
            Margin = new Padding(0, 0, 0, 8),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = _theme.Sidebar,
            ForeColor = _theme.Text,
            Tag = "nav"
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += onClick;
        return button;
    }

    private Button MakeTabButton(string text, EventHandler onClick, bool active)
    {
        var button = new Button
        {
            Text = text,
            Width = 92,
            Height = 30,
            Margin = new Padding(0, 0, 8, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = active ? _theme.NavActive : _theme.Surface,
            ForeColor = _theme.Text,
            Tag = "tab"
        };
        button.FlatAppearance.BorderColor = active ? _theme.Accent : _theme.Line;
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
        items = SortUsageItems(items);

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
            row.ForeColor = item.IsIdle ? _theme.Muted : _theme.Text;
            row.BackColor = _usageList.Items.Count % 2 == 0 ? _theme.Surface : _theme.ListAlt;
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

    private void OnUsageColumnClick(object? sender, ColumnClickEventArgs e)
    {
        var column = (UsageSortColumn)e.Column;
        if (_sortColumn == column)
        {
            _sortDescending = !_sortDescending;
        }
        else
        {
            _sortColumn = column;
            _sortDescending = column is UsageSortColumn.Foreground or UsageSortColumn.Background or UsageSortColumn.Total or UsageSortColumn.Percent;
        }

        UpdateUsageColumnHeaders();
        RefreshData();
    }

    private List<AppUsage> SortUsageItems(IEnumerable<AppUsage> items)
    {
        static string SafeText(string? text) => text ?? string.Empty;

        IOrderedEnumerable<AppUsage> ordered = _sortColumn switch
        {
            UsageSortColumn.App => _sortDescending
                ? items.OrderByDescending(item => SafeText(item.AppName), StringComparer.CurrentCultureIgnoreCase)
                : items.OrderBy(item => SafeText(item.AppName), StringComparer.CurrentCultureIgnoreCase),
            UsageSortColumn.Foreground => _sortDescending
                ? items.OrderByDescending(item => item.ForegroundDuration)
                : items.OrderBy(item => item.ForegroundDuration),
            UsageSortColumn.Background => _sortDescending
                ? items.OrderByDescending(item => item.BackgroundDuration)
                : items.OrderBy(item => item.BackgroundDuration),
            UsageSortColumn.Total => _sortDescending
                ? items.OrderByDescending(item => item.Duration)
                : items.OrderBy(item => item.Duration),
            UsageSortColumn.Percent => _sortDescending
                ? items.OrderByDescending(GetUsagePercentValue)
                : items.OrderBy(GetUsagePercentValue),
            UsageSortColumn.Title => _sortDescending
                ? items.OrderByDescending(item => SafeText(item.WindowTitle), StringComparer.CurrentCultureIgnoreCase)
                : items.OrderBy(item => SafeText(item.WindowTitle), StringComparer.CurrentCultureIgnoreCase),
            _ => items.OrderByDescending(item => item.Duration)
        };

        return ordered
            .ThenBy(item => item.IsIdle)
            .ThenBy(item => item.AppName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private double GetUsagePercentValue(AppUsage item) =>
        item.IsIdle || _tracker.ActiveTotal.TotalSeconds <= 0
            ? 0
            : item.Duration.TotalSeconds / _tracker.ActiveTotal.TotalSeconds;

    private void UpdateUsageColumnHeaders()
    {
        if (_usageList.Columns.Count < 6)
        {
            return;
        }

        string[] names =
        [
            "\u5e94\u7528",
            "\u524d\u53f0",
            "\u540e\u53f0",
            "\u603b\u65f6\u95f4",
            "\u5360\u6bd4",
            "\u6700\u8fd1\u7a97\u53e3\u6807\u9898"
        ];

        for (var i = 0; i < names.Length; i++)
        {
            var marker = i == (int)_sortColumn ? (_sortDescending ? " \u2193" : " \u2191") : string.Empty;
            _usageList.Columns[i].Text = names[i] + marker;
        }
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

    private void StyleNav(Button button, bool active)
    {
        button.BackColor = active ? _theme.NavActive : _theme.Sidebar;
        button.ForeColor = _theme.Text;
    }

    private void StyleTab(Button button, bool active)
    {
        button.BackColor = active ? _theme.NavActive : _theme.Surface;
        button.ForeColor = _theme.Text;
        button.FlatAppearance.BorderColor = active ? _theme.Accent : _theme.Line;
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
                ApplyTheme(AppTheme.Resolve(_settings.ThemeMode));
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
        using var dialog = new SettingsForm(_settings, _theme);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            StartupManager.SetEnabled(_settings.StartWithWindows);
            _settings.Save();
            _tracker.ApplySettings(_settings);
            ApplyTheme(AppTheme.Resolve(_settings.ThemeMode));
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

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (_settings.ThemeMode != AppThemeMode.System)
        {
            return;
        }

        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle)
        {
            BeginInvoke(() => ApplyTheme(AppTheme.Resolve(_settings.ThemeMode)));
        }
    }

    private void ApplyTheme(AppTheme theme)
    {
        _theme = theme;
        BackColor = _theme.Window;
        ApplyThemeToControl(this);
        _chart.ApplyTheme(_theme);
        _ringChart.ApplyTheme(_theme);
        _usageList.BackColor = _theme.Surface;
        _usageList.ForeColor = _theme.Text;
        RefreshNavState();
        RefreshChartTabs();
        Invalidate(true);
    }

    private void ApplyThemeToControl(Control control)
    {
        switch (control.Tag as string)
        {
            case "window":
                control.BackColor = _theme.Window;
                break;
            case "sidebar":
                control.BackColor = _theme.Sidebar;
                break;
            case "surface":
                control.BackColor = _theme.Surface;
                if (control is RoundedPanel rounded)
                {
                    rounded.BorderColor = _theme.Line;
                }
                break;
            case "text":
                control.ForeColor = _theme.Text;
                break;
            case "muted":
                control.ForeColor = _theme.Muted;
                break;
            case "button":
                control.BackColor = _theme.Surface;
                control.ForeColor = _theme.Text;
                if (control is Button secondaryButton)
                {
                    secondaryButton.FlatAppearance.BorderColor = _theme.Line;
                }
                break;
            case "primaryButton":
                control.BackColor = _theme.Accent;
                control.ForeColor = Color.White;
                if (control is Button primaryButton)
                {
                    primaryButton.FlatAppearance.BorderColor = _theme.Accent;
                }
                break;
            case "nav":
                control.BackColor = _theme.Sidebar;
                control.ForeColor = _theme.Text;
                break;
            case "tab":
                control.ForeColor = _theme.Text;
                break;
        }

        foreach (Control child in control.Controls)
        {
            ApplyThemeToControl(child);
        }
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

    private sealed class RoundedPanel : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Radius { get; init; } = 8;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor { get; set; } = Color.LightGray;

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
