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
        Text = "\u8bbe\u7f6e";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 410);
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
            Text = "\u8bbe\u7f6e",
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
            Padding = new Padding(16)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        for (var i = 0; i < 7; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        }
        root.Controls.Add(panel, 0, 1);

        AddNumberRow(panel, 0, "\u91c7\u6837\u95f4\u9694\uff08\u79d2\uff09", _sampleSeconds, 1, 60);
        AddNumberRow(panel, 1, "\u7a7a\u95f2\u5224\u5b9a\uff08\u5206\u949f\uff09", _idleMinutes, 1, 240);
        AddCheckRow(panel, 2, "\u5f00\u673a\u81ea\u542f", _startWithWindows);
        AddCheckRow(panel, 3, "\u542f\u52a8\u540e\u76f4\u63a5\u6700\u5c0f\u5316\u5230\u540e\u53f0", _startMinimized);
        AddCheckRow(panel, 4, "\u70b9\u51fb\u5173\u95ed\u6309\u94ae\u65f6\u6700\u5c0f\u5316\u5230\u540e\u53f0", _minimizeOnClose);
        AddCheckRow(panel, 5, "\u5217\u8868\u4e2d\u663e\u793a\u7a7a\u95f2\u65f6\u95f4", _includeIdle);

        var hint = new Label
        {
            Dock = DockStyle.Fill,
            Text = "\u8bbe\u7f6e\u4f1a\u4fdd\u5b58\u5728\u5f53\u524d\u7528\u6237\u7684\u672c\u5730\u5e94\u7528\u6570\u636e\u76ee\u5f55\u3002",
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
        var ok = MakeButton("\u4fdd\u5b58", DialogResult.OK);
        var cancel = MakeButton("\u53d6\u6d88", DialogResult.Cancel);
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
