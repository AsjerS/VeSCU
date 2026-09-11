namespace VeSCU;

internal static class Program
{
    [STAThread]
    static async Task Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        Application.ThreadException += (_, e) =>
            AppDialogs.Error(e.Exception);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            AppDialogs.Error((Exception)e.ExceptionObject);

        try
        {
            AppConfig? config = AppConfig.Load();
            if (config is null || !AppEnvironment.EnsurePrerequisites(config)) return;

            Application.Run(new ScreenshotContext(config));
        }
        catch (Exception ex)
        {
            AppDialogs.Error(ex);
        }
    }
}
