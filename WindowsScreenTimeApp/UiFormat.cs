namespace WindowsScreenTimeApp;

public static class UiFormat
{
    public static string Duration(TimeSpan value)
    {
        if (value.TotalHours >= 1)
        {
            return $"{(int)value.TotalHours}小时 {value.Minutes}分钟";
        }

        if (value.TotalMinutes >= 1)
        {
            return $"{(int)value.TotalMinutes}分钟 {value.Seconds}秒";
        }

        return $"{Math.Max(0, value.Seconds)}秒";
    }
}
