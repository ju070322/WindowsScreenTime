using System.Text.Json;

namespace WindowsScreenTimeApp;

public enum AppThemeMode
{
    Light,
    Dark,
    System
}

public sealed class AppSettings
{
    public int SampleSeconds { get; set; } = 3;
    public int IdleMinutes { get; set; } = 10;
    public bool StartMinimized { get; set; }
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool IncludeIdleInList { get; set; } = true;
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;

    public static string AppDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WindowsScreenTime");

    public static string SettingsPath => Path.Combine(AppDataDirectory, "settings.json");

    public static AppSettings Load()
    {
        Directory.CreateDirectory(AppDataDirectory);
        if (!File.Exists(SettingsPath))
        {
            var defaults = new AppSettings
            {
                StartWithWindows = StartupManager.IsEnabled()
            };
            defaults.Save();
            return defaults;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings();
            settings.SampleSeconds = Math.Clamp(settings.SampleSeconds, 1, 60);
            settings.IdleMinutes = Math.Clamp(settings.IdleMinutes, 1, 240);
            settings.StartWithWindows = StartupManager.IsEnabled();
            if (!Enum.IsDefined(settings.ThemeMode))
            {
                settings.ThemeMode = AppThemeMode.System;
            }
            return settings;
        }
        catch
        {
            return new AppSettings
            {
                StartWithWindows = StartupManager.IsEnabled()
            };
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(AppDataDirectory);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    public void CopyFrom(AppSettings other)
    {
        SampleSeconds = Math.Clamp(other.SampleSeconds, 1, 60);
        IdleMinutes = Math.Clamp(other.IdleMinutes, 1, 240);
        StartMinimized = other.StartMinimized;
        MinimizeToTrayOnClose = other.MinimizeToTrayOnClose;
        StartWithWindows = other.StartWithWindows;
        IncludeIdleInList = other.IncludeIdleInList;
        ThemeMode = Enum.IsDefined(other.ThemeMode) ? other.ThemeMode : AppThemeMode.System;
    }
}
