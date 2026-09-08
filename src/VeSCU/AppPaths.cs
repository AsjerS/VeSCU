namespace VeSCU;

public static class AppPaths
{
    public static string AppDirectory =>
        AppDomain.CurrentDomain.BaseDirectory;

    public static bool IsPortable =>
        File.Exists(Path.Combine(
            AppDirectory,
            "config.toml"
        ));

    public static string ConfigFile => IsPortable
        ? Path.Combine(
            AppDirectory,
            "config.toml"
        )
        : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VeSCU",
            "config.toml"
        );

    public static string InitConfigFile =>
        Path.Combine(
            AppDirectory,
            "config_init.toml"
        );

    public static string ToolsDirectory => IsPortable
        ? Path.Combine(
            AppDirectory,
            "bin"
        )
        : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VeSCU",
            "bin"
        );

    public static string? ResolveEncoder(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (File.Exists(path)) return Path.GetFullPath(path);

        string localAppPath = Path.Combine(AppDirectory, path);
        if (File.Exists(localAppPath)) return localAppPath;

        string toolPath = Path.Combine(ToolsDirectory, path);
        if (File.Exists(toolPath)) return toolPath;

        return FindInPathEnv(path);
    }

    private static string? FindInPathEnv(string fileName)
    {
        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv)) return null;

        foreach (string rawDir in pathEnv.Split(Path.PathSeparator))
        {
            string dir = rawDir.Trim().Trim('"');
            if (string.IsNullOrEmpty(dir)) continue;

            string fullPath = Path.Combine(dir, fileName);
            if (File.Exists(fullPath)) return fullPath;
        }

        return null;
    }
}