namespace WindowsScreenTimeApp;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly AppTheme _theme;
    private readonly Texts _texts;
    private readonly NumericUpDown _sampleSeconds = new();
    private readonly NumericUpDown _idleMinutes = new();
    private readonly CheckBox _startWithWindows = new();
    private readonly CheckBox _startMinimized = new();
    private readonly CheckBox _minimizeOnClose = new();
    private readonly CheckBox _includeIdle = new();
    private readonly ComboBox _themeMode = new();
    private readonly ComboBox _language = new();

    public SettingsForm(AppSettings settings, AppTheme theme, Texts texts)
    {
        _settings = settings;
        _theme = theme;
        _texts = texts;
        Text = _texts.Settings;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 520);
        BackColor = _theme.Window;
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = _texts.Settings,
            Font = new Font(Font.FontFamily, 18F, FontStyle.Bold),
            ForeColor = _theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 10,
            BackColor = _theme.Surface,
            Padding = new Padding(16)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        for (var i = 0; i < 8; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        }
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.Controls.Add(panel, 0, 1);

        AddNumberRow(panel, 0, _texts.SampleSeconds, _sampleSeconds, 1, 60);
        AddNumberRow(panel, 1, _texts.IdleMinutes, _idleMinutes, 1, 240);
        AddCheckRow(panel, 2, _texts.StartWithWindows, _startWithWindows);
        AddCheckRow(panel, 3, _texts.StartMinimized, _startMinimized);
        AddCheckRow(panel, 4, _texts.MinimizeOnClose, _minimizeOnClose);
        AddCheckRow(panel, 5, _texts.IncludeIdle, _includeIdle);
        AddThemeRow(panel, 6);
        AddLanguageRow(panel, 7);

        var hint = new Label
        {
            Dock = DockStyle.Fill,
            Text = _texts.SettingsHint,
            ForeColor = _theme.Muted,
            TextAlign = ContentAlignment.TopLeft,
            AutoEllipsis = true,
            Padding = new Padding(0, 10, 0, 0)
        };
        panel.Controls.Add(hint, 0, 8);
        panel.SetColumnSpan(hint, 2);

        var buttonHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BackColor,
            Padding = new Padding(0, 14, 4, 10)
        };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 198,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = BackColor,
            WrapContents = false
        };
        var ok = MakeButton(_texts.Save, DialogResult.OK);
        var cancel = MakeButton(_texts.Cancel, DialogResult.Cancel);
        ok.Click += (_, _) => SaveValues();
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        buttonHost.Controls.Add(buttons);
        AcceptButton = ok;
        CancelButton = cancel;
        root.Controls.Add(buttonHost, 0, 2);
    }

    private static Button MakeButton(string text, DialogResult result) => new()
    {
        Text = text,
        Width = 92,
        Height = 34,
        Margin = new Padding(6, 0, 0, 0),
        DialogResult = result,
        FlatStyle = FlatStyle.System
    };

    private void AddNumberRow(TableLayoutPanel panel, int row, string labelText, NumericUpDown input, int min, int max)
    {
        panel.Controls.Add(MakeLabel(labelText), 0, row);
        input.Dock = DockStyle.Fill;
        input.Minimum = min;
        input.Maximum = max;
        input.TextAlign = HorizontalAlignment.Right;
        input.BackColor = _theme.Surface;
        input.ForeColor = _theme.Text;
        panel.Controls.Add(input, 1, row);
    }

    private void AddCheckRow(TableLayoutPanel panel, int row, string labelText, CheckBox input)
    {
        panel.Controls.Add(MakeLabel(labelText), 0, row);
        input.Dock = DockStyle.Left;
        input.AutoSize = true;
        input.ForeColor = _theme.Text;
        input.BackColor = _theme.Surface;
        panel.Controls.Add(input, 1, row);
    }

    private void AddThemeRow(TableLayoutPanel panel, int row)
    {
        panel.Controls.Add(MakeLabel(_texts.Theme), 0, row);
        _themeMode.Dock = DockStyle.Fill;
        _themeMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _themeMode.Items.AddRange(
        [
            new ThemeOption(AppThemeMode.System, _texts.FollowSystem),
            new ThemeOption(AppThemeMode.Light, _texts.Light),
            new ThemeOption(AppThemeMode.Dark, _texts.Dark)
        ]);
        _themeMode.BackColor = _theme.Surface;
        _themeMode.ForeColor = _theme.Text;
        panel.Controls.Add(_themeMode, 1, row);
    }

    private void AddLanguageRow(TableLayoutPanel panel, int row)
    {
        panel.Controls.Add(MakeLabel(_texts.Language), 0, row);
        _language.Dock = DockStyle.Fill;
        _language.DropDownStyle = ComboBoxStyle.DropDownList;
        _language.Items.AddRange(
        [
            new LanguageOption(AppLanguage.System, _texts.FollowSystem),
            new LanguageOption(AppLanguage.ChineseSimplified, _texts.SimplifiedChinese),
            new LanguageOption(AppLanguage.English, _texts.English)
        ]);
        _language.BackColor = _theme.Surface;
        _language.ForeColor = _theme.Text;
        panel.Controls.Add(_language, 1, row);
    }

    private Label MakeLabel(string text) => new()
    {
        Dock = DockStyle.Fill,
        Text = text,
        ForeColor = _theme.Text,
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
        for (var i = 0; i < _themeMode.Items.Count; i++)
        {
            if (_themeMode.Items[i] is ThemeOption option && option.Mode == _settings.ThemeMode)
            {
                _themeMode.SelectedIndex = i;
                break;
            }
        }

        if (_themeMode.SelectedIndex < 0)
        {
            _themeMode.SelectedIndex = 0;
        }

        for (var i = 0; i < _language.Items.Count; i++)
        {
            if (_language.Items[i] is LanguageOption option && option.Language == _settings.Language)
            {
                _language.SelectedIndex = i;
                break;
            }
        }

        if (_language.SelectedIndex < 0)
        {
            _language.SelectedIndex = 0;
        }
    }

    private void SaveValues()
    {
        _settings.SampleSeconds = (int)_sampleSeconds.Value;
        _settings.IdleMinutes = (int)_idleMinutes.Value;
        _settings.StartWithWindows = _startWithWindows.Checked;
        _settings.StartMinimized = _startMinimized.Checked;
        _settings.MinimizeToTrayOnClose = _minimizeOnClose.Checked;
        _settings.IncludeIdleInList = _includeIdle.Checked;
        if (_themeMode.SelectedItem is ThemeOption option)
        {
            _settings.ThemeMode = option.Mode;
        }
        if (_language.SelectedItem is LanguageOption language)
        {
            _settings.Language = language.Language;
        }
    }

    private sealed class ThemeOption(AppThemeMode mode, string label)
    {
        public AppThemeMode Mode { get; } = mode;
        public override string ToString() => label;
    }

    private sealed class LanguageOption(AppLanguage language, string label)
    {
        public AppLanguage Language { get; } = language;
        public override string ToString() => label;
    }
}
