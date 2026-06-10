namespace WindowsScreenTimeApp;

public enum UsagePeriod
{
    Day,
    Week
}

public sealed class AppUsage
{
    public DateOnly Date { get; init; } = DateOnly.FromDateTime(DateTime.Now);
    public string AppName { get; init; } = "";
    public string ExePath { get; init; } = "";
    public string WindowTitle { get; set; } = "";
    public TimeSpan ForegroundDuration { get; set; }
    public TimeSpan BackgroundDuration { get; set; }
    public bool IsIdle { get; init; }

    public TimeSpan Duration => ForegroundDuration + BackgroundDuration;

    public AppUsage CloneForSummary() => new()
    {
        Date = DateOnly.MinValue,
        AppName = AppName,
        ExePath = ExePath,
        WindowTitle = WindowTitle,
        ForegroundDuration = ForegroundDuration,
        BackgroundDuration = BackgroundDuration,
        IsIdle = IsIdle
    };
}
