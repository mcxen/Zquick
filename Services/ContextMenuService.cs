using System.Text.RegularExpressions;
using Microsoft.Win32;
using Zquick.Models;

namespace Zquick.Services;

public class ContextMenuStatus
{
    public bool IsRegistered { get; set; }
    public bool OnFolder { get; set; }
    public bool OnBackground { get; set; }
    public string RegisteredExePath { get; set; } = "";
    public bool PathMatchesCurrent { get; set; }
    public int RegisteredItemCount { get; set; }
}

public static class ContextMenuService
{
    public const int MaxActionItems = 15;
    private const string FolderMenuKey = @"Software\Classes\Directory\shell\Zquick";
    private const string BackgroundMenuKey = @"Software\Classes\Directory\Background\shell\Zquick";

    public static (int RegisteredCount, bool Truncated) SyncContextMenu(AppConfig config, string exePath)
    {
        string normalizedExe = Path.GetFullPath(exePath);
        UninstallContextMenu();

        var targets = new List<string>();
        if (config.RegisterOnFolder)
        {
            targets.Add(FolderMenuKey);
        }

        if (config.RegisterOnBackground)
        {
            targets.Add(BackgroundMenuKey);
        }

        if (targets.Count == 0)
        {
            return (0, false);
        }

        List<MenuAction> allEnabled = config.Actions.Where(a => a.Enabled).ToList();
        bool truncated = allEnabled.Count > MaxActionItems;
        List<MenuAction> activeActions = allEnabled.Take(MaxActionItems).ToList();

        string rootTitle = string.IsNullOrWhiteSpace(config.RootMenuTitle)
            ? "Zquick"
            : config.RootMenuTitle.Trim();

        foreach (string rootPath in targets)
        {
            using RegistryKey root = Registry.CurrentUser.CreateSubKey(rootPath);
            root.SetValue("MUIVerb", rootTitle);
            root.SetValue("Icon", $"\"{normalizedExe}\",0");
            // 关键：空字符串使 Explorer 从当前键下的 shell 子键构建二级级联菜单，完全无需 COM
            root.SetValue("SubCommands", "");

            int order = 1;
            foreach (MenuAction action in activeActions)
            {
                string subKeyName = $@"shell\{order:D2}_{SanitizeKeyName(action.Id)}";
                using RegistryKey itemKey = root.CreateSubKey(subKeyName);
                itemKey.SetValue("MUIVerb", action.Title);
                if (action.SeparatorBefore && order > 1)
                {
                    itemKey.SetValue("CommandFlags", 0x20, RegistryValueKind.DWord);
                }

                using RegistryKey cmdKey = itemKey.CreateSubKey("command");
                cmdKey.SetValue("", $"\"{normalizedExe}\" run \"{action.Id}\" \"%V\"");
                order++;
            }

            using RegistryKey settingsKey = root.CreateSubKey(@"shell\99_Settings");
            settingsKey.SetValue("MUIVerb", "管理菜单与设置...");
            settingsKey.SetValue("Icon", $"\"{normalizedExe}\",0");
            if (activeActions.Count > 0)
            {
                settingsKey.SetValue("CommandFlags", 0x20, RegistryValueKind.DWord);
            }

            using RegistryKey settingsCmdKey = settingsKey.CreateSubKey("command");
            settingsCmdKey.SetValue("", $"\"{normalizedExe}\"");
        }

        return (activeActions.Count, truncated);
    }

    public static void UninstallContextMenu()
    {
        Registry.CurrentUser.DeleteSubKeyTree(FolderMenuKey, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(BackgroundMenuKey, throwOnMissingSubKey: false);
    }

    public static ContextMenuStatus GetStatus(string currentExePath)
    {
        bool onFolder = KeyExists(FolderMenuKey);
        bool onBackground = KeyExists(BackgroundMenuKey);

        string probeRoot = onFolder ? FolderMenuKey : (onBackground ? BackgroundMenuKey : "");
        string registeredExe = "";
        int itemCount = 0;

        if (!string.IsNullOrEmpty(probeRoot))
        {
            using RegistryKey? root = Registry.CurrentUser.OpenSubKey(probeRoot);
            if (root != null)
            {
                using RegistryKey? shellKey = root.OpenSubKey("shell");
                if (shellKey != null)
                {
                    string[] subNames = shellKey.GetSubKeyNames();
                    itemCount = subNames.Count(n => !string.Equals(n, "99_Settings", StringComparison.OrdinalIgnoreCase));
                }

                using RegistryKey? settingsCmd = root.OpenSubKey(@"shell\99_Settings\command");
                string? rawCmd = settingsCmd?.GetValue("") as string;
                if (!string.IsNullOrWhiteSpace(rawCmd))
                {
                    registeredExe = ExtractExePath(rawCmd!);
                }
            }
        }

        bool matches = !string.IsNullOrEmpty(registeredExe) &&
                       string.Equals(
                           Path.GetFullPath(registeredExe),
                           Path.GetFullPath(currentExePath),
                           StringComparison.OrdinalIgnoreCase);

        return new ContextMenuStatus
        {
            IsRegistered = onFolder || onBackground,
            OnFolder = onFolder,
            OnBackground = onBackground,
            RegisteredExePath = registeredExe,
            PathMatchesCurrent = matches,
            RegisteredItemCount = itemCount
        };
    }

    private static bool KeyExists(string subKeyPath)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(subKeyPath);
        return key != null;
    }

    private static string ExtractExePath(string commandValue)
    {
        string trimmed = commandValue.Trim();
        if (trimmed.StartsWith("\""))
        {
            int endQuote = trimmed.IndexOf('"', 1);
            if (endQuote > 1)
            {
                return trimmed.Substring(1, endQuote - 1);
            }
        }

        int firstSpace = trimmed.IndexOf(' ');
        return firstSpace > 0 ? trimmed.Substring(0, firstSpace) : trimmed;
    }

    private static string SanitizeKeyName(string raw)
    {
        string cleaned = Regex.Replace(raw ?? "", @"[^A-Za-z0-9_\-]", "_");
        return string.IsNullOrEmpty(cleaned) ? "action" : cleaned;
    }
}
