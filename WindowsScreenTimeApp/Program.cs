namespace WindowsScreenTimeApp;

static class Program
{
    private static Mutex? _singleInstance;

    [STAThread]
    static void Main()
    {
        _singleInstance = new Mutex(true, "WindowsScreenTimeApp.SingleInstance", out var created);
        if (!created)
        {
            MessageBox.Show("Windows Screen Time \u5df2\u7ecf\u5728\u8fd0\u884c\u3002\u8bf7\u4ece\u7cfb\u7edf\u6258\u76d8\u6253\u5f00\u3002", "Windows Screen Time",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.Run(new MainForm());
        _singleInstance.ReleaseMutex();
    }
}
