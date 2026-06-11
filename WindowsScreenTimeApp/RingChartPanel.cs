namespace WindowsScreenTimeApp;

public sealed class RingChartPanel : Control
{
    private TimeSpan _foreground;
    private TimeSpan _background;
    private AppTheme _theme = AppTheme.Resolve(AppThemeMode.Light);
    private Texts _texts = Texts.Resolve(AppLanguage.System);

    public RingChartPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        ForeColor = Color.FromArgb(29, 36, 48);
        Font = new Font("Microsoft YaHei UI", 9F);
    }

    public void ApplyTheme(AppTheme theme)
    {
        _theme = theme;
        BackColor = theme.Surface;
        ForeColor = theme.Text;
        Invalidate();
    }

    public void ApplyTexts(Texts texts)
    {
        _texts = texts;
        Invalidate();
    }

    public void SetDurations(TimeSpan foreground, TimeSpan background)
    {
        _foreground = foreground;
        _background = background;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.Clear(BackColor);

        var total = _foreground + _background;
        using var titleFont = new Font(Font.FontFamily, 13F, FontStyle.Bold);
        using var textBrush = new SolidBrush(ForeColor);
        using var mutedBrush = new SolidBrush(_theme.Muted);
        e.Graphics.DrawString(_texts.ForegroundBackgroundShare, titleFont, textBrush, 0, 0);

        if (total.TotalSeconds <= 0)
        {
            DrawCenteredText(e.Graphics, _texts.EmptyRingChart, ClientRectangle, mutedBrush);
            return;
        }

        var chartSize = Math.Min(ClientSize.Width - 260, ClientSize.Height - 68);
        chartSize = Math.Max(140, Math.Min(chartSize, 240));
        var chart = new Rectangle(24, 54, chartSize, chartSize);
        var fgSweep = (float)(_foreground.TotalSeconds / total.TotalSeconds * 360);
        var bgSweep = 360 - fgSweep;

        using var basePen = new Pen(_theme.Grid, 24) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        using var fgPen = new Pen(Color.FromArgb(47, 111, 237), 24) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        using var bgPen = new Pen(Color.FromArgb(18, 128, 92), 24) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };

        e.Graphics.DrawArc(basePen, chart, 0, 360);
        e.Graphics.DrawArc(fgPen, chart, -90, fgSweep);
        if (bgSweep > 0.1f)
        {
            e.Graphics.DrawArc(bgPen, chart, -90 + fgSweep, bgSweep);
        }

        using var centerFont = new Font(Font.FontFamily, 16F, FontStyle.Bold);
        using var smallFont = new Font(Font.FontFamily, 8.5F);
        DrawCenteredLine(e.Graphics, UiFormat.Duration(total), centerFont, textBrush, chart);
        var sub = new Rectangle(chart.Left, chart.Top + chart.Height / 2 + 22, chart.Width, 24);
        DrawCenteredLine(e.Graphics, _texts.TotalUsageTime, smallFont, mutedBrush, sub);

        var legendX = chart.Right + 42;
        var legend = new Rectangle(legendX, 70, Math.Max(180, ClientSize.Width - legendX - 16), chart.Height);
        DrawLegend(e.Graphics, legend, _texts.ForegroundTime, _foreground, Color.FromArgb(47, 111, 237), total, 0);
        DrawLegend(e.Graphics, legend, _texts.BackgroundTime, _background, Color.FromArgb(18, 128, 92), total, 76);
    }

    private void DrawLegend(Graphics graphics, Rectangle bounds, string label, TimeSpan value, Color color, TimeSpan total, int yOffset)
    {
        var y = bounds.Top + yOffset;
        using var dot = new SolidBrush(color);
        using var textBrush = new SolidBrush(ForeColor);
        using var mutedBrush = new SolidBrush(_theme.Muted);
        using var bold = new Font(Font.FontFamily, 12F, FontStyle.Bold);
        using var normal = new Font(Font.FontFamily, 9F);
        graphics.FillEllipse(dot, bounds.Left, y + 8, 12, 12);
        graphics.DrawString(label, normal, mutedBrush, bounds.Left + 22, y);
        graphics.DrawString(UiFormat.Duration(value), bold, textBrush, bounds.Left + 22, y + 24);
        var percent = value.TotalSeconds / Math.Max(1, total.TotalSeconds);
        graphics.DrawString($"{percent:P1}", normal, mutedBrush, bounds.Left + 22, y + 50);
    }

    private static void DrawCenteredText(Graphics graphics, string text, Rectangle bounds, Brush brush)
    {
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        using var font = new Font("Microsoft YaHei UI", 9F);
        graphics.DrawString(text, font, brush, bounds, format);
    }

    private static void DrawCenteredLine(Graphics graphics, string text, Font font, Brush brush, Rectangle bounds)
    {
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };
        graphics.DrawString(text, font, brush, bounds, format);
    }
}
