namespace WindowsScreenTimeApp;

public sealed class ActivityTracker : IDisposable
{
    private readonly Dictionary<string, AppUsage> _usage;
    private readonly System.Windows.Forms.Timer _timer = new();
    private DateTimeOffset _lastTick = DateTimeOffset.Now;
    private ForegroundWindowInfo _current = ForegroundWindowInfo.Idle();
    private AppSettings _settings;

    public event EventHandler? Updated;

    public ActivityTracker(AppSettings settings)
    {
        _settings = settings;
        _usage = UsageDataStore.Load();
        _timer.Tick += OnTick;
        ApplySettings(settings);
    }

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.Now;

    public ForegroundWindowInfo Current => _current;

    public UsagePeriod Period { get; private set; } = UsagePeriod.Day;

    public IReadOnlyCollection<AppUsage> RawItems => _usage.Values.ToList();

    public IReadOnlyCollection<AppUsage> Items => BuildSummary(Period);

    public TimeSpan ActiveTotal =>
        TimeSpan.FromSeconds(Items.Where(item => !item.IsIdle).Sum(item => item.Duration.TotalSeconds));

    public TimeSpan ForegroundTotal =>
        TimeSpan.FromSeconds(Items.Where(item => !item.IsIdle).Sum(item => item.ForegroundDuration.TotalSeconds));

    public TimeSpan BackgroundTotal =>
        TimeSpan.FromSeconds(Items.Where(item => !item.IsIdle).Sum(item => item.BackgroundDuration.TotalSeconds));

    public void Start()
    {
        _current = ForegroundWindowReader.Read(TimeSpan.FromMinutes(_settings.IdleMinutes));
        _lastTick = DateTimeOffset.Now;
        EnsureUsage(_current, DateOnly.FromDateTime(DateTime.Now));
        _timer.Start();
        Updated?.Invoke(this, EventArgs.Empty);
    }

    public void SetPeriod(UsagePeriod period)
    {
        Period = period;
        Updated?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        _usage.Clear();
        _lastTick = DateTimeOffset.Now;
        _current = ForegroundWindowReader.Read(TimeSpan.FromMinutes(_settings.IdleMinutes));
        EnsureUsage(_current, DateOnly.FromDateTime(DateTime.Now));
        UsageDataStore.Save(_usage.Values);
        Updated?.Invoke(this, EventArgs.Empty);
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        _timer.Interval = Math.Clamp(settings.SampleSeconds, 1, 60) * 1000;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        AddElapsedUntilNow();

        _current = ForegroundWindowReader.Read(TimeSpan.FromMinutes(_settings.IdleMinutes));
        EnsureUsage(_current, DateOnly.FromDateTime(DateTime.Now)).WindowTitle = _current.WindowTitle;
        UsageDataStore.Save(_usage.Values);
        Updated?.Invoke(this, EventArgs.Empty);
    }

    public void ExportTo(string path, AppSettings settings)
    {
        AddElapsedUntilNow();
        UsageDataStore.Save(_usage.Values);
        UsageDataStore.Export(path, settings, _usage.Values);
        Updated?.Invoke(this, EventArgs.Empty);
    }

    public UsageImportResult ImportFrom(string path)
    {
        AddElapsedUntilNow();
        var result = UsageDataStore.Import(path);
        foreach (var imported in result.Items)
        {
            var key = UsageDataStore.GetKey(imported);
            if (_usage.TryGetValue(key, out var existing))
            {
                existing.ForegroundDuration += imported.ForegroundDuration;
                existing.BackgroundDuration += imported.BackgroundDuration;
                if (!string.IsNullOrWhiteSpace(imported.WindowTitle))
                {
                    existing.WindowTitle = imported.WindowTitle;
                }
            }
            else
            {
                _usage[key] = imported;
            }
        }

        UsageDataStore.Save(_usage.Values);
        Updated?.Invoke(this, EventArgs.Empty);
        return result;
    }

    private IReadOnlyCollection<AppUsage> BuildSummary(UsagePeriod period)
    {
        var start = GetPeriodStart(period);
        return _usage.Values
            .Where(item => item.Date >= start)
            .GroupBy(UsageDataStore.GetSummaryKey)
            .Select(group =>
            {
                var first = group.First();
                return new AppUsage
                {
                    Date = DateOnly.MinValue,
                    AppName = first.AppName,
                    ExePath = first.ExePath,
                    WindowTitle = group.OrderByDescending(item => item.Date).First().WindowTitle,
                    ForegroundDuration = TimeSpan.FromSeconds(group.Sum(item => item.ForegroundDuration.TotalSeconds)),
                    BackgroundDuration = TimeSpan.FromSeconds(group.Sum(item => item.BackgroundDuration.TotalSeconds)),
                    IsIdle = first.IsIdle
                };
            })
            .OrderByDescending(item => item.Duration)
            .ToList();
    }

    private static DateOnly GetPeriodStart(UsagePeriod period)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (period == UsagePeriod.Day)
        {
            return today;
        }

        var diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return today.AddDays(-diff);
    }

    private void AddElapsedUntilNow()
    {
        var now = DateTimeOffset.Now;
        var elapsed = now - _lastTick;
        if (elapsed <= TimeSpan.Zero)
        {
            elapsed = TimeSpan.FromMilliseconds(_timer.Interval);
        }

        var date = DateOnly.FromDateTime(_lastTick.LocalDateTime);
        if (_current.IsIdle)
        {
            EnsureUsage(_current, date).ForegroundDuration += elapsed;
        }
        else
        {
            var foreground = EnsureUsage(_current, date);
            foreground.ForegroundDuration += elapsed;

            foreach (var app in ForegroundWindowReader.ReadVisibleApps())
            {
                if (app.AppKey == _current.AppKey)
                {
                    continue;
                }

                var background = EnsureUsage(app, date);
                background.BackgroundDuration += elapsed;
                background.WindowTitle = app.WindowTitle;
            }
        }

        _lastTick = now;
    }

    private AppUsage EnsureUsage(ForegroundWindowInfo info, DateOnly date)
    {
        var key = info.IsIdle ? $"{date:yyyy-MM-dd}|Idle" : $"{date:yyyy-MM-dd}|{info.AppName}|{info.ExePath}";
        if (_usage.TryGetValue(key, out var usage))
        {
            return usage;
        }

        usage = new AppUsage
        {
            Date = date,
            AppName = info.AppName,
            ExePath = info.ExePath,
            WindowTitle = info.WindowTitle,
            IsIdle = info.IsIdle,
            ForegroundDuration = TimeSpan.Zero,
            BackgroundDuration = TimeSpan.Zero
        };
        _usage[key] = usage;
        return usage;
    }

    public void Dispose()
    {
        AddElapsedUntilNow();
        UsageDataStore.Save(_usage.Values);
        _timer.Dispose();
    }
}
