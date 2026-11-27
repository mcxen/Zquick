using System.Text;
using Zquick.Models;

namespace Zquick.Services;

public class ConfigService
{
    private const int MaxRecentPrefixes = 15;

    public string DataDirectory { get; }
    public string ConfigFilePath { get; }

    public ConfigService()
    {
        DataDirectory = ResolveWritableDataDirectory();
        ConfigFilePath = Path.Combine(DataDirectory, "config.json");
    }

    private static string ResolveWritableDataDirectory()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        if (CanWriteToDirectory(baseDir))
        {
            return baseDir;
        }

        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Zquick");
        Directory.CreateDirectory(appDataDir);
        return appDataDir;
    }

    private static bool CanWriteToDirectory(string dir)
    {
        try
        {
            if (!Directory.Exists(dir))
            {
                return false;
            }

            string probe = Path.Combine(dir, $".write_test_{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public AppConfig Load()
    {
        if (!File.Exists(ConfigFilePath))
        {
            AppConfig defaultConfig = DefaultConfigFactory.CreateDefault();
            Save(defaultConfig);
            return defaultConfig;
        }

        try
        {
            string json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
            AppConfig? loaded = JsonHelper.Deserialize<AppConfig>(json);
            if (loaded == null)
            {
                return DefaultConfigFactory.CreateDefault();
            }

            loaded.Actions ??= new List<MenuAction>();
            loaded.RecentPrefixes ??= new List<string>();
            if (string.IsNullOrWhiteSpace(loaded.RootMenuTitle))
            {
                loaded.RootMenuTitle = "Zquick";
            }

            return loaded;
        }
        catch
        {
            return DefaultConfigFactory.CreateDefault();
        }
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(DataDirectory);
        string json = JsonHelper.Serialize(config);
        string tempPath = ConfigFilePath + ".tmp";
        File.WriteAllText(tempPath, json, Encoding.UTF8);
        if (File.Exists(ConfigFilePath))
        {
            File.Delete(ConfigFilePath);
        }

        File.Move(tempPath, ConfigFilePath);
    }

    public void RecordRecentPrefix(AppConfig config, string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return;
        }

        config.RecentPrefixes.RemoveAll(p => string.Equals(p, prefix, StringComparison.Ordinal));
        config.RecentPrefixes.Insert(0, prefix);
        if (config.RecentPrefixes.Count > MaxRecentPrefixes)
        {
            config.RecentPrefixes.RemoveRange(MaxRecentPrefixes, config.RecentPrefixes.Count - MaxRecentPrefixes);
        }

        Save(config);
    }
}
