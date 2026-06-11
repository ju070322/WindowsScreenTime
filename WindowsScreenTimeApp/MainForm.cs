using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
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
    private Texts _texts;
    private readonly Label _titleLabel = new();
    private readonly Label _viewSectionLabel = new();
    private readonly Label _actionSectionLabel = new();
    private readonly Label _versionLabel = new();
    private readonly Label _usageTitleLabel = new();
    private readonly Label _bootValue = new();
    private readonly Label _foregroundValue = new();
    private readonly Label _backgroundValue = new();
    private readonly Label _currentValue = new();
    private readonly Label _bootLabel = new();
    private readonly Label _foregroundLabel = new();
    private readonly Label _backgroundLabel = new();
    private readonly Label _currentLabel = new();
    private readonly BarChartPanel _chart = new();
    private readonly RingChartPanel _ringChart = new();
    private readonly ListView _usageList = new();
    private readonly Label _emptyState = new();
    private readonly Button _todayNav;
    private readonly Button _weekNav;
    private readonly Button _exportNav;
    private readonly Button _importNav;
    private readonly Button _settingsNav;
    private readonly Button _resetNav;
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
        _texts = Texts.Resolve(_settings.Language);
        BackColor = _theme.Window;
        _tracker = new ActivityTracker(_settings);
        _tracker.Updated += (_, _) => RefreshData();
        _notifyIcon = CreateNotifyIcon();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        _todayNav = MakeNavButton(_texts.Today, (_, _) => SetPeriod(UsagePeriod.Day));
        _weekNav = MakeNavButton(_texts.Week, (_, _) => SetPeriod(UsagePeriod.Week));
        _exportNav = MakeNavButton(_texts.ExportData, (_, _) => ExportData());
        _importNav = MakeNavButton(_texts.ImportData, (_, _) => ImportData());
        _settingsNav = MakeNavButton(_texts.Settings, (_, _) => OpenSettings());
        _resetNav = MakeNavButton(_texts.ResetStats, (_, _) => ResetData());
        _barTab = MakeTabButton(_texts.BarChart, (_, _) => SetChartMode(ChartMode.Bar), true);
        _ringTab = MakeTabButton(_texts.RingChart, (_, _) => SetChartMode(ChartMode.Ring), false);

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

        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.Text = "Windows\r\nScreen Time";
        _titleLabel.Font = new Font(Font.FontFamily, 15F, FontStyle.Bold);
        _titleLabel.ForeColor = _theme.Text;
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _titleLabel.Tag = "text";
        layout.Controls.Add(_titleLabel, 0, 0);

        var viewSection = MakeSidebarSection(_viewSectionLabel, _texts.View);
        viewSection.Controls.Add(_todayNav);
        viewSection.Controls.Add(_weekNav);
        layout.Controls.Add(viewSection, 0, 1);

        var actionSection = MakeSidebarSection(_actionSectionLabel, _texts.Actions);
        actionSection.Controls.Add(_exportNav);
        actionSection.Controls.Add(_importNav);
        actionSection.Controls.Add(_settingsNav);
        actionSection.Controls.Add(_resetNav);
        layout.Controls.Add(actionSection, 0, 2);

        _versionLabel.Dock = DockStyle.Fill;
        _versionLabel.Text = $"{_texts.Version} {AppInfo.Version}\r\n{_texts.Author}: {AppInfo.Author}\r\n{AppInfo.ProjectUrl}";
        _versionLabel.ForeColor = _theme.Muted;
        _versionLabel.TextAlign = ContentAlignment.BottomLeft;
        _versionLabel.Tag = "muted";
        layout.Controls.Add(_versionLabel, 0, 4);
        return sidebar;
    }

    private FlowLayoutPanel MakeSidebarSection(Label label, string title)
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
        label.Width = 222;
        label.Height = 24;
        label.Text = title;
        label.ForeColor = _theme.Muted;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Tag = "muted";
        section.Controls.Add(label);
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

        var metrics = new[]
        {
            MakeMetric(_texts.Uptime, _bootLabel, _bootValue),
            MakeMetric(_texts.ForegroundTime, _foregroundLabel, _foregroundValue),
            MakeMetric(_texts.BackgroundTime, _backgroundLabel, _backgroundValue),
            MakeMetric(_texts.CurrentApp, _currentLabel, _currentValue)
        };

        for (var i = 0; i < metrics.Length; i++)
        {
            metrics[i].Margin = new Padding(0, 0, i == metrics.Length - 1 ? 0 : 12, 4);
            grid.Controls.Add(metrics[i], i, 0);
        }

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
        _chart.ApplyTexts(_texts);
        _ringChart.Dock = DockStyle.Fill;
        _ringChart.ApplyTheme(_theme);
        _ringChart.ApplyTexts(_texts);
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

        _usageTitleLabel.Dock = DockStyle.Fill;
        _usageTitleLabel.Text = _texts.UsageDetails;
        _usageTitleLabel.Font = new Font(Font.FontFamily, 13F, FontStyle.Bold);
        _usageTitleLabel.ForeColor = _theme.Text;
        _usageTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _usageTitleLabel.Tag = "text";
        layout.Controls.Add(_usageTitleLabel, 0, 0);

        _usageList.Dock = DockStyle.Fill;
        _usageList.View = View.Details;
        _usageList.FullRowSelect = true;
        _usageList.GridLines = false;
        _usageList.BorderStyle = BorderStyle.None;
        _usageList.OwnerDraw = true;
        _usageList.HideSelection = false;
        _usageList.BackColor = _theme.Surface;
        _usageList.ForeColor = _theme.Text;
        _usageList.Columns.Add(_texts.AppColumn, 210);
        _usageList.Columns.Add(_texts.ForegroundColumn, 110);
        _usageList.Columns.Add(_texts.BackgroundColumn, 110);
        _usageList.Columns.Add(_texts.TotalColumn, 110);
        _usageList.Columns.Add(_texts.ShareColumn, 80);
        _usageList.Columns.Add(_texts.RecentWindowTitle, 520);
        _usageList.ColumnClick += OnUsageColumnClick;
        _usageList.DrawColumnHeader += DrawUsageColumnHeader;
        _usageList.DrawItem += (_, _) => { };
        _usageList.DrawSubItem += DrawUsageSubItem;
        _usageList.Resize += (_, _) => UpdateUsageColumnWidths();
        _usageList.HandleCreated += (_, _) => ApplyListViewSystemTheme();
        UpdateUsageColumnHeaders();
        UpdateUsageColumnWidths();

        _emptyState.Dock = DockStyle.Fill;
        _emptyState.Text = _texts.EmptyDetails;
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

    private Control MakeMetric(string labelText, Label label, Label valueLabel)
    {
        var panel = MakePanel();
        panel.Margin = new Padding(0, 0, 12, 16);
        panel.Padding = new Padding(16, 12, 16, 12);

        label.Dock = DockStyle.Top;
        label.Height = 24;
        label.Text = labelText;
        label.ForeColor = _theme.Muted;
        label.Tag = "muted";
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
        var button = new FocuslessButton
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
        var button = new FocuslessButton
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
        var button = new FocuslessButton
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
        var icon = new NotifyIcon
        {
            Text = "Windows Screen Time",
            Icon = _appIcon,
            Visible = true,
            ContextMenuStrip = CreateTrayMenu()
        };
        icon.DoubleClick += (_, _) => ShowFromTray();
        return icon;
    }

    private ContextMenuStrip CreateTrayMenu()
    {
        var menu = new ContextMenuStrip
        {
            BackColor = _theme.Surface,
            ForeColor = _theme.Text
        };
        menu.Items.Add(_texts.Open, null, (_, _) => ShowFromTray());
        menu.Items.Add(_texts.ExportData, null, (_, _) => ExportData());
        menu.Items.Add(_texts.ImportData, null, (_, _) => ImportData());
        menu.Items.Add(_texts.Settings, null, (_, _) => OpenSettings());
        menu.Items.Add(_texts.ResetStats, null, (_, _) => ResetData());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_texts.Exit, null, (_, _) => ExitApplication());
        return menu;
    }

    private void RefreshData()
    {
        _bootValue.Text = UiFormat.Duration(GetSystemUptime());
        _foregroundValue.Text = UiFormat.Duration(_tracker.ForegroundTotal);
        _backgroundValue.Text = UiFormat.Duration(_tracker.BackgroundTotal);
        _currentValue.Text = _tracker.Current.AppName;
        _notifyIcon.Text = ClipNotifyText($"Windows Screen Time\n{_texts.CurrentNotify}: {_tracker.Current.AppName}\n{_texts.TotalNotify}: {UiFormat.Duration(_tracker.ActiveTotal)}");
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
            _texts.AppColumn,
            _texts.ForegroundColumn,
            _texts.BackgroundColumn,
            _texts.TotalColumn,
            _texts.ShareColumn,
            _texts.RecentWindowTitle
        ];

        for (var i = 0; i < names.Length; i++)
        {
            var marker = i == (int)_sortColumn ? (_sortDescending ? " \u2193" : " \u2191") : string.Empty;
            _usageList.Columns[i].Text = names[i] + marker;
        }
    }

    private void UpdateUsageColumnWidths()
    {
        if (_usageList.Columns.Count < 6 || _usageList.ClientSize.Width <= 0)
        {
            return;
        }

        var width = Math.Max(680, _usageList.ClientSize.Width - 4);
        var rightEdgeCover = _theme.IsDark ? SystemInformation.VerticalScrollBarWidth + 2 : 0;
        var app = Math.Max(150, (int)(width * 0.18));
        var foreground = Math.Max(92, (int)(width * 0.12));
        var background = Math.Max(92, (int)(width * 0.12));
        var total = Math.Max(92, (int)(width * 0.12));
        var share = Math.Max(72, (int)(width * 0.08));
        var title = Math.Max(180, width - app - foreground - background - total - share + rightEdgeCover);

        _usageList.Columns[0].Width = app;
        _usageList.Columns[1].Width = foreground;
        _usageList.Columns[2].Width = background;
        _usageList.Columns[3].Width = total;
        _usageList.Columns[4].Width = share;
        _usageList.Columns[5].Width = title;
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
            Title = _texts.ExportDialogTitle,
            Filter = _texts.DataFilter,
            FileName = $"WindowsScreenTime-{DateTime.Now:yyyyMMdd-HHmm}.wstdata"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _tracker.ExportTo(dialog.FileName, _settings);
            MessageBox.Show(_texts.DataExported, _texts.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{_texts.ExportFailed} {ex.Message}", _texts.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportData()
    {
        using var dialog = new OpenFileDialog
        {
            Title = _texts.ImportDialogTitle,
            Filter = _texts.ImportFilter
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var confirm = MessageBox.Show(
            _texts.ImportConfirm,
            _texts.AppName,
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
                ApplyTexts(Texts.Resolve(_settings.Language));
            }
            RefreshData();
            MessageBox.Show(_texts.DataImported, _texts.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{_texts.ImportFailed} {ex.Message}", _texts.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetData()
    {
        var confirm = MessageBox.Show(
            _texts.ResetConfirm,
            _texts.AppName,
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning);
        if (confirm == DialogResult.OK)
        {
            _tracker.Reset();
        }
    }

    private void OpenSettings()
    {
        using var dialog = new SettingsForm(_settings, _theme, _texts);
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
            ApplyTexts(Texts.Resolve(_settings.Language));
            RefreshData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{_texts.SaveSettingsFailed} {ex.Message}", _texts.AppName,
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
        ApplyListViewSystemTheme();
        _usageList.Invalidate();
        if (_notifyIcon.ContextMenuStrip is not null)
        {
            _notifyIcon.ContextMenuStrip.BackColor = _theme.Surface;
            _notifyIcon.ContextMenuStrip.ForeColor = _theme.Text;
        }
        RefreshNavState();
        RefreshChartTabs();
        Invalidate(true);
    }

    private void ApplyTexts(Texts texts)
    {
        _texts = texts;
        Text = $"{_texts.AppName} {AppInfo.Version}";
        _todayNav.Text = _texts.Today;
        _weekNav.Text = _texts.Week;
        _exportNav.Text = _texts.ExportData;
        _importNav.Text = _texts.ImportData;
        _settingsNav.Text = _texts.Settings;
        _resetNav.Text = _texts.ResetStats;
        _barTab.Text = _texts.BarChart;
        _ringTab.Text = _texts.RingChart;
        _viewSectionLabel.Text = _texts.View;
        _actionSectionLabel.Text = _texts.Actions;
        _versionLabel.Text = $"{_texts.Version} {AppInfo.Version}\r\n{_texts.Author}: {AppInfo.Author}\r\n{AppInfo.ProjectUrl}";
        _bootLabel.Text = _texts.Uptime;
        _foregroundLabel.Text = _texts.ForegroundTime;
        _backgroundLabel.Text = _texts.BackgroundTime;
        _currentLabel.Text = _texts.CurrentApp;
        _usageTitleLabel.Text = _texts.UsageDetails;
        _emptyState.Text = _texts.EmptyDetails;
        _chart.ApplyTexts(_texts);
        _ringChart.ApplyTexts(_texts);
        UpdateUsageColumnHeaders();
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.ContextMenuStrip = CreateTrayMenu();
        RefreshChartTabs();
    }

    private void DrawUsageColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        using var back = new SolidBrush(_theme.IsDark ? _theme.ListAlt : _theme.Surface);
        using var line = new Pen(_theme.Line);
        e.Graphics.FillRectangle(back, e.Bounds);
        var textBounds = new Rectangle(e.Bounds.Left + 6, e.Bounds.Top, e.Bounds.Width - 10, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? string.Empty, Font ?? SystemFonts.MessageBoxFont, textBounds, _theme.Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        e.Graphics.DrawLine(line, e.Bounds.Right - 1, e.Bounds.Top + 4, e.Bounds.Right - 1, e.Bounds.Bottom - 4);
        e.Graphics.DrawLine(line, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }

    private void DrawUsageSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.Item is null || e.SubItem is null)
        {
            return;
        }

        var selected = e.Item.Selected;
        var rowBack = selected
            ? _theme.NavActive
            : e.ItemIndex % 2 == 0 ? _theme.Surface : _theme.ListAlt;
        var rowText = e.Item.ForeColor;
        using var back = new SolidBrush(rowBack);
        e.Graphics.FillRectangle(back, e.Bounds);
        var textBounds = new Rectangle(e.Bounds.Left + 6, e.Bounds.Top, e.Bounds.Width - 10, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, e.SubItem.Text, Font ?? SystemFonts.MessageBoxFont, textBounds, rowText,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }

    private void ApplyListViewSystemTheme()
    {
        if (!_usageList.IsHandleCreated)
        {
            return;
        }

        try
        {
            SetWindowTheme(_usageList.Handle, _theme.IsDark ? "DarkMode_Explorer" : "Explorer", null);
            var header = SendMessage(_usageList.Handle, 0x101F, IntPtr.Zero, IntPtr.Zero);
            if (header != IntPtr.Zero)
            {
                SetWindowTheme(header, _theme.IsDark ? "DarkMode_ItemsView" : "Explorer", null);
            }
        }
        catch
        {
            // Owner-drawn colors still keep the list readable on systems that ignore this theme hint.
        }
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

    private sealed class FocuslessButton : Button
    {
        protected override bool ShowFocusCues => false;
    }

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? pszSubAppName, string? pszSubIdList);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
