using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VeSCU;

public static class EncoderDownloader
{
    private const string ManifestUrl = "https://raw.githubusercontent.com/AsjerS/VeSCU/main/encoders.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task DownloadAsync(string exeName, string destinationDir)
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
        string file = Path.GetFileName(exeName);

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

        using Stream networkStream = await client.GetStreamAsync(downloadUrl);
        using var archive = new ZipArchive(networkStream, ZipArchiveMode.Read);

        ZipArchiveEntry entry = archive.Entries.FirstOrDefault(e =>
            e.Name.Equals(file, StringComparison.OrdinalIgnoreCase)
        ) ?? throw new FileNotFoundException($"Archive did not contain '{file}'.");

        entry.ExtractToFile(targetFilePath, overwrite: true);
    }
}