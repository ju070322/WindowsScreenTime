namespace WindowsScreenTimeApp;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly NumericUpDown _sampleSeconds = new();
    private readonly NumericUpDown _idleMinutes = new();
    private readonly CheckBox _startWithWindows = new();
    private readonly CheckBox _startMinimized = new();
    private readonly CheckBox _minimizeOnClose = new();
    private readonly CheckBox _includeIdle = new();

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        Text = "设置";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 402);
        BackColor = Color.FromArgb(243, 244, 248);
        Font = new Font("Microsoft YaHei UI", 9F);
        Icon = Owner?.Icon ?? Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;

        BuildLayout();
        LoadValues();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(20),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "设置",
            Font = new Font(Font.FontFamily, 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(29, 36, 48),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            BackColor = Color.White,
            Padding = new Padding(16),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        for (var i = 0; i < 7; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        }
        root.Controls.Add(panel, 0, 1);

        AddNumberRow(panel, 0, "采样间隔（秒）", _sampleSeconds, 1, 60);
        AddNumberRow(panel, 1, "空闲判定（分钟）", _idleMinutes, 1, 240);
        AddCheckRow(panel, 2, "开机自启", _startWithWindows);
        AddCheckRow(panel, 3, "启动后直接最小化到后台", _startMinimized);
        AddCheckRow(panel, 4, "点击关闭按钮时最小化到后台", _minimizeOnClose);
        AddCheckRow(panel, 5, "列表中显示空闲时间", _includeIdle);

        var hint = new Label
        {
            Dock = DockStyle.Fill,
            Text = "设置会保存在当前用户的本地应用数据目录。",
            ForeColor = Color.FromArgb(102, 112, 133),
            TextAlign = ContentAlignment.MiddleLeft
        };
        panel.Controls.Add(hint, 0, 6);
        panel.SetColumnSpan(hint, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = BackColor,
            Padding = new Padding(0, 14, 0, 0)
        };
        var ok = MakeButton("保存", DialogResult.OK);
        var cancel = MakeButton("取消", DialogResult.Cancel);
        ok.Click += (_, _) => SaveValues();
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
        root.Controls.Add(buttons, 0, 2);
    }

    private static Button MakeButton(string text, DialogResult result) => new()
    {
        Text = text,
        Width = 92,
        Height = 32,
        DialogResult = result,
        FlatStyle = FlatStyle.System
    };

    private static void AddNumberRow(TableLayoutPanel panel, int row, string labelText, NumericUpDown input, int min, int max)
    {
        panel.Controls.Add(MakeLabel(labelText), 0, row);
        input.Dock = DockStyle.Fill;
        input.Minimum = min;
        input.Maximum = max;
        input.TextAlign = HorizontalAlignment.Right;
        panel.Controls.Add(input, 1, row);
    }

    private static void AddCheckRow(TableLayoutPanel panel, int row, string labelText, CheckBox input)
    {
        panel.Controls.Add(MakeLabel(labelText), 0, row);
        input.Dock = DockStyle.Left;
        input.AutoSize = true;
        panel.Controls.Add(input, 1, row);
    }

    private static Label MakeLabel(string text) => new()
    {
        Dock = DockStyle.Fill,
        Text = text,
        ForeColor = Color.FromArgb(29, 36, 48),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private void LoadValues()
    {
        _sampleSeconds.Value = _settings.SampleSeconds;
        _idleMinutes.Value = _settings.IdleMinutes;
        _startWithWindows.Checked = StartupManager.IsEnabled();
        _startMinimized.Checked = _settings.StartMinimized;
        _minimizeOnClose.Checked = _settings.MinimizeToTrayOnClose;
        _includeIdle.Checked = _settings.IncludeIdleInList;
    }

    private void SaveValues()
    {
        _settings.SampleSeconds = (int)_sampleSeconds.Value;
        _settings.IdleMinutes = (int)_idleMinutes.Value;
        _settings.StartWithWindows = _startWithWindows.Checked;
        _settings.StartMinimized = _startMinimized.Checked;
        _settings.MinimizeToTrayOnClose = _minimizeOnClose.Checked;
        _settings.IncludeIdleInList = _includeIdle.Checked;
    }
}
