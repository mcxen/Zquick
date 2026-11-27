using System.Reflection;
using System.Runtime.InteropServices;
using Zquick.Models;
using Zquick.Services;
using Zquick.UI;

namespace Zquick;

internal static class Program
{
    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    private static void TryEnablePerMonitorV2Dpi()
    {
        try
        {
            // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4
            SetProcessDpiAwarenessContext(new IntPtr(-4));
        }
        catch
        {
            // 降级由 app.manifest 的 dpiAware / dpiAwareness 处理
        }
    }

    [STAThread]
    private static void Main(string[] args)
    {
        TryEnablePerMonitorV2Dpi();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.ThreadException += (_, e) =>
            MessageBox.Show($"发生未处理界面异常：\n{e.Exception.Message}", "Zquick 错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"发生严重异常：\n{ex.Message}", "Zquick 错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        var configService = new ConfigService();
        var historyService = new HistoryService(configService.DataDirectory);

        if (args.Length > 0)
        {
            string cmd = args[0].Trim();

            if (string.Equals(cmd, "--install", StringComparison.OrdinalIgnoreCase))
            {
                AppConfig cfg = configService.Load();
                ContextMenuService.SyncContextMenu(cfg, GetCurrentExePath());
                return;
            }

            if (string.Equals(cmd, "--uninstall", StringComparison.OrdinalIgnoreCase))
            {
                ContextMenuService.UninstallContextMenu();
                return;
            }

            if (string.Equals(cmd, "run", StringComparison.OrdinalIgnoreCase) && args.Length >= 3)
            {
                string actionId = args[1].Trim();
                string rawFolder = string.Join(" ", args.Skip(2));
                string folderPath = PathGuard.NormalizeFolderPath(rawFolder);

                AppConfig cfg = configService.Load();
                MenuAction? action = cfg.Actions.FirstOrDefault(
                    a => string.Equals(a.Id, actionId, StringComparison.OrdinalIgnoreCase));

                if (action == null)
                {
                    MessageBox.Show(
                        $"未找到右键动作 \"{actionId}\"，请打开管理界面重新同步右键菜单。",
                        "Zquick",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                Application.Run(new RunProgressForm(configService, historyService, cfg, action, folderPath));
                return;
            }
        }

        Application.Run(new MainForm(configService, historyService));
    }

    public static string GetCurrentExePath()
    {
        return Application.ExecutablePath;
    }

    public static string GetDisplayVersion()
    {
        var attr = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        string? raw = attr?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            int plusIdx = raw.IndexOf('+');
            return plusIdx > 0 ? raw.Substring(0, plusIdx) : raw;
        }

        Version? ver = Assembly.GetExecutingAssembly().GetName().Version;
        return ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "1.0.1";
    }
}
