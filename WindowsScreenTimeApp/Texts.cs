using System.Globalization;

namespace WindowsScreenTimeApp;

public sealed class Texts
{
    private readonly bool _english;

    private Texts(bool english)
    {
        _english = english;
    }

    public static Texts Resolve(AppLanguage language)
    {
        if (language == AppLanguage.System)
        {
            language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase)
                ? AppLanguage.ChineseSimplified
                : AppLanguage.English;
        }

        return new Texts(language == AppLanguage.English);
    }

    public string AppName => "Windows Screen Time";
    public string Today => _english ? "Today" : "\u4eca\u65e5\u4f7f\u7528\u65f6\u95f4";
    public string Week => _english ? "This week" : "\u672c\u5468\u4f7f\u7528\u65f6\u95f4";
    public string View => _english ? "View" : "\u89c6\u56fe";
    public string Actions => _english ? "Actions" : "\u64cd\u4f5c";
    public string ExportData => _english ? "Export data" : "\u5bfc\u51fa\u6570\u636e";
    public string ImportData => _english ? "Import data" : "\u5bfc\u5165\u6570\u636e";
    public string Settings => _english ? "Settings" : "\u8bbe\u7f6e";
    public string ResetStats => _english ? "Reset statistics" : "\u91cd\u7f6e\u7edf\u8ba1";
    public string Version => _english ? "Version" : "\u7248\u672c";
    public string Author => _english ? "Author" : "\u4f5c\u8005";
    public string Uptime => _english ? "Uptime" : "\u5f00\u673a\u65f6\u95f4";
    public string ForegroundTime => _english ? "Foreground" : "\u524d\u53f0\u65f6\u95f4";
    public string BackgroundTime => _english ? "Background" : "\u540e\u53f0\u65f6\u95f4";
    public string CurrentApp => _english ? "Current app" : "\u5f53\u524d\u5e94\u7528";
    public string BarChart => _english ? "Bar chart" : "\u67f1\u72b6\u56fe";
    public string RingChart => _english ? "Ring chart" : "\u5706\u73af\u56fe";
    public string UsageRanking => _english ? "App usage ranking" : "\u5e94\u7528\u4f7f\u7528\u65f6\u95f4\u6392\u884c";
    public string ForegroundBackgroundShare => _english ? "Foreground / background share" : "\u524d\u53f0 / \u540e\u53f0\u65f6\u95f4\u5360\u6bd4";
    public string TotalUsageTime => _english ? "Total usage" : "\u603b\u4f7f\u7528\u65f6\u95f4";
    public string UsageDetails => _english ? "App details" : "\u5e94\u7528\u660e\u7ec6";
    public string AppColumn => _english ? "App" : "\u5e94\u7528";
    public string ForegroundColumn => _english ? "Foreground" : "\u524d\u53f0";
    public string BackgroundColumn => _english ? "Background" : "\u540e\u53f0";
    public string TotalColumn => _english ? "Total" : "\u603b\u65f6\u95f4";
    public string ShareColumn => _english ? "Share" : "\u5360\u6bd4";
    public string RecentWindowTitle => _english ? "Recent window title" : "\u6700\u8fd1\u7a97\u53e3\u6807\u9898";
    public string EmptyDetails => _english ? "App details will appear after a few seconds." : "\u7edf\u8ba1\u51e0\u79d2\u540e\u4f1a\u663e\u793a\u5e94\u7528\u660e\u7ec6\u3002";
    public string EmptyBarChart => _english ? "The bar chart will appear after a few seconds." : "\u7edf\u8ba1\u51e0\u79d2\u540e\u4f1a\u663e\u793a\u67f1\u72b6\u56fe\u3002";
    public string EmptyRingChart => _english ? "The ring chart will appear after a few seconds." : "\u7edf\u8ba1\u51e0\u79d2\u540e\u4f1a\u663e\u793a\u5706\u73af\u56fe\u3002";
    public string Open => _english ? "Open" : "\u6253\u5f00";
    public string Exit => _english ? "Exit" : "\u9000\u51fa";
    public string DataExported => _english ? "Data exported." : "\u6570\u636e\u5df2\u5bfc\u51fa\u3002";
    public string DataImported => _english ? "Data imported and merged." : "\u6570\u636e\u5df2\u5bfc\u5165\u5e76\u5408\u5e76\u3002";
    public string SaveSettingsFailed => _english ? "Failed to save settings:" : "\u4fdd\u5b58\u8bbe\u7f6e\u5931\u8d25\uff1a";
    public string ExportFailed => _english ? "Export failed:" : "\u5bfc\u51fa\u5931\u8d25\uff1a";
    public string ImportFailed => _english ? "Import failed:" : "\u5bfc\u5165\u5931\u8d25\uff1a";
    public string ExportDialogTitle => _english ? "Export usage data" : "\u5bfc\u51fa\u4f7f\u7528\u6570\u636e";
    public string ImportDialogTitle => _english ? "Import usage data" : "\u5bfc\u5165\u4f7f\u7528\u6570\u636e";
    public string DataFilter => _english
        ? "Windows Screen Time data (*.wstdata)|*.wstdata|JSON files (*.json)|*.json"
        : "Windows Screen Time \u6570\u636e (*.wstdata)|*.wstdata|JSON \u6587\u4ef6 (*.json)|*.json";
    public string ImportFilter => _english
        ? "Windows Screen Time data (*.wstdata;*.json)|*.wstdata;*.json|All files (*.*)|*.*"
        : "Windows Screen Time \u6570\u636e (*.wstdata;*.json)|*.wstdata;*.json|\u6240\u6709\u6587\u4ef6 (*.*)|*.*";
    public string ImportConfirm => _english
        ? "Importing will merge usage time from the file into current statistics and inherit settings in that file. Continue?"
        : "\u5bfc\u5165\u4f1a\u628a\u6587\u4ef6\u4e2d\u7684\u4f7f\u7528\u65f6\u95f4\u5408\u5e76\u5230\u5f53\u524d\u7edf\u8ba1\uff0c\u5e76\u7ee7\u627f\u5bfc\u5165\u6587\u4ef6\u91cc\u7684\u8bbe\u7f6e\u3002\u662f\u5426\u7ee7\u7eed\uff1f";
    public string ResetConfirm => _english
        ? "Clear current accumulated statistics? Exported backup files will not be deleted."
        : "\u786e\u5b9a\u8981\u6e05\u7a7a\u5f53\u524d\u7d2f\u8ba1\u7edf\u8ba1\u5417\uff1f\u8fd9\u4e0d\u4f1a\u5220\u9664\u5bfc\u51fa\u7684\u5907\u4efd\u6587\u4ef6\u3002";
    public string CurrentNotify => _english ? "Current" : "\u5f53\u524d";
    public string TotalNotify => _english ? "Total" : "\u603b\u8ba1";
    public string SampleSeconds => _english ? "Sample interval (seconds)" : "\u91c7\u6837\u95f4\u9694\uff08\u79d2\uff09";
    public string IdleMinutes => _english ? "Idle threshold (minutes)" : "\u7a7a\u95f2\u5224\u5b9a\uff08\u5206\u949f\uff09";
    public string StartWithWindows => _english ? "Start with Windows" : "\u5f00\u673a\u81ea\u542f";
    public string StartMinimized => _english ? "Start minimized to background" : "\u542f\u52a8\u540e\u76f4\u63a5\u6700\u5c0f\u5316\u5230\u540e\u53f0";
    public string MinimizeOnClose => _english ? "Minimize to background when closing" : "\u70b9\u51fb\u5173\u95ed\u6309\u94ae\u65f6\u6700\u5c0f\u5316\u5230\u540e\u53f0";
    public string IncludeIdle => _english ? "Show idle time in list" : "\u5217\u8868\u4e2d\u663e\u793a\u7a7a\u95f2\u65f6\u95f4";
    public string Theme => _english ? "Theme" : "\u5916\u89c2\u4e3b\u9898";
    public string Language => _english ? "Language" : "\u8bed\u8a00";
    public string FollowSystem => _english ? "Follow system" : "\u8ddf\u968f\u7cfb\u7edf";
    public string Light => _english ? "Light" : "\u6d45\u8272";
    public string Dark => _english ? "Dark" : "\u6df1\u8272";
    public string SimplifiedChinese => _english ? "Simplified Chinese" : "\u7b80\u4f53\u4e2d\u6587";
    public string English => _english ? "English" : "English";
    public string Save => _english ? "Save" : "\u4fdd\u5b58";
    public string Cancel => _english ? "Cancel" : "\u53d6\u6d88";
    public string SettingsHint => _english
        ? "Settings are saved in the current user's local app data folder."
        : "\u8bbe\u7f6e\u4f1a\u4fdd\u5b58\u5728\u5f53\u524d\u7528\u6237\u7684\u672c\u5730\u5e94\u7528\u6570\u636e\u76ee\u5f55\u3002";
}
