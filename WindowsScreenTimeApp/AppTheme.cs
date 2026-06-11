using Microsoft.Win32;

namespace WindowsScreenTimeApp;

public sealed class AppTheme
{
    public Color Window { get; init; }
    public Color Sidebar { get; init; }
    public Color Surface { get; init; }
    public Color NavActive { get; init; }
    public Color Text { get; init; }
    public Color Muted { get; init; }
    public Color Line { get; init; }
    public Color Accent { get; init; }
    public Color ListAlt { get; init; }
    public Color Grid { get; init; }
    public bool IsDark { get; init; }

    public static AppTheme Resolve(AppThemeMode mode)
    {
        if (mode == AppThemeMode.System)
        {
            mode = IsSystemDarkMode() ? AppThemeMode.Dark : AppThemeMode.Light;
        }

        return mode == AppThemeMode.Dark ? Dark : Light;
    }

    private static AppTheme Light => new()
    {
        Window = Color.FromArgb(243, 244, 248),
        Sidebar = Color.FromArgb(248, 249, 252),
        Surface = Color.White,
        NavActive = Color.FromArgb(230, 238, 255),
        Text = Color.FromArgb(29, 36, 48),
        Muted = Color.FromArgb(102, 112, 133),
        Line = Color.FromArgb(217, 222, 231),
        Accent = Color.FromArgb(47, 111, 237),
        ListAlt = Color.FromArgb(248, 250, 252),
        Grid = Color.FromArgb(232, 236, 244),
        IsDark = false
    };

    private static AppTheme Dark => new()
    {
        Window = Color.FromArgb(28, 31, 36),
        Sidebar = Color.FromArgb(24, 27, 32),
        Surface = Color.FromArgb(34, 38, 45),
        NavActive = Color.FromArgb(43, 56, 78),
        Text = Color.FromArgb(238, 242, 247),
        Muted = Color.FromArgb(154, 164, 179),
        Line = Color.FromArgb(61, 68, 80),
        Accent = Color.FromArgb(96, 165, 250),
        ListAlt = Color.FromArgb(38, 43, 51),
        Grid = Color.FromArgb(55, 62, 74),
        IsDark = true
    };

    private static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }
}
