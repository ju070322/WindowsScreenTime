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
            MessageBox.Show("Windows Screen Time 已经在运行。请从系统托盘打开。", "Windows Screen Time",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.Run(new MainForm());
        _singleInstance.ReleaseMutex();
    }
}
