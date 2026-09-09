using System.Reflection;
using System.Text.Json.Serialization;
using Tomlyn;

namespace VeSCU;

internal sealed class AppConfig
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
            if (File.Exists(AppPaths.ConfigFile) && new FileInfo(AppPaths.ConfigFile).Length > 0)
            {
                string tomlText = File.ReadAllText(AppPaths.ConfigFile);
                var config = TomlSerializer.Deserialize<AppConfig>(tomlText) ?? new AppConfig();

                if (!HasAllKeys(tomlText)) config.Save();
                return config;
            }

            using var dialog = new FirstRunDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.SelectedConfig.Save();
            }

            return null;
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
