namespace WindowsScreenTimeSetupBuilder;

static class Program
{
    [STAThread]
    static int Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new SetupForm());
        return 0;
    }
}
