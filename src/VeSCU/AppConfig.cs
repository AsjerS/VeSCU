using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Tomlyn;
using Tomlyn.Model;
using VeCore;
using VeSCU.ConfigDialogs;

namespace VeSCU;

internal sealed partial class AppConfig : AppConfigHandler<AppConfig, AppInfo>
{
    public sealed record SavingSection(
        string Directory = "%USERPROFILE%\\Pictures\\Screenshots",
        string Filename = "Screenshot_{yyyy}-{MM}-{dd}_{HH}-{mm}-{ss}"
    );

    public enum InputFormat
    {
        Ppm,
        Png
    }

    public sealed record EncoderSection(
        string Path = "cwebp.exe",
        string Arguments = "-q 90 -m 6 -o {Output} -- -",
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        InputFormat InputFormat = InputFormat.Png,
        string Extension = ".webp"
    );

    public int ConfigVersion_DoNotModify { get; set; } = Migrator.TargetVersion;

    public AppHotkey Hotkey { get; set; } = new();
    public SavingSection Saving { get; set; } = new();
    public EncoderSection Encoder { get; set; } = new();

    public static AppConfig? Load()
    {
        if (File.Exists(AppPaths.ConfigFile))
        {
            if (Migrator.TryMigrate(File.ReadAllText(AppPaths.ConfigFile),
                out var migratedText))
            {
                File.WriteAllText(AppPaths.ConfigFile, migratedText);
            }
        }

        var config = Load(AppPaths.ConfigFile);
        if (config is not null) return config;

        if (ConfigDialog.Prompt<EncoderDialog, EncoderSection>(out var encoder))
        {
            return new AppConfig { Encoder = encoder }.Save();
        }

        return null;
    }

    private static class Migrator
    {
        private const string VersionKey = nameof(ConfigVersion_DoNotModify);

        private static readonly Action<TomlTable>[] Migrations =
        [
            ToV2,
        ];

        public static readonly int TargetVersion = Migrations.Length + 1;

        public static bool TryMigrate(
            string tomlText,
            [NotNullWhen(true)] out string? migratedText)
        {
            migratedText = null;

            if (tomlText.Contains($"{VersionKey} = {TargetVersion}", StringComparison.Ordinal))
                return false;

            TomlTable? model = TomlSerializer.Deserialize<TomlTable>(tomlText);
            if (model == null) return false;

            int currentVersion = model.TryGetValue(VersionKey, out var verObj) && verObj is long l
                ? (int)l
                : 1;
            if (currentVersion >= TargetVersion) return false;

            for (int i = currentVersion - 1; i < Migrations.Length; i++)
            {
                Migrations[i](model);
            }

            model[VersionKey] = TargetVersion;
            migratedText = FormatToml(TomlSerializer.Serialize(model));
            return true;
        }

        private static void ToV2(TomlTable model)
        {
            if (model.TryGetValue("Saving", out var savingObj)
                && savingObj is TomlTable saving
                && saving.TryGetValue("Extension", out var ext))
            {
                saving.Remove("Extension");

                if (model.TryGetValue("Encoder", out var encoderObj)
                    && encoderObj is TomlTable encoder)
                {
                    encoder["Extension"] = ext;
                }
                else
                {
                    model["Encoder"] = new TomlTable { ["Extension"] = ext };
                }
            }
        }
    }

    public AppConfig Save() => Save(AppPaths.ConfigFile);
}
