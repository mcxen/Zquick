namespace Zquick.Models;

public enum ActionKind
{
    ListFolder,
    AddFolderPrefix,
    RenameFilesByFolder,
    RemovePrefix,
    CompressSubFolders,
    RotateVideos,
    CustomScript,
    Pipeline
}

public enum RenameScope
{
    CurrentFolder,
    SubFolders
}

public enum VideoRotationMethod
{
    Metadata,
    DisplayRotation
}

public class MenuAction
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool SeparatorBefore { get; set; }
    public ActionKind Kind { get; set; } = ActionKind.RenameFilesByFolder;
    public bool ConfirmBeforeRun { get; set; } = true;

    // 前缀与重命名相关参数
    public string DefaultPrefix { get; set; } = "";
    public bool PromptForInput { get; set; } = true;
    public List<string> RemovePrefixes { get; set; } = new();
    public RenameScope RenameScope { get; set; } = RenameScope.CurrentFolder;
    public bool GroupByExtension { get; set; } = true;
    public bool SingleFileNoNumber { get; set; } = true;
    public string NumberSeparator { get; set; } = "_";
    public int PadDigits { get; set; } = 2;
    public bool SkipHiddenAndSystem { get; set; } = true;
    public bool Recursive { get; set; } = true;

    // 7z 压缩相关参数
    public bool IncludeRootFolder { get; set; } = true;
    public bool DeleteSourceAfterSuccess { get; set; }
    public bool MoveDeletedToRecycleBin { get; set; } = true;
    public bool UsePassword { get; set; }
    public string EncryptedPassword { get; set; } = "";
    public bool PromptPasswordIfEmpty { get; set; } = true;
    public string ArchiveFormat { get; set; } = "zip";

    // FFmpeg 视频旋转相关参数
    public int RotationAngle { get; set; } = 90;
    public string VideoExtensions { get; set; } = ".mp4;.mov;.mkv";
    public string OutputSubFolder { get; set; } = "newfiles";
    public VideoRotationMethod RotationMethod { get; set; } = VideoRotationMethod.Metadata;

    // 自定义脚本相关参数
    public string ScriptPath { get; set; } = "";
    public string ScriptArguments { get; set; } = "";
    public bool ShowConsoleWindow { get; set; }

    // 组合流水线步骤 ID 列表
    public List<string> StepActionIds { get; set; } = new();

    public MenuAction Clone()
    {
        return new MenuAction
        {
            Id = Id,
            Title = Title,
            Enabled = Enabled,
            SeparatorBefore = SeparatorBefore,
            Kind = Kind,
            ConfirmBeforeRun = ConfirmBeforeRun,
            DefaultPrefix = DefaultPrefix,
            PromptForInput = PromptForInput,
            RemovePrefixes = new List<string>(RemovePrefixes),
            RenameScope = RenameScope,
            GroupByExtension = GroupByExtension,
            SingleFileNoNumber = SingleFileNoNumber,
            NumberSeparator = NumberSeparator,
            PadDigits = PadDigits,
            SkipHiddenAndSystem = SkipHiddenAndSystem,
            Recursive = Recursive,
            IncludeRootFolder = IncludeRootFolder,
            DeleteSourceAfterSuccess = DeleteSourceAfterSuccess,
            MoveDeletedToRecycleBin = MoveDeletedToRecycleBin,
            UsePassword = UsePassword,
            EncryptedPassword = EncryptedPassword,
            PromptPasswordIfEmpty = PromptPasswordIfEmpty,
            ArchiveFormat = ArchiveFormat,
            RotationAngle = RotationAngle,
            VideoExtensions = VideoExtensions,
            OutputSubFolder = OutputSubFolder,
            RotationMethod = RotationMethod,
            ScriptPath = ScriptPath,
            ScriptArguments = ScriptArguments,
            ShowConsoleWindow = ShowConsoleWindow,
            StepActionIds = new List<string>(StepActionIds)
        };
    }

    public static string GetKindDisplayName(ActionKind kind) => kind switch
    {
        ActionKind.ListFolder => "目录概览",
        ActionKind.AddFolderPrefix => "文件夹加前缀",
        ActionKind.RenameFilesByFolder => "按文件夹名重命名",
        ActionKind.RemovePrefix => "递归移除前缀",
        ActionKind.CompressSubFolders => "批量 7z 压缩",
        ActionKind.RotateVideos => "视频无损旋转",
        ActionKind.CustomScript => "自定义脚本",
        ActionKind.Pipeline => "组合流水线",
        _ => kind.ToString()
    };

    public string GetSummary() => Kind switch
    {
        ActionKind.ListFolder => "统计子目录/文件数量并生成目录树",
        ActionKind.AddFolderPrefix => PromptForInput
            ? (string.IsNullOrEmpty(DefaultPrefix) ? "运行前弹窗输入前缀" : $"弹窗输入前缀 (默认: {DefaultPrefix})")
            : $"固定前缀: {DefaultPrefix}",
        ActionKind.RenameFilesByFolder =>
            $"{(RenameScope == RenameScope.CurrentFolder ? "当前目录子文件" : "各子目录内文件")} | " +
            $"{(GroupByExtension ? "按扩展名分组" : "统一编号")} | " +
            $"{(SingleFileNoNumber ? "单文件不编号" : "总是编号")} | 补零{PadDigits}位",
        ActionKind.RemovePrefix =>
            $"移除 {RemovePrefixes.Count} 条前缀 ({string.Join(", ", RemovePrefixes.Take(2))}{(RemovePrefixes.Count > 2 ? "..." : "")})" +
            $" | {(Recursive ? "递归子目录" : "仅当前层级")}",
        ActionKind.CompressSubFolders =>
            $"格式:{ArchiveFormat} | {(IncludeRootFolder ? "含外层目录" : "平铺内容")} | " +
            $"{(UsePassword ? "AES256加密" : "无密码")} | " +
            $"{(DeleteSourceAfterSuccess ? (MoveDeletedToRecycleBin ? "成功后移入回收站" : "成功后永久删除") : "保留源目录")}",
        ActionKind.RotateVideos =>
            $"顺时针旋转 {RotationAngle}° | 扩展名:{VideoExtensions} | 输出至 {OutputSubFolder}\\ | " +
            $"{(RotationMethod == VideoRotationMethod.Metadata ? "Metadata模式" : "DisplayRotation模式")}",
        ActionKind.CustomScript =>
            string.IsNullOrWhiteSpace(ScriptPath) ? "未配置脚本路径" : $"脚本: {Path.GetFileName(ScriptPath)}",
        ActionKind.Pipeline =>
            $"串联 {StepActionIds.Count} 个动作: {string.Join(" -> ", StepActionIds)}",
        _ => ""
    };
}

public class AppConfig
{
    public string RootMenuTitle { get; set; } = "Zquick";
    public bool RegisterOnFolder { get; set; } = true;
    public bool RegisterOnBackground { get; set; } = true;
    public string SevenZipPath { get; set; } = @"C:\Program Files\7-Zip\7z.exe";
    public string FfmpegPath { get; set; } = "ffmpeg.exe";
    public bool ConfirmRenameByDefault { get; set; } = true;
    public List<string> RecentPrefixes { get; set; } = new();
    public List<MenuAction> Actions { get; set; } = new();
}

public static class DefaultConfigFactory
{
    public static AppConfig CreateDefault()
    {
        return new AppConfig
        {
            RootMenuTitle = "Zquick",
            RegisterOnFolder = true,
            RegisterOnBackground = true,
            SevenZipPath = @"C:\Program Files\7-Zip\7z.exe",
            FfmpegPath = "ffmpeg.exe",
            ConfirmRenameByDefault = true,
            RecentPrefixes = new List<string>(),
            Actions = new List<MenuAction>
            {
                new()
                {
                    Id = "list-folder",
                    Title = "1. 目录结构概览",
                    Enabled = true,
                    Kind = ActionKind.ListFolder,
                    ConfirmBeforeRun = false
                },
                new()
                {
                    Id = "add-folder-prefix",
                    Title = "2. 子文件夹批量加前缀...",
                    Enabled = true,
                    Kind = ActionKind.AddFolderPrefix,
                    PromptForInput = true,
                    ConfirmBeforeRun = true
                },
                new()
                {
                    Id = "rename-current-files",
                    Title = "3a. 用当前文件夹名重命名子文件",
                    Enabled = true,
                    Kind = ActionKind.RenameFilesByFolder,
                    RenameScope = RenameScope.CurrentFolder,
                    GroupByExtension = true,
                    SingleFileNoNumber = true,
                    NumberSeparator = "_",
                    PadDigits = 2,
                    SkipHiddenAndSystem = true,
                    ConfirmBeforeRun = true
                },
                new()
                {
                    Id = "rename-subfolders-files",
                    Title = "3b. 各子文件夹内文件按文件夹名重命名",
                    Enabled = true,
                    Kind = ActionKind.RenameFilesByFolder,
                    RenameScope = RenameScope.SubFolders,
                    GroupByExtension = false,
                    SingleFileNoNumber = false,
                    NumberSeparator = "_",
                    PadDigits = 2,
                    SkipHiddenAndSystem = true,
                    ConfirmBeforeRun = true
                },
                new()
                {
                    Id = "remove-prefix",
                    Title = "清理指定前缀（可在设置自定义）",
                    Enabled = true,
                    Kind = ActionKind.RemovePrefix,
                    RemovePrefixes = new List<string> { "[广告]_" },
                    Recursive = true,
                    SkipHiddenAndSystem = true,
                    ConfirmBeforeRun = true
                },
                new()
                {
                    Id = "compress-with-root-clean",
                    Title = "4a. 批量压缩子文件夹(含外层+移入回收站)",
                    Enabled = true,
                    SeparatorBefore = true,
                    Kind = ActionKind.CompressSubFolders,
                    IncludeRootFolder = true,
                    DeleteSourceAfterSuccess = true,
                    MoveDeletedToRecycleBin = true,
                    UsePassword = false,
                    ArchiveFormat = "zip",
                    ConfirmBeforeRun = false
                },
                new()
                {
                    Id = "compress-flat-clean",
                    Title = "4b. 批量压缩子文件夹(平铺+移入回收站)",
                    Enabled = true,
                    Kind = ActionKind.CompressSubFolders,
                    IncludeRootFolder = false,
                    DeleteSourceAfterSuccess = true,
                    MoveDeletedToRecycleBin = true,
                    UsePassword = false,
                    ArchiveFormat = "zip",
                    ConfirmBeforeRun = false
                },
                new()
                {
                    Id = "compress-pwd-keep",
                    Title = "4c. 批量加密压缩子文件夹(AES256+保留源)",
                    Enabled = true,
                    Kind = ActionKind.CompressSubFolders,
                    IncludeRootFolder = false,
                    DeleteSourceAfterSuccess = false,
                    MoveDeletedToRecycleBin = true,
                    UsePassword = true,
                    EncryptedPassword = "",
                    PromptPasswordIfEmpty = true,
                    ArchiveFormat = "zip",
                    ConfirmBeforeRun = false
                },
                new()
                {
                    Id = "rotate-video-90",
                    Title = "批量无损旋转视频 90°",
                    Enabled = true,
                    SeparatorBefore = true,
                    Kind = ActionKind.RotateVideos,
                    RotationAngle = 90,
                    VideoExtensions = ".mp4;.mov;.mkv",
                    OutputSubFolder = "newfiles",
                    RotationMethod = VideoRotationMethod.Metadata,
                    ConfirmBeforeRun = false
                },
                new()
                {
                    Id = "rotate-video-270",
                    Title = "批量无损旋转视频 270°",
                    Enabled = true,
                    Kind = ActionKind.RotateVideos,
                    RotationAngle = 270,
                    VideoExtensions = ".mp4;.mov;.mkv",
                    OutputSubFolder = "newfiles",
                    RotationMethod = VideoRotationMethod.Metadata,
                    ConfirmBeforeRun = false
                },
                new()
                {
                    Id = "pipeline-organize",
                    Title = "一键流水线 (去前缀→重命名→平铺压缩)",
                    Enabled = false,
                    SeparatorBefore = true,
                    Kind = ActionKind.Pipeline,
                    StepActionIds = new List<string>
                    {
                        "remove-prefix",
                        "rename-subfolders-files",
                        "compress-flat-clean"
                    },
                    ConfirmBeforeRun = false
                }
            }
        };
    }
}
