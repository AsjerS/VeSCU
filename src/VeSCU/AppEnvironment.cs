using VeCore;

namespace VeSCU;

internal sealed class AppEnvironment
{
    public static bool EnsurePrerequisites(AppConfig config)
    {
        // Saving.Directory checks
        if (string.IsNullOrWhiteSpace(config.Saving.Directory))
        {
            AppDialogs<AppInfo>.Error("Invalid saving directory:\n\nPath is empty.");
            return false;
        }

        // Encoder.Path checks
        if (string.IsNullOrWhiteSpace(config.Encoder.Path))
        {
            AppDialogs<AppInfo>.Error("Invalid encoder path:\n\nPath is empty.");
            return false;
        }

        if (AppPaths.ResolveEncoder(config.Encoder.Path) is null)
        {
            string encoder = Path.GetFileName(config.Encoder.Path);

            // prompt encoder download
            if (
                !AppDialogs<AppInfo>.Confirm(
                    $"Encoder '{encoder}' was not found.\n\n" +
                    $"Would you like to attempt to download it from the internet?"
                )
            )
            {
                AppDialogs<AppInfo>.Error($"Invalid encoder path:\n\n'{encoder}'");
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
                        AppDialogs<AppInfo>.Error(dialog.Error.Message);
                        break;

                    case Exception ex:
                        AppDialogs<AppInfo>.Error($"Download failed:\n\n{ex.Message}");
                        break;
                }

                return false;
            }
        }

        return true;
    }
}
