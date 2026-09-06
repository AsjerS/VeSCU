using System.Diagnostics;

namespace VeSCU;

static class Program
{
    [STAThread]
    static async Task Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.ThreadException += (s, e) =>
            ShowAppError(e.Exception);

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            ShowAppError(e.ExceptionObject as Exception);

        try
        {
            AppConfig? config = AppConfig.Load();
            if (config is null) return;

            if (!await EnsurePrerequisitesAsync(config)) return;

            Application.Run(new ScreenshotContext(config));
        }
        catch (Exception ex)
        {
            ShowAppError(ex);
        }
    }

    private static async Task<bool> EnsurePrerequisitesAsync(AppConfig config)
    {
        // ensure saving directory exists
        if (string.IsNullOrWhiteSpace(config.Saving?.Directory))
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

        // ensure encoder exists
        if (string.IsNullOrWhiteSpace(config.Encoder?.Path))
        {
            ShowConfigError("Invalid encoder path:\n\nPath is empty.");
            return false;
        }

        // prompt encoder download
        if (!AppPaths.IsEncoderAvailable(config.Encoder.Path))
        {
            string encoder = Path.GetFileName(config.Encoder.Path);

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

            try
            {
                await EncoderDownloader.DownloadAsync(encoder, AppPaths.ToolsDirectory);
                return true;
            }
            catch (Exception ex) when (ex is FileNotFoundException or PlatformNotSupportedException)
            {
                ShowConfigError(ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Download failed:\n\n{ex.Message}",
                    "Network Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
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