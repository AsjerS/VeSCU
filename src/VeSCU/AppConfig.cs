using System.Reflection;
using System.Text.Json.Serialization;
using Tomlyn;

namespace VeSCU;

public class AppConfig
{
    public record HotkeySection(
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        Keys Key = Keys.PrintScreen,
        bool Ctrl = true,
        bool Alt = true,
        bool Shift = false,
        bool Win = false
    );

    public record SavingSection(
        string Directory = "%USERPROFILE%\\Pictures\\Screenshots",
        string Extension = ".jxl"
    );

    public enum InputFormat
    {
        Ppm,
        Png
    }

    public record EncoderSection(
        string Path = "cjxl.exe",
        string Arguments = "-d 1 - {Output}",
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        InputFormat InputFormat = InputFormat.Ppm
    );

    public HotkeySection Hotkey { get; set; } = new();
    public SavingSection Saving { get; set; } = new();
    public EncoderSection Encoder { get; set; } = new();

    public static AppConfig? Load()
    {
        try
        {
            if (File.Exists(AppPaths.ConfigFile))
            {
                string tomlText = File.ReadAllText(AppPaths.ConfigFile);
                var config = TomlSerializer.Deserialize<AppConfig>(tomlText) ?? new AppConfig();
                if (!HasAllKeys(tomlText)) config.Save();
                return config;
            }

            if (File.Exists(AppPaths.InitConfigFile))
            {
                var seededConfig = TomlSerializer.Deserialize<AppConfig>(
                    File.ReadAllText(AppPaths.InitConfigFile)
                ) ?? new AppConfig();
                return seededConfig.Save();
            }

            return new AppConfig().Save();
        }
        catch (Exception ex)
        {
            var result = MessageBox.Show(
                $"Failed to read configuration:\n\n{ex.Message}\n\nReset to defaults?",
                "Config Error",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                return new AppConfig().Save();
            }

            return null;
        }
    }

    public AppConfig Save()
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(AppPaths.ConfigFile)!
        );

        File.WriteAllText(
            AppPaths.ConfigFile,
            TomlSerializer.Serialize(this)
                .Replace("\n[", "\n\n[").Trim()
                + Environment.NewLine
        );

        return this;
    }

    private static bool HasAllKeys(string tomlText) =>
        typeof(AppConfig)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(section =>
                section
                    .PropertyType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(prop => prop.Name)
                    .Prepend($"[{section.Name}]")
            )
            .All(token =>
                tomlText
                .Contains(token, StringComparison.OrdinalIgnoreCase)
            );
}
