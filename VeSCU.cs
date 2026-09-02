using System.Buffers;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Tomlyn;

namespace VeSCU;

public class AppConfig
{
    private static readonly string ConfigPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "config.toml"
    );

    public record HotkeySection(
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        Keys Key = Keys.PrintScreen,
        bool Ctrl = true,
        bool Alt = true,
        bool Shift = false,
        bool Win = false
    );

    public record SavingSection(
        string Directory = "%USERPROFILE%\\Pictures",
        string Extension = ".jxl"
    );

    public record EncoderSection(
        string Path = "cjxl.exe",
        string Arguments = "-d 1 - {Output}",
        string InputFormat = "Ppm"
    )
    {
        [JsonIgnore]
        public ImageFormat ImageFormat =>
            typeof(ImageFormat)
                .GetProperty(InputFormat, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase)
                ?.GetValue(null) as ImageFormat
                ?? ImageFormat.Bmp;
    }

    public HotkeySection Hotkey { get; set; } = new();
    public SavingSection Saving { get; set; } = new();
    public EncoderSection Encoder { get; set; } = new();

    public static AppConfig? Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                return TomlSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath));
            }

            var freshConfig = new AppConfig();
            freshConfig.Save();
            return freshConfig;
        }
        catch (Exception ex)
        {
            var result = MessageBox.Show(
                $"Failed to read config.toml:\n\n{ex.Message}\n\nReset to defaults?",
                "Config Error", MessageBoxButtons.YesNo, MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                var freshConfig = new AppConfig();
                freshConfig.Save();
                return freshConfig;
            }

            return null;
        }
    }

    public void Save()
    {
        File.WriteAllText(
            ConfigPath,
            TomlSerializer.Serialize(this)
                .Replace("\n[", "\n\n[").Trim()
                + Environment.NewLine
        );
    }
}

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.ThreadException += (s, e) => ShowError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowError(e.ExceptionObject as Exception);

        try
        {
            AppConfig? config = AppConfig.Load();
            if (config is null) return;

            Application.Run(new ScreenshotContext(config));
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    public static void ShowError(Exception? ex)
    {
        if (ex is null) return;
        MessageBox.Show(ex.Message, "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}


public class ScreenshotContext : ApplicationContext
{
    private readonly AppConfig _config;
    private readonly NotifyIcon _trayIcon;
    private readonly HotkeyListener _hotkeyListener;

    public ScreenshotContext(AppConfig config)
    {
        _config = config;

        // setup hotkey
        _hotkeyListener = new HotkeyListener(_config.Hotkey);
        _hotkeyListener.HotkeyPressed += () => Task.Run(CaptureAndEncode);

        // setup tray menu
        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Capture Screen", null, (s, e) => Task.Run(CaptureAndEncode));
        trayMenu.Items.Add("-");
        trayMenu.Items.Add("Exit", null, (s, e) => Shutdown());

        // setup tray icon
        _trayIcon = new NotifyIcon
        {
            Text = "Custom Encoder Screenshot Utility",
            Icon = SystemIcons.Application,
            ContextMenuStrip = trayMenu,
            Visible = true
        };
    }

    private void Shutdown()
    {
        _trayIcon.Dispose();
        _hotkeyListener.Dispose();
        Application.DoEvents();
        ExitThread();
    }

    private void CaptureAndEncode()
    {
        try
        {
            // define screen bounds
            Rectangle bounds = (Screen.PrimaryScreen ?? throw new Exception("No primary screen detected.")).Bounds;

            // capture screen
            using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppRgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
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
                    FileName = _config.Encoder.Path,
                    Arguments = _config.Encoder.Arguments.Replace("{Output}", $"\"{outputPath}\""),
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // stream image to stdin
            if (_config.Encoder.InputFormat.Equals("Ppm", StringComparison.OrdinalIgnoreCase))
            {
                WritePpmToStream(bitmap, process.StandardInput.BaseStream);
            }
            else
            {
                bitmap.Save(process.StandardInput.BaseStream, _config.Encoder.ImageFormat);
            }

            // close stdin
            process.StandardInput.BaseStream.Flush();
            process.StandardInput.Close();

            // wait for the encoder
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Encoder error (exit code {process.ExitCode}):\n{error}");
            }
        }
        catch (Exception ex)
        {
            Program.ShowError(ex);
        }
    }

    private static void WritePpmToStream(Bitmap bitmap, Stream destination)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        int rowByteCount = width * 3;

        // write header
        byte[] header = System.Text.Encoding.ASCII.GetBytes($"P6\n{width} {height}\n255\n");
        destination.Write(header, 0, header.Length);

        // lock bitmap to access raw memory later
        BitmapData data = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb
        );

        // rent a buffer
        byte[] rowBuffer = ArrayPool<byte>.Shared.Rent(rowByteCount);

        // do the conversion
        try
        {
            unsafe
            {
                byte* scan0 = (byte*)data.Scan0.ToPointer();
                int stride = data.Stride;

                for (int y = 0; y < height; y++)
                {
                    byte* row = scan0 + (y * stride);
                    int bufferIndex = 0;

                    for (int x = 0; x < width; x++)
                    {
                        int pixelIndex = x * 4;

                        // BGRA -> RGB
                        rowBuffer[bufferIndex++] = row[pixelIndex + 2]; // red
                        rowBuffer[bufferIndex++] = row[pixelIndex + 1]; // green
                        rowBuffer[bufferIndex++] = row[pixelIndex];     // blue
                    }

                    destination.Write(rowBuffer, 0, rowByteCount);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rowBuffer);
            bitmap.UnlockBits(data);
        }
    }
}


public sealed partial class HotkeyListener : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 1;
    private const uint MOD_NOREPEAT = 0x4000;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    public event Action? HotkeyPressed;

    public HotkeyListener(AppConfig.HotkeySection hotkey)
    {
        CreateHandle(new CreateParams());

        uint fsModifiers = MOD_NOREPEAT;
        if (hotkey.Alt) fsModifiers |= 0x0001;
        if (hotkey.Ctrl) fsModifiers |= 0x0002;
        if (hotkey.Shift) fsModifiers |= 0x0004;
        if (hotkey.Win) fsModifiers |= 0x0008;

        uint vk = (uint)hotkey.Key;

        if (!RegisterHotKey(Handle, HOTKEY_ID, fsModifiers, vk))
        {
            throw new InvalidOperationException($"Could not register hotkey {hotkey.Key}. It may already be in use.");
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke();
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        UnregisterHotKey(this.Handle, HOTKEY_ID);
        DestroyHandle();
    }
}