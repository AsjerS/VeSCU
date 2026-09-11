namespace VeSCU;

internal sealed class AppEnvironment
{
    public static bool EnsurePrerequisites(AppConfig config)
    {
        // Saving.Directory checks
        if (string.IsNullOrWhiteSpace(config.Saving.Directory))
        {
            AppDialogs.Error("Invalid saving directory:\n\nPath is empty.");
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
            AppDialogs.Error($"Invalid saving directory:\n\n{ex.Message}");
            return false;
        }

        // Encoder.Path checks
        if (string.IsNullOrWhiteSpace(config.Encoder.Path))
        {
            AppDialogs.Error("Invalid encoder path:\n\nPath is empty.");
            return false;
        }

        if (AppPaths.ResolveEncoder(config.Encoder.Path) is null)
        {
            string encoder = Path.GetFileName(config.Encoder.Path);

            // prompt encoder download
            if (
                !AppDialogs.Confirm(
                    $"Encoder '{encoder}' was not found.\n\n" +
                    $"Would you like to attempt to download it from the internet?"
                )
            )
            {
                AppDialogs.Error($"Invalid encoder path:\n\n'{encoder}'");
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
                        AppDialogs.Error(dialog.Error.Message);
                        break;

                    case Exception ex:
                        AppDialogs.Error($"Download failed:\n\n{ex.Message}");
                        break;
                }

                return false;
            }
        }

        return true;
    }
}
