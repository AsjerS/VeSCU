using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VeSCU;

internal static class EncoderDownloader
{
    private const string ManifestUrl = "https://raw.githubusercontent.com/AsjerS/VeSCU/main/manifests/encoders-v1.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task DownloadAsync(string encoder, string destinationDir)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("VeSCU-Downloader");

        // fetch encoder catalog
        string json = await client.GetStringAsync(ManifestUrl);

        var manifest = JsonSerializer.Deserialize<Dictionary<string, Dictionary<Architecture, string>>>(
            json,
            JsonOptions
        ) ?? throw new InvalidOperationException("Failed to read encoder catalog.");

        // check if exe exists in the catalog
        string file = Path.GetFileName(encoder);

        if (!manifest.TryGetValue(file, out var archMap))
        {
            throw new FileNotFoundException($"'{file}' is not in the online encoder catalog.");
        }

        // check if possible architecture exists for exe
        var arch = RuntimeInformation.ProcessArchitecture;

        bool TryGetUrl(Architecture key, out string? url) =>
            archMap.TryGetValue(key, out url) && !string.IsNullOrWhiteSpace(url);

        if (!TryGetUrl(arch, out string? downloadUrl) &&
            !(arch == Architecture.Arm64 && TryGetUrl(Architecture.X64, out downloadUrl)))
        {
            throw new PlatformNotSupportedException($"'{file}' is not available for {arch}.");
        }

        // download exe
        Directory.CreateDirectory(destinationDir);
        string targetFilePath = Path.Combine(destinationDir, file);

        using Stream networkStream = await client.GetStreamAsync(downloadUrl!);
        using var archive = new ZipArchive(networkStream, ZipArchiveMode.Read);

        ZipArchiveEntry entry = archive.Entries.FirstOrDefault(
            e => e.Name.Equals(
                file,
                StringComparison.OrdinalIgnoreCase
            )
        ) ?? archive.Entries.FirstOrDefault(
            e => e.Name.StartsWith(Path.GetFileNameWithoutExtension(file),
            StringComparison.OrdinalIgnoreCase
        ) && e.Name.EndsWith(
            ".exe",
            StringComparison.OrdinalIgnoreCase
        )) ?? throw new FileNotFoundException(
            $"Archive did not contain '{file}'."
        );

        entry.ExtractToFile(targetFilePath, overwrite: true);
    }

    public sealed class DownloadDialog : Form
    {
        private readonly Func<Task> _work;
        public Exception? Error { get; private set; }

        public DownloadDialog(string encoder, Func<Task> work)
        {
            _work = work;

            Text = "VeSCU";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(320, 85);
            TopMost = true;

            var label = new Label
            {
                Text = $"Downloading {encoder}...",
                Location = new Point(16, 16),
                AutoSize = true
            };

            var bar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Location = new Point(16, 40),
                Size = new Size(288, 20)
            };

            Controls.Add(label);
            Controls.Add(bar);
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);

            try
            {
                await _work();
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                Error = ex;
                DialogResult = DialogResult.Abort;
            }
            finally
            {
                Close();
            }
        }
    }
}