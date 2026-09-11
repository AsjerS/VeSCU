using System.Diagnostics;

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
            ShowAppError(e.Exception);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            ShowAppError(e.ExceptionObject as Exception);

        try
        {
            AppConfig? config = AppConfig.Load();
            if (config is null || !AppEnvironment.EnsurePrerequisites(config)) return;

            Application.Run(new ScreenshotContext(config));
        }
        catch (Exception ex)
        {
            ShowAppError(ex);
        }
    }

    public static void ShowConfigError(string message)
    {
        var prompt = MessageBox.Show(
            $"{message}\n\nWould you like to open the config to fix it?",
            "Configuration Error",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        );

        if (prompt == DialogResult.Yes)
        {
            Process.Start(new ProcessStartInfo(AppPaths.ConfigFile) { UseShellExecute = true });
        }
    }

    public static void ShowAppError(Exception? ex)
    {
        if (ex is null) return;
        MessageBox.Show(
            ex.Message,
            "Application Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );
    }
}
