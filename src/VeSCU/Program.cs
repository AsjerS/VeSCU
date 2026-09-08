using System.Diagnostics;

namespace VeSCU;

static class Program
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
            if (config is null) return;

            if (!EnsurePrerequisites(config)) return;

            Application.Run(new ScreenshotContext(config));
        }
        catch (Exception ex)
        {
            ShowAppError(ex);
        }
    }

    private static bool EnsurePrerequisites(AppConfig config)
    {
        // Saving.Directory checks
        if (string.IsNullOrWhiteSpace(config.Saving.Directory))
        {
            ShowConfigError("Invalid saving directory:\n\nPath is empty.");
            return false;
        }

        try
        {
            Directory.CreateDirectory(
                Environment.ExpandEnvironmentVariables(config.Saving.Directory)
            );
        }
        catch (Exception ex)
        {
            ShowConfigError($"Invalid saving directory:\n\n{ex.Message}");
            return false;
        }

        // Encoder.Path checks
        if (string.IsNullOrWhiteSpace(config.Encoder.Path))
        {
            ShowConfigError("Invalid encoder path:\n\nPath is empty.");
            return false;
        }

        if (AppPaths.ResolveEncoder(config.Encoder.Path) is null)
        {
            string encoder = Path.GetFileName(config.Encoder.Path);

            // prompt encoder download
            var prompt = MessageBox.Show(
                $"Encoder '{encoder}' was not found.\n\n" +
                "Would you like to attempt to download it from the internet?",
                "Encoder Missing",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (prompt != DialogResult.Yes)
            {
                ShowConfigError($"Invalid encoder path:\n\n'{encoder}'");
                return false;
            }

            // run download
            using var dialog = new EncoderDownloader.DownloadDialog(encoder, () =>
                EncoderDownloader.DownloadAsync(encoder, AppPaths.ToolsDirectory)
            );

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                switch (dialog.Error)
                {
                    case FileNotFoundException or PlatformNotSupportedException:
                        ShowConfigError(
                            dialog.Error.Message
                        );
                        break;

                    case Exception ex:
                        MessageBox.Show(
                            $"Download failed:\n\n{ex.Message}",
                            "Network Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        break;
                }

                return false;
            }
        }

        return true;
    }

    private static void ShowConfigError(string message)
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