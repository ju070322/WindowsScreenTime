using System.Text.Json;

namespace WindowsScreenTimeApp;

public sealed class UsageImportResult
{
    public AppSettings? Settings { get; init; }
    public IReadOnlyList<AppUsage> Items { get; init; } = [];
}

public static class UsageDataStore
{
    private const int CurrentVersion = 2;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string UsagePath => Path.Combine(AppSettings.AppDataDirectory, "usage.json");

    public static Dictionary<string, AppUsage> Load()
    {
        Directory.CreateDirectory(AppSettings.AppDataDirectory);
        if (!File.Exists(UsagePath))
        {
            return [];
        }

        try
        {
            var package = JsonSerializer.Deserialize<UsagePackage>(File.ReadAllText(UsagePath));
            var items = (package?.Usage ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item.AppName))
                .Select(ToAppUsage)
                .ToList();
            return items.ToDictionary(GetKey, item => item);
        }
        catch
        {
            return [];
        }
    }

    public static void Save(IEnumerable<AppUsage> items)
    {
        Directory.CreateDirectory(AppSettings.AppDataDirectory);
        var package = new UsagePackage
        {
            Version = CurrentVersion,
            ExportedAt = DateTimeOffset.Now,
            Usage = items.Select(ToPersisted).ToList()
        };
        File.WriteAllText(UsagePath, JsonSerializer.Serialize(package, JsonOptions));
    }

    public static void Export(string path, AppSettings settings, IEnumerable<AppUsage> items)
    {
        var package = new UsagePackage
        {
            Version = CurrentVersion,
            ExportedAt = DateTimeOffset.Now,
            Settings = settings,
            Usage = items.Select(ToPersisted).ToList()
        };
        File.WriteAllText(path, JsonSerializer.Serialize(package, JsonOptions));
    }

    public static UsageImportResult Import(string path)
    {
        var package = JsonSerializer.Deserialize<UsagePackage>(File.ReadAllText(path))
            ?? throw new InvalidOperationException("导入文件格式不正确。");

        return new UsageImportResult
        {
            Settings = package.Settings,
            Items = (package.Usage ?? []).Select(ToAppUsage).ToList()
        };
    }

    public static string GetKey(AppUsage item) =>
        item.IsIdle
            ? $"{item.Date:yyyy-MM-dd}|Idle"
            : $"{item.Date:yyyy-MM-dd}|{item.AppName}|{item.ExePath}";

    public static string GetSummaryKey(AppUsage item) =>
        item.IsIdle ? "Idle" : $"{item.AppName}|{item.ExePath}";

    private static PersistedUsageItem ToPersisted(AppUsage item) => new()
    {
        Date = item.Date.ToString("yyyy-MM-dd"),
        AppName = item.AppName,
        ExePath = item.ExePath,
        WindowTitle = item.WindowTitle,
        ForegroundSeconds = Math.Max(0, item.ForegroundDuration.TotalSeconds),
        BackgroundSeconds = Math.Max(0, item.BackgroundDuration.TotalSeconds),
        IsIdle = item.IsIdle
    };

    private static AppUsage ToAppUsage(PersistedUsageItem item)
    {
        var foregroundSeconds = item.ForegroundSeconds;
        var backgroundSeconds = item.BackgroundSeconds;

        if (foregroundSeconds <= 0 && backgroundSeconds <= 0 && item.DurationSeconds > 0)
        {
            foregroundSeconds = item.DurationSeconds;
        }

        return new AppUsage
        {
            Date = DateOnly.TryParse(item.Date, out var date)
                ? date
                : DateOnly.FromDateTime(DateTime.Now),
            AppName = item.AppName,
            ExePath = item.ExePath,
            WindowTitle = item.WindowTitle,
            ForegroundDuration = TimeSpan.FromSeconds(Math.Max(0, foregroundSeconds)),
            BackgroundDuration = TimeSpan.FromSeconds(Math.Max(0, backgroundSeconds)),
            IsIdle = item.IsIdle
        };
    }

    private sealed class UsagePackage
    {
        public int Version { get; set; }
        public DateTimeOffset ExportedAt { get; set; }
        public AppSettings? Settings { get; set; }
        public List<PersistedUsageItem>? Usage { get; set; }
    }

    private sealed class PersistedUsageItem
    {
        public string Date { get; set; } = "";
        public string AppName { get; set; } = "";
        public string ExePath { get; set; } = "";
        public string WindowTitle { get; set; } = "";
        public double ForegroundSeconds { get; set; }
        public double BackgroundSeconds { get; set; }
        public double DurationSeconds { get; set; }
        public bool IsIdle { get; set; }
    }
}
