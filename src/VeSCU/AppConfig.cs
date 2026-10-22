using System.Text.Json.Serialization;
using VeCore;

namespace VeSCU;

internal sealed class AppConfig : AppConfigHandler<AppConfig, AppInfo>
{
    public sealed record SavingSection(
        string Directory = "%USERPROFILE%\\Pictures\\Screenshots",
        string Filename = "Screenshot_{yyyy}-{MM}-{dd}_{HH}-{mm}-{ss}",
        string Extension = ".jxl"
    );

    public enum InputFormat
    {
        Ppm,
        Png
    }

    public sealed record EncoderSection(
        string Path = "cjxl.exe",
        string Arguments = "-d 1 - {Output}",
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        InputFormat InputFormat = InputFormat.Ppm
    );

    public AppHotkey Hotkey { get; set; } = new();
    public SavingSection Saving { get; set; } = new();
    public EncoderSection Encoder { get; set; } = new();

    public static AppConfig? Load()
    {
        var config = Load(AppPaths.ConfigFile);
        if (config != null) return config;

        using var dialog = new FirstRunDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            return dialog.SelectedConfig.Save();
        }

        return null;
    }

    public AppConfig Save() => Save(AppPaths.ConfigFile);
}
