using System.Diagnostics;
using System.Drawing.Imaging;

namespace VeSCU;

internal sealed class ScreenshotContext : ApplicationContext
{
    private AppConfig _config;
    private string _encoderPath;
    private HotkeyListener _hotkeyListener;
    private readonly NotifyIcon _trayIcon;

    public ScreenshotContext(AppConfig config)
    {
        _config = config;
        _encoderPath = AppPaths.ResolveEncoder(_config.Encoder.Path)!;

        // setup hotkey
        _hotkeyListener = new HotkeyListener(_config.Hotkey);
        _hotkeyListener.HotkeyPressed += () => Task.Run(CaptureAndEncode);

        // setup tray icon
        _trayIcon = new NotifyIcon
        {
            Text = AppInfo.Name,
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
            ContextMenuStrip = CreateTrayMenu(),
            Visible = true
        };

        _trayIcon.DoubleClick += (_, _) =>
            OpenConfig();

        _trayIcon.MouseClick += (_, e) =>
            { if (e.Button == MouseButtons.Middle) ReloadConfig(); };
    }

    private void Shutdown()
    {
        _trayIcon.Dispose();
        _hotkeyListener.Dispose();
        Application.DoEvents();
        ExitThread();
    }

    private ContextMenuStrip CreateTrayMenu()
    {
        var trayMenu = new ContextMenuStrip();

        trayMenu.Items.Add("Capture Screen", null, (_, _) =>
            Task.Run(CaptureAndEncode));

        trayMenu.Items.Add("Open Config", null, (_, _) =>
            Process.Start(new ProcessStartInfo(AppPaths.ConfigFile) { UseShellExecute = true }));

        trayMenu.Items.Add("Reload Config", null, (_, _) =>
            ReloadConfig());

        if (!AppPaths.IsPortable)
        {
            trayMenu.Items.Add(new ToolStripMenuItem("Start with Windows", null, (s, _) =>
                AutoStartupManager.SetEnabled(((ToolStripMenuItem)s!).Checked))
            {
                CheckOnClick = true,
                Checked = AutoStartupManager.IsEnabled()
            });
        }

        trayMenu.Items.Add("-");

        trayMenu.Items.Add("Exit", null, (_, _) =>
            Shutdown());

        return trayMenu;
    }

    private static void OpenConfig() =>
        Process.Start(new ProcessStartInfo(AppPaths.ConfigFile) { UseShellExecute = true });

    private void CaptureAndEncode()
    {
        try
        {
            // define screen bounds
            Rectangle bounds = (
                Screen.PrimaryScreen
                    ?? throw new InvalidOperationException("No primary screen detected.")
            ).Bounds;

            // capture screen
            using var bitmap = new Bitmap(
                bounds.Width, bounds.Height,
                PixelFormat.Format32bppRgb
            );
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    bounds.X, bounds.Y,
                    0, 0,
                    bounds.Size,
                    CopyPixelOperation.SourceCopy
                );
            }

            // define output path
            string outputPath = Path.Combine(
                Environment.ExpandEnvironmentVariables(_config.Saving.Directory),
                $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{_config.Saving.Extension}"
            );

            // start encoder
            using var process = new Process
            {
                StartInfo = new()
                {
                    FileName = _encoderPath,
                    Arguments = _config.Encoder.Arguments.Replace("{Output}", $"\"{outputPath}\""),
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.PriorityClass = ProcessPriorityClass.BelowNormal;

            // stream image to stdin
            using (Stream stdin = process.StandardInput.BaseStream)
            {
                bitmap.WriteAs(_config.Encoder.InputFormat, stdin);
            }

            // wait for the encoder
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Encoder error (exit code {process.ExitCode}):\n{error}"
                );
            }
        }
        catch (Exception ex)
        {
            AppDialogs.Error(ex);
        }
    }

    private void ReloadConfig()
    {
        try
        {
            // load new config
            var config = AppConfig.Load();
            if (config is null || !AppEnvironment.EnsurePrerequisites(config)) return;

            // apply new config
            _config = config;
            _encoderPath = AppPaths.ResolveEncoder(config.Encoder.Path)!;
            _hotkeyListener.Dispose();
            _hotkeyListener = new HotkeyListener(config.Hotkey);
            _hotkeyListener.HotkeyPressed += () => Task.Run(CaptureAndEncode);

            // celebrate
            AppDialogs.Info("Configuration reloaded successfully.");
        }
        catch (Exception ex)
        {
            AppDialogs.Error($"Failed to reload configuration:\n\n{ex.Message}");
        }
    }
}
