namespace WindowsScreenTimeApp;

public sealed class BarChartPanel : Control
{
    private readonly Color[] _palette =
    [
        Color.FromArgb(47, 111, 237),
        Color.FromArgb(18, 128, 92),
        Color.FromArgb(180, 83, 9),
        Color.FromArgb(110, 87, 224),
        Color.FromArgb(0, 120, 212),
        Color.FromArgb(196, 43, 28),
        Color.FromArgb(3, 131, 135),
        Color.FromArgb(136, 23, 152)
    ];

    private List<AppUsage> _items = [];

    public BarChartPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        ForeColor = Color.FromArgb(29, 36, 48);
        Font = new Font("Microsoft YaHei UI", 9F);
    }

    public void SetItems(IEnumerable<AppUsage> items)
    {
        _items = items
            .Where(item => !item.IsIdle && item.Duration.TotalSeconds > 0)
            .OrderByDescending(item => item.Duration)
            .Take(8)
            .ToList();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.Clear(BackColor);

        var bounds = ClientRectangle;
        if (bounds.Width < 100 || bounds.Height < 100)
        {
            return;
        }

        using var titleFont = new Font(Font.FontFamily, 13F, FontStyle.Bold);
        using var mutedBrush = new SolidBrush(Color.FromArgb(102, 112, 133));
        using var textBrush = new SolidBrush(ForeColor);
        e.Graphics.DrawString("使用时间柱状图", titleFont, textBrush, 0, 0);

        if (_items.Count == 0)
        {
            DrawCenteredText(e.Graphics, "统计几秒后会显示柱状图。", bounds, mutedBrush);
            return;
        }

        var chart = new Rectangle(0, 42, bounds.Width, bounds.Height - 44);
        var labelHeight = 48;
        var valueHeight = 22;
        var plot = new Rectangle(chart.X + 8, chart.Y + valueHeight, chart.Width - 16, chart.Height - labelHeight - valueHeight);
        var maxSeconds = Math.Max(1, _items.Max(item => item.Duration.TotalSeconds));
        var slotWidth = plot.Width / Math.Max(1, _items.Count);
        var barWidth = Math.Max(22, Math.Min(56, (int)(slotWidth * 0.48)));

        using var gridPen = new Pen(Color.FromArgb(232, 236, 244));
        for (var i = 0; i <= 3; i++)
        {
            var y = plot.Bottom - (plot.Height * i / 3);
            e.Graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);
        }

        for (var index = 0; index < _items.Count; index++)
        {
            var item = _items[index];
            var centerX = plot.Left + slotWidth * index + slotWidth / 2;
            var barHeight = Math.Max(4, (int)(plot.Height * item.Duration.TotalSeconds / maxSeconds));
            var bar = new Rectangle(
                centerX - barWidth / 2,
                plot.Bottom - barHeight,
                barWidth,
                barHeight);

            using var fill = new SolidBrush(_palette[index % _palette.Length]);
            using var path = RoundedRect(bar, 7);
            e.Graphics.FillPath(fill, path);

            var duration = UiFormat.Duration(item.Duration);
            DrawCenteredLine(e.Graphics, duration, Font, textBrush, new Rectangle(centerX - slotWidth / 2, plot.Top - valueHeight, slotWidth, valueHeight));
            DrawLabel(e.Graphics, item.AppName, new Rectangle(centerX - slotWidth / 2, plot.Bottom + 8, slotWidth, labelHeight), mutedBrush);
        }
    }

    private static void DrawCenteredText(Graphics graphics, string text, Rectangle bounds, Brush brush)
    {
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        using var fallbackFont = new Font("Microsoft YaHei UI", 9F);
        graphics.DrawString(text, SystemFonts.MessageBoxFont ?? fallbackFont, brush, bounds, format);
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

    private void DrawLabel(Graphics graphics, string text, Rectangle bounds, Brush brush)
    {
        using var smallFont = new Font(Font.FontFamily, 8.3F);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.EllipsisCharacter
        };
        graphics.DrawString(text, smallFont, brush, bounds, format);
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        var diameter = radius * 2;
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
